# JWT Bearer Token Authentication

## Configuration

### Program.cs — JWT Bearer Setup
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience = builder.Configuration["Jwt:Audience"];
        options.RequireHttpsMetadata = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers
                        .Append("X-Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                var userId = context.Principal?.FindFirst("sub")?.Value;
                logger.LogInformation(
                    "Token validated for user {UserId}", userId);
                return Task.CompletedTask;
            }
        };
    });

// Default deny — every endpoint requires authentication
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

### appsettings.json Template
```json
{
  "Jwt": {
    "Authority": "https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0",
    "Issuer": "https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0",
    "Audience": "api://YOUR_CLIENT_ID"
  }
}
```

## Signing Key Management

### Asymmetric Keys (Recommended for Production)
```csharp
using System.Security.Cryptography;

// Load signing key from Key Vault in production
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        // For Entra ID / external IdP: key is fetched from OIDC discovery
        options.Authority = "https://login.microsoftonline.com/{tenant}/v2.0";
        // Keys are automatically resolved from .well-known/openid-configuration
    });
```

### Symmetric Keys (Development Only)
```csharp
// ONLY for development — never use symmetric keys in production
options.TokenValidationParameters = new TokenValidationParameters
{
    IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(
            builder.Configuration["Jwt:SecretKey"]!))
};
```

> **SECURITY:** Never store signing keys in `appsettings.json`. Use `dotnet user-secrets` for development and Azure Key Vault for production.

## Refresh Token Pattern

### Token Generation Service
```csharp
public sealed class TokenService(
    IOptions<JwtSettings> jwtSettings,
    IRefreshTokenRepository refreshTokenStore)
{
    public async Task<TokenPair> GenerateTokenPairAsync(
        ClaimsPrincipal principal, CancellationToken ct)
    {
        var accessToken = GenerateAccessToken(principal);
        var refreshToken = GenerateRefreshToken();

        await refreshTokenStore.StoreAsync(new RefreshTokenEntry
        {
            Token = refreshToken,
            UserId = principal.FindFirst("sub")!.Value,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        }, ct);

        return new TokenPair(accessToken, refreshToken);
    }

    private string GenerateAccessToken(ClaimsPrincipal principal)
    {
        var settings = jwtSettings.Value;
        var claims = principal.Claims.ToList();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(settings.AccessTokenLifetimeMinutes),
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            SigningCredentials = new SigningCredentials(
                settings.GetSigningKey(),
                SecurityAlgorithms.RsaSha256)
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }

    private static string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
```

### Refresh Token Endpoint
```csharp
app.MapPost("/api/auth/refresh", async (
    RefreshTokenRequest request,
    TokenService tokenService,
    IRefreshTokenRepository store,
    CancellationToken ct) =>
{
    var storedToken = await store.GetAsync(request.RefreshToken, ct);

    if (storedToken is null || storedToken.ExpiresAt < DateTime.UtcNow)
        return Results.Unauthorized();

    if (storedToken.IsRevoked)
    {
        // Potential token theft — revoke entire family
        await store.RevokeAllForUserAsync(storedToken.UserId, ct);
        return Results.Unauthorized();
    }

    // Rotate: revoke old, issue new
    await store.RevokeAsync(storedToken.Token, ct);

    var principal = ValidateExpiredToken(request.AccessToken);
    var newPair = await tokenService.GenerateTokenPairAsync(principal, ct);

    return Results.Ok(newPair);
})
.AllowAnonymous(); // Refresh endpoint must be anonymous — token is the credential
```

## Per-Endpoint Authentication Schemes

```csharp
// Support multiple auth schemes
builder.Services.AddAuthentication()
    .AddJwtBearer("ExternalApi", options =>
    {
        options.Authority = "https://external-idp.com";
        options.Audience = "external-api";
    })
    .AddJwtBearer("InternalApi", options =>
    {
        options.Authority = "https://internal-idp.com";
        options.Audience = "internal-api";
    });

// Use specific scheme on endpoints
app.MapGet("/api/external/data", () => Results.Ok())
    .RequireAuthorization(new AuthorizeAttribute
    {
        AuthenticationSchemes = "ExternalApi"
    });

app.MapGet("/api/internal/data", () => Results.Ok())
    .RequireAuthorization(new AuthorizeAttribute
    {
        AuthenticationSchemes = "InternalApi"
    });
```

## Custom Token Validation

```csharp
public sealed class EscrowTokenValidator : ISecurityTokenValidator
{
    public bool CanValidateToken => true;
    public int MaximumTokenSizeInBytes { get; set; } = 1024 * 10;

    public ClaimsPrincipal ValidateToken(
        string securityToken,
        TokenValidationParameters parameters,
        out SecurityToken validatedToken)
    {
        var handler = new JsonWebTokenHandler();
        var result = handler.ValidateTokenAsync(securityToken, parameters)
            .GetAwaiter().GetResult();

        if (!result.IsValid)
            throw new SecurityTokenValidationException(
                "Token validation failed.");

        validatedToken = result.SecurityToken;

        // Add custom escrow-specific claims
        var identity = result.ClaimsIdentity;
        identity.AddClaim(new Claim("escrow:validated", "true"));

        return new ClaimsPrincipal(identity);
    }
}
```
