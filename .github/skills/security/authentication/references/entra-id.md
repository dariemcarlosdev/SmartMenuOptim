# Entra ID (Azure AD) Authentication

## App Registration

Register the application in the Azure Portal or via Azure CLI:

```bash
# Register a new app in Entra ID
az ad app create --display-name "NexTruzt Escrow Platform" \
  --sign-in-audience AzureADMyOrg \
  --web-redirect-uris "https://localhost:5001/signin-oidc"
```

**Required configuration:**
- Redirect URIs: Set explicitly for each environment — never use wildcards in production
- API permissions: Request least-privilege scopes (`User.Read`, not `User.ReadWrite.All`)
- App Roles: Define in manifest for coarse-grained authorization (Agent, Admin, Viewer)
- Client secret: NEVER store in config files — use Azure Key Vault or Managed Identity

## Microsoft.Identity.Web Setup

### Package Installation
```bash
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.Identity.Web.UI
dotnet add package Microsoft.Identity.Web.DownstreamApi
```

### Program.cs — Blazor Server with Entra ID
```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Authentication via Entra ID
builder.Services.AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### Program.cs — API with Entra ID JWT Bearer
```csharp
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## appsettings.json Template

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

> **SECURITY:** Never add `ClientSecret` to `appsettings.json`. Use `dotnet user-secrets` for local development and Azure Key Vault for deployed environments.

```bash
# Local development — store secret safely
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret-here"
```

## Token Cache Configuration

### In-Memory Cache (Development / Single Instance)
```csharp
builder.Services.AddMicrosoftIdentityWebApp(config)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();
```

### Distributed Cache (Production / Multi-Instance)
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration
        .GetConnectionString("Redis");
});

builder.Services.AddMicrosoftIdentityWebApp(config)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDistributedTokenCaches();
```

## Managed Identity (Zero Secrets)

For Azure-hosted services accessing other Azure resources:

```csharp
using Azure.Identity;

// DefaultAzureCredential uses Managed Identity in Azure,
// falls back to developer credentials locally
builder.Services.AddSingleton(new DefaultAzureCredential());

// Access Key Vault without secrets
builder.Configuration.AddAzureKeyVault(
    new Uri("https://nextruzt-vault.vault.azure.net/"),
    new DefaultAzureCredential());
```

## Multi-Tenant Configuration

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common",
    "ClientId": "YOUR_CLIENT_ID",
    "CallbackPath": "/signin-oidc"
  }
}
```

Restrict to authorized tenants in code:

```csharp
builder.Services.Configure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme,
    options =>
    {
        var allowedTenants = new HashSet<string> { "tenant-1-id", "tenant-2-id" };

        options.TokenValidationParameters.IssuerValidator =
            (issuer, token, parameters) =>
            {
                var tenantId = token.Claims
                    .FirstOrDefault(c => c.Type == "tid")?.Value;

                if (tenantId is null || !allowedTenants.Contains(tenantId))
                    throw new SecurityTokenInvalidIssuerException(
                        $"Tenant '{tenantId}' is not authorized.");

                return issuer;
            };
    });
```

## Conditional Access Handling

Handle Entra ID conditional access challenges (e.g., MFA step-up):

```csharp
using Microsoft.Identity.Web;

public sealed class DownstreamApiService(ITokenAcquisition tokenAcquisition)
{
    public async Task<string> CallProtectedApiAsync(CancellationToken ct)
    {
        try
        {
            var token = await tokenAcquisition.GetAccessTokenForUserAsync(
                ["api://downstream/.default"], cancellationToken: ct);

            return token;
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            // Conditional access triggered — propagate the challenge
            throw;
        }
    }
}
```
