# Claims Transformation

## Overview

Claims transformation enriches the `ClaimsPrincipal` with application-specific claims after authentication but before authorization. This bridges the gap between identity provider claims (Entra ID, IdentityServer) and application-level permissions.

## IClaimsTransformation Implementation

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

public sealed class EscrowClaimsTransformation(
    IUserPermissionService permissionService,
    IMemoryCache cache,
    ILogger<EscrowClaimsTransformation> logger)
    : IClaimsTransformation
{
    private const int CacheDurationMinutes = 15;

    public async Task<ClaimsPrincipal> TransformAsync(
        ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        var userId = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId is null)
            return principal;

        // Avoid re-transforming if already enriched
        if (principal.HasClaim("escrow:transformed", "true"))
            return principal;

        var cacheKey = $"claims:{userId}";

        if (!cache.TryGetValue(cacheKey, out List<Claim>? additionalClaims))
        {
            additionalClaims = await LoadUserClaimsAsync(userId);
            cache.Set(cacheKey, additionalClaims,
                TimeSpan.FromMinutes(CacheDurationMinutes));
        }

        var identity = new ClaimsIdentity(additionalClaims);
        identity.AddClaim(new Claim("escrow:transformed", "true"));
        principal.AddIdentity(identity);

        logger.LogDebug(
            "Claims transformed for user {UserId}: added {Count} claims",
            userId, additionalClaims!.Count);

        return principal;
    }

    private async Task<List<Claim>> LoadUserClaimsAsync(string userId)
    {
        var claims = new List<Claim>();
        var permissions = await permissionService
            .GetPermissionsAsync(userId);

        if (permissions is null)
            return claims;

        // Map EscrowRole from database
        if (permissions.EscrowRole is not null)
            claims.Add(new Claim("EscrowRole", permissions.EscrowRole));

        // Map tenant
        if (permissions.TenantId is not null)
            claims.Add(new Claim("tenant_id", permissions.TenantId));

        // Map granular permissions
        foreach (var permission in permissions.Permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        // Map transaction limits
        if (permissions.MaxTransactionAmount.HasValue)
        {
            claims.Add(new Claim("max_transaction_amount",
                permissions.MaxTransactionAmount.Value.ToString("F2")));
        }

        return claims;
    }
}
```

### Register in Program.cs
```csharp
builder.Services
    .AddScoped<IClaimsTransformation, EscrowClaimsTransformation>();
```

> **Note:** `IClaimsTransformation.TransformAsync` is called on every request. Use caching to avoid database hits on every call.

## Mapping Entra ID App Roles to Claims

Entra ID App Roles are delivered in the `roles` claim of the JWT. Map them to application-specific claims:

```csharp
public sealed class EntraIdRoleMappingTransformation(
    ILogger<EntraIdRoleMappingTransformation> logger)
    : IClaimsTransformation
{
    // Map Entra ID App Role names to EscrowRole claim values
    private static readonly Dictionary<string, string> RoleMapping = new()
    {
        ["EscrowApp.Admin"] = "Admin",
        ["EscrowApp.Agent"] = "Agent",
        ["EscrowApp.Viewer"] = "Viewer",
        ["EscrowApp.Auditor"] = "Auditor",
        ["EscrowApp.Buyer"] = "Buyer",
        ["EscrowApp.Seller"] = "Seller"
    };

    public Task<ClaimsPrincipal> TransformAsync(
        ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return Task.FromResult(principal);

        if (principal.HasClaim("escrow:roles_mapped", "true"))
            return Task.FromResult(principal);

        var roleClaims = principal.FindAll("roles")
            .Concat(principal.FindAll(ClaimTypes.Role))
            .ToList();

        var additionalClaims = new List<Claim>();

        foreach (var roleClaim in roleClaims)
        {
            if (RoleMapping.TryGetValue(
                    roleClaim.Value, out var escrowRole))
            {
                additionalClaims.Add(
                    new Claim("EscrowRole", escrowRole));

                logger.LogDebug(
                    "Mapped Entra ID role '{EntraRole}' to EscrowRole '{EscrowRole}'",
                    roleClaim.Value, escrowRole);
            }
        }

        if (additionalClaims.Count > 0)
        {
            additionalClaims.Add(
                new Claim("escrow:roles_mapped", "true"));
            var identity = new ClaimsIdentity(additionalClaims);
            principal.AddIdentity(identity);
        }

        return Task.FromResult(principal);
    }
}
```

## ClaimsPrincipal Extension Methods

Clean, typed access to claims used throughout the application:

```csharp
namespace EscrowApp.Application.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal)
        => principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException(
                "User ID claim not found.");

    public static string GetEmail(this ClaimsPrincipal principal)
        => principal.FindFirst("email")?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value
            ?? throw new InvalidOperationException(
                "Email claim not found.");

    public static string? GetTenantId(this ClaimsPrincipal principal)
        => principal.FindFirst("tenant_id")?.Value;

    public static string? GetEscrowRole(this ClaimsPrincipal principal)
        => principal.FindFirst("EscrowRole")?.Value;

    public static bool HasPermission(
        this ClaimsPrincipal principal, string permission)
        => principal.HasClaim("permission", permission);

    public static decimal? GetMaxTransactionAmount(
        this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("max_transaction_amount")?.Value;
        return claim is not null ? decimal.Parse(claim) : null;
    }

    public static bool IsInEscrowRole(
        this ClaimsPrincipal principal, params string[] roles)
        => principal.FindAll("EscrowRole")
            .Any(c => roles.Contains(c.Value));
}
```

## Chaining Multiple Transformations

Register transformations in order — they execute sequentially:

```csharp
// Order matters: map roles first, then add app permissions
builder.Services
    .AddScoped<IClaimsTransformation, EntraIdRoleMappingTransformation>();

// To chain, use a composite:
public sealed class CompositeClaimsTransformation(
    IEnumerable<IClaimsTransformer> transformers)
    : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(
        ClaimsPrincipal principal)
    {
        foreach (var transformer in transformers)
        {
            principal = await transformer.TransformAsync(principal);
        }
        return principal;
    }
}

public interface IClaimsTransformer
{
    Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal);
}

// Register individual transformers
builder.Services.AddScoped<IClaimsTransformer, EntraIdRoleMapper>();
builder.Services.AddScoped<IClaimsTransformer, DatabasePermissionMapper>();
builder.Services.AddScoped<IClaimsTransformation, CompositeClaimsTransformation>();
```

## Cache Invalidation

Invalidate cached claims when permissions change:

```csharp
public sealed class PermissionChangedHandler(
    IMemoryCache cache)
    : INotificationHandler<PermissionChangedEvent>
{
    public Task Handle(
        PermissionChangedEvent notification,
        CancellationToken cancellationToken)
    {
        cache.Remove($"claims:{notification.UserId}");
        return Task.CompletedTask;
    }
}
```
