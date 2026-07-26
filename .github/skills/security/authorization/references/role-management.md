# Role Management

## Role Constants

Never hard-code role names as string literals. Define constants:

```csharp
namespace EscrowApp.Application.Authorization;

public static class Roles
{
    public const string SystemAdministrator = "SystemAdministrator";
    public const string Administrator = "Administrator";
    public const string Agent = "Agent";
    public const string Auditor = "Auditor";
    public const string Buyer = "Buyer";
    public const string Seller = "Seller";
    public const string Viewer = "Viewer";

    public static readonly IReadOnlyList<string> All =
    [
        SystemAdministrator,
        Administrator,
        Agent,
        Auditor,
        Buyer,
        Seller,
        Viewer
    ];

    public static readonly IReadOnlyList<string> AdminRoles =
    [
        SystemAdministrator,
        Administrator
    ];

    public static readonly IReadOnlyList<string> TransactionRoles =
    [
        Agent,
        Administrator,
        Buyer,
        Seller
    ];
}
```

## Entra ID App Roles

Define roles in the Entra ID application manifest:

```json
{
  "appRoles": [
    {
      "allowedMemberTypes": ["User"],
      "displayName": "System Administrator",
      "id": "00000000-0000-0000-0000-000000000001",
      "isEnabled": true,
      "description": "Full system access including tenant management",
      "value": "EscrowApp.SystemAdmin"
    },
    {
      "allowedMemberTypes": ["User"],
      "displayName": "Administrator",
      "id": "00000000-0000-0000-0000-000000000002",
      "isEnabled": true,
      "description": "Tenant-level administration",
      "value": "EscrowApp.Admin"
    },
    {
      "allowedMemberTypes": ["User"],
      "displayName": "Escrow Agent",
      "id": "00000000-0000-0000-0000-000000000003",
      "isEnabled": true,
      "description": "Can manage and release escrow transactions",
      "value": "EscrowApp.Agent"
    },
    {
      "allowedMemberTypes": ["User"],
      "displayName": "Auditor",
      "id": "00000000-0000-0000-0000-000000000004",
      "isEnabled": true,
      "description": "Read-only access to audit logs and reports",
      "value": "EscrowApp.Auditor"
    },
    {
      "allowedMemberTypes": ["User", "Application"],
      "displayName": "Buyer",
      "id": "00000000-0000-0000-0000-000000000005",
      "isEnabled": true,
      "description": "Can create and fund escrow transactions",
      "value": "EscrowApp.Buyer"
    },
    {
      "allowedMemberTypes": ["User", "Application"],
      "displayName": "Seller",
      "id": "00000000-0000-0000-0000-000000000006",
      "isEnabled": true,
      "description": "Can receive escrow fund releases",
      "value": "EscrowApp.Seller"
    }
  ]
}
```

### Map App Roles to Claims in Program.cs
```csharp
builder.Services.Configure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme,
    options =>
    {
        options.TokenValidationParameters.RoleClaimType = "roles";
    });
```

## ASP.NET Identity Roles

### Setup with RoleManager
```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Identity options
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
```

### Seed Roles on Startup
```csharp
public static class RoleSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}

// Program.cs — after app.Build()
await RoleSeeder.SeedRolesAsync(app.Services);
```

### Assign Roles to Users
```csharp
public sealed class AssignRoleHandler(
    UserManager<ApplicationUser> userManager,
    IAuthorizationService authService)
    : IRequestHandler<AssignRoleCommand, Result>
{
    public async Task<Result> Handle(
        AssignRoleCommand request,
        CancellationToken cancellationToken)
    {
        // Only admins can assign roles
        var authResult = await authService.AuthorizeAsync(
            request.CurrentUser, null!,
            AuthorizationPolicies.CanManageUsers);

        if (!authResult.Succeeded)
            return Result.Forbidden("Not authorized to manage users.");

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
            return Result.NotFound("User not found.");

        if (!Roles.All.Contains(request.Role))
            return Result.Failure($"Invalid role: {request.Role}");

        var result = await userManager.AddToRoleAsync(user, request.Role);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(result.Errors
                .Select(e => e.Description).ToArray());
    }
}
```

## Mapping Roles to Policies

Prefer policies over direct role checks. Map roles once during startup:

```csharp
builder.Services.AddAuthorizationBuilder()
    // Map role-based access to named policies
    .AddPolicy(AuthorizationPolicies.CanManageUsers, policy => policy
        .RequireRole(Roles.Administrator, Roles.SystemAdministrator))

    .AddPolicy(AuthorizationPolicies.CanReleaseFunds, policy => policy
        .RequireRole(Roles.Agent, Roles.Administrator))

    .AddPolicy(AuthorizationPolicies.CanViewAuditLogs, policy => policy
        .RequireRole(Roles.Auditor, Roles.Administrator,
            Roles.SystemAdministrator))

    .AddPolicy(AuthorizationPolicies.IsSystemAdmin, policy => policy
        .RequireRole(Roles.SystemAdministrator));
```

### Why Policies Over Role Checks

```csharp
// BAD — role name scattered throughout code, hard to refactor
[Authorize(Roles = "Admin,Agent")]
public IActionResult ReleaseFunds() { }

// GOOD — policy encapsulates the requirement, single place to change
[Authorize(Policy = AuthorizationPolicies.CanReleaseFunds)]
public IActionResult ReleaseFunds() { }
```

## Avoiding Role Explosion

Instead of creating granular roles for every permission combination, use claims and policies:

```csharp
// BAD — role explosion
// "AdminAgent", "AdminViewer", "AgentAuditor", "BuyerSeller", ...

// GOOD — compose with claims
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanViewAndRelease", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("EscrowRole", "Agent", "Admin")
        .RequireClaim("permission", "transaction.release"));
```

## Role Hierarchy Pattern

Implement implicit permissions through a hierarchy:

```csharp
public static class RoleHierarchy
{
    private static readonly Dictionary<string, HashSet<string>> Hierarchy = new()
    {
        [Roles.SystemAdministrator] = [Roles.Administrator, Roles.Agent,
            Roles.Auditor, Roles.Viewer],
        [Roles.Administrator] = [Roles.Agent, Roles.Auditor, Roles.Viewer],
        [Roles.Agent] = [Roles.Viewer],
        [Roles.Auditor] = [Roles.Viewer],
        [Roles.Buyer] = [],
        [Roles.Seller] = [],
        [Roles.Viewer] = []
    };

    public static bool HasImplicitRole(string userRole, string requiredRole)
    {
        if (userRole == requiredRole) return true;

        return Hierarchy.TryGetValue(userRole, out var impliedRoles)
            && impliedRoles.Contains(requiredRole);
    }
}
```
