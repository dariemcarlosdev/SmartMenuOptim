# Policy-Based Authorization

## Centralized Policy Definitions

Eliminate magic strings by defining all policy names in a static class:

```csharp
namespace EscrowApp.Application.Authorization;

public static class AuthorizationPolicies
{
    public const string CanViewTransactions = nameof(CanViewTransactions);
    public const string CanCreateTransaction = nameof(CanCreateTransaction);
    public const string CanReleaseFunds = nameof(CanReleaseFunds);
    public const string CanDisputeTransaction = nameof(CanDisputeTransaction);
    public const string CanManageUsers = nameof(CanManageUsers);
    public const string CanViewAuditLogs = nameof(CanViewAuditLogs);
    public const string IsSystemAdmin = nameof(IsSystemAdmin);
    public const string IsTenantAdmin = nameof(IsTenantAdmin);
}
```

## Program.cs — Policy Registration

```csharp
using EscrowApp.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

builder.Services.AddAuthorizationBuilder()
    // Transaction policies
    .AddPolicy(AuthorizationPolicies.CanViewTransactions, policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("EscrowRole", "Agent", "Admin", "Viewer"))

    .AddPolicy(AuthorizationPolicies.CanCreateTransaction, policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("EscrowRole", "Agent", "Admin"))

    .AddPolicy(AuthorizationPolicies.CanReleaseFunds, policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("EscrowRole", "Agent", "Admin")
        .AddRequirements(new MinimumTenureRequirement(days: 30)))

    .AddPolicy(AuthorizationPolicies.CanDisputeTransaction, policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("EscrowRole", "Agent", "Admin", "Buyer", "Seller"))

    // Admin policies
    .AddPolicy(AuthorizationPolicies.CanManageUsers, policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(Roles.Administrator))

    .AddPolicy(AuthorizationPolicies.CanViewAuditLogs, policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("EscrowRole", "Admin", "Auditor"))

    .AddPolicy(AuthorizationPolicies.IsSystemAdmin, policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(Roles.SystemAdministrator)
        .RequireClaim("tenant_id", "system"))

    .AddPolicy(AuthorizationPolicies.IsTenantAdmin, policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(Roles.Administrator)
        .AddRequirements(new TenantMembershipRequirement()))

    // Default: require authentication on all endpoints
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
```

## Custom Authorization Requirements

### IAuthorizationRequirement
```csharp
public sealed class MinimumTenureRequirement(int days)
    : IAuthorizationRequirement
{
    public int RequiredDays { get; } = days;
}

public sealed class TenantMembershipRequirement
    : IAuthorizationRequirement;
```

### IAuthorizationHandler
```csharp
public sealed class MinimumTenureHandler(
    IUserProfileService userProfileService)
    : AuthorizationHandler<MinimumTenureRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumTenureRequirement requirement)
    {
        var userId = context.User.FindFirst("sub")?.Value;

        if (userId is null)
            return; // Do not call context.Fail() — let other handlers run

        var profile = await userProfileService.GetByIdAsync(userId);

        if (profile is null)
            return;

        var tenure = DateTime.UtcNow - profile.CreatedAt;

        if (tenure.TotalDays >= requirement.RequiredDays)
        {
            context.Succeed(requirement);
        }
    }
}

public sealed class TenantMembershipHandler(
    ITenantService tenantService)
    : AuthorizationHandler<TenantMembershipRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantMembershipRequirement requirement)
    {
        var tenantClaim = context.User.FindFirst("tenant_id")?.Value;

        if (tenantClaim is null)
            return;

        var isMember = await tenantService.IsUserMemberAsync(
            context.User.FindFirst("sub")!.Value, tenantClaim);

        if (isMember)
        {
            context.Succeed(requirement);
        }
    }
}
```

### Register Handlers
```csharp
builder.Services.AddScoped<IAuthorizationHandler, MinimumTenureHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TenantMembershipHandler>();
```

## Applying Policies to Controllers and Endpoints

### Controller-Level
```csharp
[ApiController]
[Route("api/escrow")]
[Authorize(Policy = AuthorizationPolicies.CanViewTransactions)]
public sealed class EscrowController(ISender sender) : ControllerBase
{
    [HttpPost("release/{transactionId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanReleaseFunds)]
    public async Task<IActionResult> ReleaseFunds(
        Guid transactionId, CancellationToken ct)
    {
        var command = new ReleaseEscrowFundsCommand(transactionId, User);
        var result = await sender.Send(command, ct);
        return result.ToActionResult();
    }
}
```

### Minimal API Endpoints
```csharp
app.MapGet("/api/escrow/transactions", async (ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new GetTransactionsQuery(), ct);
    return Results.Ok(result);
})
.RequireAuthorization(AuthorizationPolicies.CanViewTransactions);

app.MapPost("/api/escrow/release/{id:guid}", async (
    Guid id, ISender sender, ClaimsPrincipal user, CancellationToken ct) =>
{
    var command = new ReleaseEscrowFundsCommand(id, user);
    var result = await sender.Send(command, ct);
    return result.ToMinimalApiResult();
})
.RequireAuthorization(AuthorizationPolicies.CanReleaseFunds);
```

## Combining Multiple Requirements

Policies with multiple requirements use AND logic — all must succeed:

```csharp
.AddPolicy("CanApproveHighValueRelease", policy => policy
    .RequireAuthenticatedUser()
    .RequireClaim("EscrowRole", "Admin")
    .AddRequirements(new MinimumTenureRequirement(days: 90))
    .AddRequirements(new TwoFactorRequirement()))
```

For OR logic, use a single handler that checks multiple conditions:

```csharp
public sealed class EscrowRoleOrAdminHandler
    : AuthorizationHandler<EscrowRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EscrowRoleRequirement requirement)
    {
        if (context.User.IsInRole(Roles.SystemAdministrator) ||
            context.User.HasClaim("EscrowRole", requirement.Role))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```
