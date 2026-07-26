# Authentication & Authorization Reference

> **Load when:** Configuring JWT, Entra ID, ASP.NET Core Identity, or policy-based authorization.

## Authentication Strategy Selection

| Provider | Use When | Setup Complexity |
|---|---|---|
| **Entra ID (Azure AD)** | Cloud-hosted, enterprise, Entra ecosystem | Low (with `Microsoft.Identity.Web`) |
| **Duende IdentityServer** | Self-hosted OIDC, on-prem, multi-IdP federation | High |
| **ASP.NET Core Identity** | Simple app-local authentication, smaller projects | Medium |
| **JWT Bearer** | API-to-API, microservices, mobile clients | Low |

## Entra ID Configuration

```csharp
// Program.cs
builder.Services
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// appsettings.json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "CallbackPath": "/signin-oidc"
  }
}
```

## JWT Bearer Authentication

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience = builder.Configuration["Jwt:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });
```

## Policy-Based Authorization

### Define Policies

```csharp
// AuthorizationPolicies.cs — centralized policy definitions
public static class AuthorizationPolicies
{
    public const string EscrowOperator = nameof(EscrowOperator);
    public const string EscrowManager = nameof(EscrowManager);
    public const string ComplianceOfficer = nameof(ComplianceOfficer);
    public const string SystemAdmin = nameof(SystemAdmin);

    public static void AddEscrowPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(EscrowOperator, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireClaim("scope", "escrow.read"));

        options.AddPolicy(EscrowManager, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireClaim("scope", "escrow.write")
                  .RequireRole(Roles.Manager, Roles.Admin));

        options.AddPolicy(ComplianceOfficer, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireClaim("department", "compliance")
                  .RequireRole(Roles.ComplianceOfficer));

        options.AddPolicy(SystemAdmin, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireRole(Roles.Admin));
    }
}

// Roles constants
public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Operator = "Operator";
    public const string ComplianceOfficer = "ComplianceOfficer";
}
```

### Register Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build(); // Default deny-all

    options.AddEscrowPolicies();
});
```

### Apply on Endpoints

```csharp
group.MapGet("/", GetAllEscrows)
    .RequireAuthorization(AuthorizationPolicies.EscrowOperator);

group.MapPost("/{id}/release", ReleaseEscrow)
    .RequireAuthorization(AuthorizationPolicies.EscrowManager);
```

## Resource-Based Authorization

```csharp
// For entity-level access control
public sealed class EscrowAuthorizationHandler 
    : AuthorizationHandler<EscrowOperationRequirement, Escrow>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EscrowOperationRequirement requirement,
        Escrow escrow)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (requirement.Operation == "view" &&
            (escrow.BuyerId == userId || escrow.SellerId == userId))
        {
            context.Succeed(requirement);
        }

        if (requirement.Operation == "release" &&
            context.User.IsInRole(Roles.Manager))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

## Claims Transformation

```csharp
public sealed class AppClaimsTransformation(
    IUserService userService) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = (ClaimsIdentity)principal.Identity!;
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is not null && !identity.HasClaim("app_role", ""))
        {
            var appUser = await userService.GetByExternalIdAsync(userId);
            if (appUser is not null)
            {
                identity.AddClaim(new Claim("app_role", appUser.Role));
                identity.AddClaim(new Claim("tenant_id", appUser.TenantId));
            }
        }

        return principal;
    }
}
```

## Blazor Authentication

```csharp
// In Blazor Server components
@attribute [Authorize(Policy = "EscrowOperator")]

<AuthorizeView Policy="EscrowManager">
    <Authorized>
        <button @onclick="ReleaseEscrow">Release Funds</button>
    </Authorized>
    <NotAuthorized>
        <p>Insufficient permissions to release escrow.</p>
    </NotAuthorized>
</AuthorizeView>

// Code-behind: access user identity
[CascadingParameter] private Task<AuthenticationState> AuthState { get; set; } = default!;

private async Task<string> GetCurrentUserId()
{
    var state = await AuthState;
    return state.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
}
```
