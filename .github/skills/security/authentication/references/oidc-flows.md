# OpenID Connect (OIDC) Authentication Flows

## Authorization Code + PKCE (Interactive Users)

The recommended flow for all interactive authentication. PKCE prevents authorization code interception attacks.

### Program.cs — AddOpenIdConnect with PKCE
```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = builder.Configuration["Oidc:Authority"];
        options.ClientId = builder.Configuration["Oidc:ClientId"];
        options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
        options.ResponseType = "code";
        options.UsePkce = true; // ALWAYS enable PKCE

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("offline_access"); // For refresh tokens

        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.RequireHttpsMetadata = true;

        // Map claims from IdP to ClaimsPrincipal
        options.ClaimActions.MapJsonKey("role", "role");
        options.ClaimActions.MapJsonKey("escrow_role", "escrow_role");

        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                var sub = context.Principal?
                    .FindFirst("sub")?.Value;
                logger.LogInformation(
                    "OIDC token validated for {Subject}", sub);
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Failure,
                    "OIDC remote authentication failure");
                context.HandleResponse();
                context.Response.Redirect("/auth/error");
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

### appsettings.json Template
```json
{
  "Oidc": {
    "Authority": "https://your-identity-server.com",
    "ClientId": "nextruzt-escrow-app"
  }
}
```

> **SECURITY:** Store `ClientSecret` in `dotnet user-secrets` (dev) or Azure Key Vault (prod). Never in appsettings.json.

## Client Credentials Flow (Machine-to-Machine)

Used for service-to-service communication where no user interaction is involved.

### Token Acquisition Service
```csharp
using System.Net.Http.Headers;

public sealed class ClientCredentialsTokenService(
    IHttpClientFactory httpClientFactory,
    IOptions<ClientCredentialsOptions> options,
    IMemoryCache cache,
    ILogger<ClientCredentialsTokenService> logger)
{
    private const string CacheKey = "m2m_access_token";

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out string? cachedToken))
            return cachedToken!;

        var settings = options.Value;
        var client = httpClientFactory.CreateClient("TokenClient");

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = settings.ClientId,
            ["client_secret"] = settings.ClientSecret,
            ["scope"] = settings.Scope
        };

        var response = await client.PostAsync(
            settings.TokenEndpoint,
            new FormUrlEncodedContent(tokenRequest),
            ct);

        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content
            .ReadFromJsonAsync<TokenResponse>(ct);

        // Cache with buffer before expiry
        var cacheExpiry = TimeSpan.FromSeconds(
            tokenResponse!.ExpiresIn - 60);
        cache.Set(CacheKey, tokenResponse.AccessToken, cacheExpiry);

        logger.LogInformation(
            "Acquired M2M token, expires in {Seconds}s",
            tokenResponse.ExpiresIn);

        return tokenResponse.AccessToken;
    }
}

public sealed record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType);
```

### Register Delegating Handler for Downstream APIs
```csharp
public sealed class ClientCredentialsHandler(
    ClientCredentialsTokenService tokenService)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var token = await tokenService.GetAccessTokenAsync(ct);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, ct);
    }
}

// Program.cs
builder.Services.AddTransient<ClientCredentialsHandler>();
builder.Services.AddHttpClient("DownstreamApi", client =>
{
    client.BaseAddress = new Uri("https://api.nextruzt.io");
})
.AddHttpMessageHandler<ClientCredentialsHandler>();
```

## OIDC Discovery

OpenID Connect providers expose a discovery document at `/.well-known/openid-configuration`:

```csharp
// Automatic discovery (default behavior)
options.Authority = "https://your-identity-server.com";
// The middleware automatically fetches:
// https://your-identity-server.com/.well-known/openid-configuration
// to discover endpoints, signing keys, supported scopes, etc.

// Manual configuration (when discovery is not available)
options.Configuration = new OpenIdConnectConfiguration
{
    AuthorizationEndpoint = "https://idp.com/connect/authorize",
    TokenEndpoint = "https://idp.com/connect/token",
    UserInfoEndpoint = "https://idp.com/connect/userinfo",
    EndSessionEndpoint = "https://idp.com/connect/endsession"
};
```

## Duende IdentityServer Integration

### Client Configuration in IdentityServer
```csharp
// IdentityServer Config.cs
public static IEnumerable<Client> Clients =>
[
    // Blazor Server app — Authorization Code + PKCE
    new Client
    {
        ClientId = "nextruzt-blazor",
        ClientName = "NexTruzt Escrow Platform",
        AllowedGrantTypes = GrantTypes.Code,
        RequirePkce = true,
        RequireClientSecret = true,
        ClientSecrets = { new Secret("secret".Sha256()) },
        RedirectUris = { "https://localhost:5001/signin-oidc" },
        PostLogoutRedirectUris = { "https://localhost:5001/signout-callback-oidc" },
        AllowedScopes =
        {
            IdentityServerConstants.StandardScopes.OpenId,
            IdentityServerConstants.StandardScopes.Profile,
            IdentityServerConstants.StandardScopes.Email,
            "escrow.api"
        },
        AllowOfflineAccess = true,
        AccessTokenLifetime = 3600, // 1 hour
        RefreshTokenUsage = TokenUsage.OneTimeOnly,
        RefreshTokenExpiration = TokenExpiration.Sliding,
        SlidingRefreshTokenLifetime = 86400 // 24 hours
    },

    // Machine-to-machine — Client Credentials
    new Client
    {
        ClientId = "nextruzt-payment-service",
        ClientName = "NexTruzt Payment Service",
        AllowedGrantTypes = GrantTypes.ClientCredentials,
        ClientSecrets = { new Secret("payment-secret".Sha256()) },
        AllowedScopes = { "escrow.api", "payment.process" }
    }
];
```

### API Scope Definitions
```csharp
public static IEnumerable<ApiScope> ApiScopes =>
[
    new ApiScope("escrow.api", "Escrow API")
    {
        UserClaims = { "escrow_role", "tenant_id" }
    },
    new ApiScope("payment.process", "Payment Processing")
    {
        UserClaims = { "payment_tier" }
    }
];
```

## Logout Flow

### Sign-Out with IdP Redirect
```csharp
app.MapGet("/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(
        OpenIdConnectDefaults.AuthenticationScheme);
})
.AllowAnonymous(); // Logout must be accessible to trigger sign-out
```

### Blazor Server Logout Component
```csharp
// LogoutButton.razor.cs
public sealed partial class LogoutButton : ComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private void Logout()
    {
        Navigation.NavigateTo("/auth/logout", forceLoad: true);
    }
}
```
