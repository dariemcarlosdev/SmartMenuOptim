# Permission System Design

## Overview
This document explains the design rationale for using a dedicated Permission System alongside ASP.NET Core Identity in the Smart Menu Optimizer application.

## Why a Custom Permission System?

### 1. Identity vs. Application Permissions
- **Identity Framework** provides:
  - Authentication and basic role-based authorization
  - User management and sign-in flows
  - Role management

  ```csharp
  // Basic Identity role-based authorization
  [Authorize(Roles = "RestaurantManager")]
  public class MenuController : Controller
  {
      // Limited to role-based checks
  }
  ```

- **Custom Permission System** enables:
  - Fine-grained, dynamic application permissions
  - Business-specific permission requirements beyond basic roles
  - Per-tenant permission scoping
  - Complex permission hierarchies

  ```csharp
  // Fine-grained permission check using a custom policy
  [Authorize(Policy = "EditMenuPolicy")]
  public async Task<IActionResult> EditMenu(int restaurantId, MenuDto menu)
  {
      // Only users with specific menu edit permission for this restaurant
  }
  ```

### 2. Multi-tenant Requirements
- Each restaurant (tenant) needs its own permission sets
- Staff members need different permissions in different restaurants
- Permissions must respect tenant boundaries
- Cross-tenant permissions for admin users

```csharp
public class TenantAwarePermissionRequirement : IAuthorizationRequirement
{
    public string PermissionName { get; }

    public TenantAwarePermissionRequirement(string permissionName)
    {
        PermissionName = permissionName;
    }
}

public class PermissionHandler : AuthorizationHandler<TenantAwarePermissionRequirement>
{
    private readonly IUserPermissionService _permissionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionHandler(IUserPermissionService permissionService, IHttpContextAccessor httpContextAccessor)
    {
        _permissionService = permissionService;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantAwarePermissionRequirement requirement)
    {
        var user = context.User;
        // Resolve tenant from HttpContext, set by a middleware
        var tenantIdString = _httpContextAccessor.HttpContext?.Items["TenantId"]?.ToString();
        if (!int.TryParse(tenantIdString, out var tenantId))
        {
            context.Fail();
            return;
        }

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            context.Fail();
            return;
        }

        var hasPermission = await _permissionService.UserHasPermissionAsync(
            userId,
            requirement.PermissionName,
            tenantId);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
```

### 3. Profile-Specific Permissions
- Different permission sets based on user profile type:
  - Admin users need system-wide permissions
  - Staff members need restaurant-specific permissions
  - Customers need limited, focused permissions
- Permission inheritance and delegation based on profile hierarchy

```csharp
public enum ProfileType
{
    Admin,
    Staff,
    Customer
}

public class ProfilePermissions
{
    public static readonly Dictionary<ProfileType, List<string>> DefaultPermissions = new()
    {
        {
            ProfileType.Admin, new List<string>
            {
                "system.access",
                "restaurant.manage",
                "menu.full",
                "staff.manage"
            }
        },
        {
            ProfileType.Staff, new List<string>
            {
                "menu.read",
                "menu.edit",
                "orders.manage"
            }
        },
        {
            ProfileType.Customer, new List<string>
            {
                "menu.read",
                "orders.create",
                "reviews.submit"
            }
        }
    };
}
```

### 4. Flexible Permission Assignment
- Dynamic permission updates without code changes
- Time-based permissions (temporary access)
- Conditional permissions based on business rules
- Granular access control at feature level
- Support for permission groups and templates

Example of dynamic permission management:

```csharp
public class DynamicPermissionService
{
    public async Task AssignTemporaryPermissionAsync(
        string userId, 
        string permissionName, 
        int restaurantId, 
        TimeSpan duration)
    {
        var permission = new UserPermission
        {
            ApplicationUserId = userId,
            Name = permissionName,
            RestaurantId = restaurantId,
            ExpiresAt = DateTime.UtcNow.Add(duration),
            GrantedBy = "system"
        };

        await _permissionRepository.AddAsync(permission);
    }

    public async Task<bool> ValidatePermissionAsync(UserPermission permission)
    {
        return permission.IsActive && 
               (!permission.ExpiresAt.HasValue || 
                permission.ExpiresAt.Value > DateTime.UtcNow);
    }
}
```

### 5. Profile-Specific Access Control
- AdminUser permissions for system management
- StaffMember permissions for restaurant operations
- Customer permissions for ordering and reviews
- Permission inheritance within profile types

Example implementation for different profiles:

```csharp
public class StaffPermissionService
{
    public async Task<bool> CanAccessKitchenSystem(int staffId, int restaurantId)
    {
        var staff = await _staffRepository.GetByIdAsync(staffId);
        return staff.Role == StaffRole.Chef && 
               await HasPermissionAsync(staff.Id, "kitchen.access", restaurantId);
    }

    public async Task<bool> CanManageOrders(int staffId, int restaurantId)
    {
        var staff = await _staffRepository.GetByIdAsync(staffId);
        return (staff.Role == StaffRole.Waiter || staff.Role == StaffRole.Manager) && 
               await HasPermissionAsync(staff.Id, "orders.manage", restaurantId);
    }
}
```

### 6. Business Rule Integration
- Permissions tied to business metrics and KPIs
- Rule-based permission automation
- Integration with workflow systems
- Audit trails for permission changes

Example of permission integration with business rules:

```csharp
public class BusinessRulePermissionService
{
    public async Task ApplyBusinessRules(int restaurantId)
    {
        var rules = await _businessRuleRepository.GetActiveRulesAsync(restaurantId);
        
        foreach (var rule in rules)
        {
            switch (rule.RuleType)
            {
                case RuleType.StaffPermissionThreshold:
                    await ApplyStaffPermissionRule(rule, restaurantId);
                    break;
                case RuleType.CustomerLoyaltyPermission:
                    await ApplyCustomerLoyaltyRule(rule, restaurantId);
                    break;
            }
        }
    }

    private async Task ApplyStaffPermissionRule(BusinessRule rule, int restaurantId)
    {
        var staffMembers = await _staffRepository.GetAllActiveAsync(restaurantId);
        
        foreach (var staff in staffMembers)
        {
            if (staff.CompletedShifts >= rule.Value)
            {
                await _permissionService.GrantPermissionAsync(
                    staff.ApplicationUserId,
                    "orders.approve",
                    restaurantId);
            }
        }
    }
}
```

## Implementation Benefits

This approach:
- **Complements Identity Framework** rather than replacing it
  - Builds on top of ASP.NET Core Identity
  - Leverages existing authentication
  - Extends authorization capabilities
  
- **Provides Business-Specific Authorization**
  - Fine-grained control over features
  - Dynamic permission management
  - Flexible permission rules
  
- **Supports Multi-tenant Permission Management**
- **Enables Dynamic Permission Updates**
  - Runtime permission changes
  - No code deployment needed
  - Business rule automation
  
- **Allows for Audit Trails**
  - Permission history tracking
  - Change logging
  - Compliance support
  
- **Integrates with Profile System**
  - Profile-specific permissions
  - Hierarchical inheritance
  - Clean separation of concerns

### 1. Integration with Blazor Components
Define policies in `Program.cs` and use them in Blazor components for cleaner, declarative authorization.

**Policy Definition (`Program.cs`):**
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EditMenuPolicy", policy =>
        policy.Requirements.Add(new TenantAwarePermissionRequirement("menu.edit")));
    options.AddPolicy("ViewAnalyticsPolicy", policy =>
        policy.Requirements.Add(new TenantAwarePermissionRequirement("analytics.view")));
});
```

**Blazor Component Usage:**
```razor
@* Use the policy directly in the AuthorizeView component *@
<AuthorizeView Policy="EditMenuPolicy">
    <Authorized>
        <button class="btn btn-primary">Edit Menu</button>
    </Authorized>
    <NotAuthorized>
        <p>You are not authorized to edit menus.</p>
    </NotAuthorized>
</AuthorizeView>
```

### 2. API Endpoint Protection
The `[Authorize(Policy = "PolicyName")]` attribute is the standard way to protect API endpoints.

**API Controller Usage:**
```csharp
[ApiController]
[Route("api/menu")]
public class MenuController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "EditMenuPolicy")] // Using the defined policy
    public async Task<IActionResult> AddDish([FromBody] DishDto dish)
    {
        // Logic to add a dish
        return Ok();
    }
}
```

### 3. Audit Trail Implementation
Example of permission audit logging:

```csharp
public class PermissionAuditService
{
    public async Task LogPermissionChange(
        UserPermission permission, 
        string action, 
        string performedBy)
    {
        var audit = new PermissionAudit
        {
            PermissionId = permission.Id,
            Action = action,
            PerformedBy = performedBy,
            Timestamp = DateTime.UtcNow,
            Details = System.Text.Json.JsonSerializer.Serialize(new
            {
                permission.Name,
                permission.RestaurantId,
                permission.ApplicationUserId,
                permission.ExpiresAt
            })
        };

        await _auditRepository.AddAsync(audit);
    }
}
```

## Best Practices

1. **Permission Granularity**
   - Keep permissions focused and specific
   - Use permission groups for common combinations
   - Avoid overly complex permission hierarchies

2. **Performance Considerations**
   - Cache frequently-used permissions
   - Optimize permission checks
   - Use efficient queries for permission lookups

3. **Security Guidelines**
   - Always validate tenant context
   - Implement proper permission checks
   - Maintain audit trails
   - Regular permission reviews

4. **Maintenance**
   - Document permission changes
   - Regular cleanup of expired permissions
   - Monitor permission usage patterns

### 1. Always Validate Tenant Context
```csharp
public class TenantContextValidator
{
    public async Task ValidateContext(int restaurantId, string userId)
    {
        var userRestaurants = await _userRestaurantService
            .GetUserRestaurantsAsync(userId);
        
        if (!userRestaurants.Contains(restaurantId))
        {
            throw new UnauthorizedTenantAccessException(
                "User does not have access to this restaurant");
        }
    }
}
```

### 2. Cache Frequently Used Permissions
```csharp
public class CachedPermissionService : IUserPermissionService
{
    private readonly IMemoryCache _cache;
    private readonly UserPermissionService _decorated; // The actual service

    public CachedPermissionService(IMemoryCache cache, UserPermissionService decorated)
    {
        _cache = cache;
        _decorated = decorated;
    }

    public async Task<bool> UserHasPermissionAsync(string userId, string permission, int restaurantId)
    {
        var cacheKey = $"perm_{userId}_{permission}_{restaurantId}";
        
        return await _cache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(5);
                return await _decorated.UserHasPermissionAsync(userId, permission, restaurantId);
            });
    }
}
```

### 3. Regular Permission Cleanup
```csharp
public class PermissionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public PermissionCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var permissionRepo = scope.ServiceProvider.GetRequiredService<IRepository<UserPermission>>();
                var auditService = scope.ServiceProvider.GetRequiredService<IPermissionAuditService>();
                var unityOfWork = scope.ServiceProvider.GetRequiredService<IUnityOfWork>();

                var expired = await permissionRepo.Query()
                    .Where(p => p.ExpiresAt.HasValue && p.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync(stoppingToken);
                
                if(expired.Any())
                {
                    foreach (var permission in expired)
                    {
                        permissionRepo.Delete(permission);
                        await auditService.LogPermissionChange(permission, "Expired", "System");
                    }
                    await unityOfWork.SaveChangesAsync();
                }
            }
            
            // Wait for 24 hours before running again
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
```

This implementation provides:
- Clear separation of concerns
- Strong typing and compile-time checks
- Integration with existing ASP.NET Core features
- Scalable and maintainable permission management
- Comprehensive audit trail capabilities
- Efficient caching and cleanup mechanisms

For more details on specific implementations, refer to the related service and entity documentation.