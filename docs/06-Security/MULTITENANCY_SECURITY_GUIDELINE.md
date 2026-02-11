# Multi-Tenancy Security Guidelines for SmartMenuOptim

## Overview

This document provides security best practices for the SmartMenuOptim multi-tenant architecture, where each **Restaurant** represents an isolated tenant.

**Multi-Tenancy Model:**
- **Tenant Entity:** `Restaurant`
- **Tenant Identifier:** `RestaurantId` (integer)
- **Isolation Strategy:** Database-level query filters + application-level validation
- **User-Tenant Relationship:** Users can belong to one or multiple restaurants via `ApplicationUser.RestaurantTenantId` and permission assignments

**Document Structure:**
1. **Implementation Status** - Current state and priorities
2. **Architecture Overview** - Core concepts and design
3. **Implementation Guide** - Step-by-step technical details
4. **Testing & Compliance** - Validation and standards
5. **Operations** - Monitoring, incidents, and maintenance

---

## 1. Implementation Status & Action Plan

### 1.1 Current State (42% Complete)

**✅ Implemented:**
- Soft-delete filters, audit logging, ASP.NET Core Identity
- Rate limiting (100 req/min), CORS, HTTPS redirection
- Azure OpenAI + Text Analytics integration
- Sentry monitoring, `TenantResolverMiddleware` created (⚠️ NOT registered)

**⚠️ Partial:**
- Input validation (Data Annotations only, no FluentValidation)
- Key Vault (Blazor Server only, NOT in API)

**❌ Critical Gaps:**

| Item | Impact | Effort |
|------|--------|--------|
| **Register TenantResolverMiddleware** | 🔴 Critical | 5 min |
| **TenantAuthorizationHandler** | 🔴 Critical | 2-3 hrs |
| **Enable EF tenant query filters** | 🔴 Critical | 3-4 hrs |
| **FluentValidation** | 🟡 High | 1-2 hrs |
| **Azure deployment** | 🟡 Medium | 4-6 hrs |

### 1.2 Security Risks

| Risk | Severity | Details |
|------|----------|---------|
| **No automatic tenant filtering** | 🔴 Critical | Manual `.Where()` in every controller—high data leak risk |
| **Middleware not registered** | 🔴 Critical | Tenant context never set |
| **No authorization policy** | 🟡 High | No tenant access enforcement |

### 1.3 Action Plan

**Phase 1 (Critical - 6-8 hrs):**
1. Register `TenantResolverMiddleware` in `Program.cs`
2. Create `Authorization/TenantAuthorizationHandler.cs` + `TenantAccessRequirement.cs`
3. Implement `ITenantContextAccessor`, enable EF query filters
4. Add FluentValidation + DTO validators

**Phase 2 (Testing - 3-4 hrs):**
5. Write tenant isolation integration tests

**Phase 3 (Azure - 4-6 hrs):**
6. Provision: PostgreSQL, App Services, Key Vault (see `AZURE-SETUP-GUIDE.md`)
7. Configure Managed Identity, Application Insights
8. Deploy and verify

---

## 2. Architecture Overview

### 1.1 Data Isolation Strategy

**Entity Design:**
```csharp
// Base class for tenant-specific entities
public abstract class TenantEntityBase : EntityBase
{
    public int RestaurantId { get; set; }
    public virtual Restaurant Restaurant { get; set; } = null!;
}

// Example: Orders are scoped to restaurants
public class Order : TenantEntityBase
{
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    // ... other properties
}
```

### 1.2 EF Core Query Filters

**Global query filters automatically enforce tenant boundaries:**

```csharp
// In AppDbContext.OnModelCreating()
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Apply soft-delete filter globally
    modelBuilder.Entity<Dish>()
        .HasQueryFilter(d => !d.IsDeleted);
    
    modelBuilder.Entity<Order>()
        .HasQueryFilter(o => !o.IsDeleted);
    
    // Restaurant-level query filter (commented in AppDbContext for manual control)
    // Uncomment when implementing automatic tenant filtering:
    // modelBuilder.Entity<Dish>()
    //     .HasQueryFilter(d => d.RestaurantId == _currentRestaurantTenantId);
}
```

**Current Implementation:**
- Soft-delete filters: ✅ Active
- Tenant filters: Manual via `Where()` clauses in controllers/repositories
- See: `SmartMenuOptim.Shared\Data\Context\AppDbContext.cs`

### 1.3 Tenant Resolution

**Middleware:** `TenantResolverMiddleware` (SmartMenuOptim.Infrastructure)

```csharp
// Extracts tenant from:
// 1. Header: X-Tenant-ID
// 2. Query string: ?tenantId=123
// 3. Subdomain: tenant1.example.com

public async Task InvokeAsync(HttpContext context)
{
    string? tenantId = null;

    // Try header first
    if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var headerTenant))
    {
        tenantId = headerTenant.FirstOrDefault();
    }

    // Fallback to query string
    if (string.IsNullOrEmpty(tenantId))
    {
        tenantId = context.Request.Query["tenantId"].FirstOrDefault();
    }

    // Store in HttpContext for downstream use
    if (!string.IsNullOrEmpty(tenantId))
    {
        context.Items["TenantId"] = tenantId;
        await _next(context);
    }
    else
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Tenant ID is required.");
    }
}
```

**Registration:**
```csharp
// In Program.cs
app.UseMiddleware<TenantResolverMiddleware>();
```

---

## 3. Data Access & Repository Pattern

### 3.1 Repository Implementation

**SmartMenuOptim uses `IRepository<T>` with `IUnityOfWork`:**

```csharp
// SmartMenuOptim.Shared\Data\Interfaces\IRepository.cs
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[]? includes);
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[]? includes);
    IQueryable<T> Query(); // For advanced LINQ operations
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<bool> ExistsAsync(int id);
}

// SmartMenuOptim.Shared\Data\Repositories\Repository.cs
public class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public IQueryable<T> Query() => _dbSet.AsQueryable();
    
    // ... other implementations
}
```

### 3.2 Controller Usage Pattern

**Manual tenant filtering in controllers:**

```csharp
// From SmartMenuOptim.API\Controllers\AiController.cs
[HttpGet("underperforming")]
public async Task<ActionResult<List<UnderperformingDishDTO>>> GetUnderperformingDishes(
    [FromQuery] DateTime? startDate = null)
{
    // Get current restaurant from context (manual extraction)
    var restaurantId = GetRestaurantIdFromContext();

    // Use Query() for advanced filtering
    var saleRecords = await _unityOfWork.SaleRecords.Query()
        .AsNoTracking()
        .Where(sr => sr.RestaurantId == restaurantId) // Explicit tenant filter
        .Where(sr => sr.SaleDate >= startDate)
        .Include(sr => sr.Dish)
        .ToListAsync();

    return Ok(saleRecords);
}
```

### 3.3 Best Practices for Tenant-Specific Queries

**Critical Rules:**
- ✅ **Always filter by `RestaurantId`** as the first `.Where()` clause
- ✅ Use `AsNoTracking()` for read-only queries (improves performance)
- ✅ Apply tenant filtering **before** other complex filters
- ❌ **Never** query tenant-specific entities without `RestaurantId` filter

**Correct Examples:**

```csharp
// ✅ CORRECT: Tenant-specific query
public async Task<List<Dish>> GetDishesByRestaurantAsync(int restaurantId) 
{ 
    return await _context.Dishes 
        .Where(d => d.RestaurantId == restaurantId) // Tenant filter FIRST
        .Where(d => !d.IsDeleted) // Then other filters
        .ToListAsync(); 
}

// ✅ CORRECT: Global entity with authorization check
public async Task<AdminUser?> GetAdminUserAsync(int userId, ClaimsPrincipal user) 
{ 
    // Verify user is authorized to access admin data
    if (!user.IsInRole("SystemAdmin")) 
        throw new UnauthorizedAccessException();
    
    return await _context.AdminUsers
        .FirstOrDefaultAsync(a => a.Id == userId); 
}
```

**Anti-Patterns to Avoid:**

```csharp
// ❌ CRITICAL RISK: Global query without tenant filter
public async Task<List<Dish>> GetAllDishesAsync() 
{ 
    return await _context.Dishes.ToListAsync(); // Exposes all tenants' data!
}

// ❌ WRONG: Tenant filter applied too late
public async Task<List<Order>> GetRecentOrdersAsync(int restaurantId)
{
    return await _context.Orders
        .Include(o => o.OrderItems) // Heavy join before filtering
        .Where(o => o.RestaurantId == restaurantId) // Filter should be first!
        .ToListAsync();
}
```

---

## 4. Authentication & Authorization

### 4.1 ASP.NET Core Identity Integration

**User-Tenant Association:**

```csharp
// SmartMenuOptim.Shared\Data\Entities\GlobalEntities\ApplicationUser.cs
public class ApplicationUser : IdentityUser
{
    public ProfileType ProfileType { get; set; } // Admin, Customer, Staff
    public int? ProfileId { get; set; }
    public int? RestaurantTenantId { get; set; } // Primary restaurant

    // Navigation properties
    public AdminUser? AdminProfile { get; set; }
    public Customer? CustomerProfile { get; set; }
    public StaffMember? StaffProfile { get; set; }
    public ICollection<UserPermission> PermissionsAssigment { get; set; }
}
```

### 4.2 Permission System

**UserPermission Entity (SmartMenuOptim.Shared):**

```csharp
public class UserPermission : TenantEntityBase
{
    public string ApplicationUserId { get; set; } = null!;
    public ApplicationUser ApplicationUser { get; set; } = null!;
    
    public string Name { get; set; } = null!; // e.g., "Dishes.Read"
    public string? Description { get; set; }
    public string? Area { get; set; } // e.g., "Menu Management"
    public AccessLevel AccessLevel { get; set; } // Read, Write, Delete, Admin
    
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public string? GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; }
}
```

**See:** `docs\security\Permission-System-Design.md` for detailed permission implementation

### 4.3 Authorization Handler (Recommended Implementation)

**Create authorization handler for tenant-specific access:**

```csharp
// Step 1: Create requirement and handler
public class TenantAccessRequirement : IAuthorizationRequirement 
{
    public int RestaurantId { get; }
    public TenantAccessRequirement(int restaurantId) => RestaurantId = restaurantId;
}

public class TenantAuthorizationHandler : AuthorizationHandler<TenantAccessRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantAuthorizationHandler> _logger;

    public TenantAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor, 
        ILogger<TenantAuthorizationHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        TenantAccessRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("No HttpContext available for tenant authorization");
            return Task.CompletedTask;
        }

        // Extract tenant from middleware
        if (!httpContext.Items.TryGetValue("TenantId", out var tenantIdObj) || 
            !(tenantIdObj is string tenantIdStr) || 
            !int.TryParse(tenantIdStr, out var tenantId))
        {
            _logger.LogWarning("TenantId missing or invalid in HttpContext");
            return Task.CompletedTask;
        }

        // Get user's restaurant associations
        var userRestaurantIds = GetUserRestaurantIds(context.User);

        if (userRestaurantIds.Contains(tenantId))
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("User not authorized for restaurant {RestaurantId}", tenantId);
        }

        return Task.CompletedTask;
    }

    private static List<int> GetUserRestaurantIds(ClaimsPrincipal user)
    {
        var claimValue = user?.FindFirst("restaurant_ids")?.Value;
        if (string.IsNullOrWhiteSpace(claimValue)) return new List<int>();

        return claimValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();
    }
}

// Step 2: Register in Program.cs (SmartMenuOptim.API)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TenantAccess", policy =>
        policy.Requirements.Add(new TenantAccessRequirement(0))); // Dynamic per request
});
builder.Services.AddScoped<IAuthorizationHandler, TenantAuthorizationHandler>();

// Step 3: Use in controllers
[Authorize(Policy = "TenantAccess")]
[HttpGet("api/dishes/{id}")]
public async Task<ActionResult<Dish>> GetDish(int id)
{
    var restaurantId = int.Parse(HttpContext.Items["TenantId"]!.ToString()!);
    var dish = await _unityOfWork.Dishes.Query()
        .FirstOrDefaultAsync(d => d.Id == id && d.RestaurantId == restaurantId);
    
    return dish == null ? NotFound() : Ok(dish);
}
```

---

## 5. API Security Controls

### 5.1 Rate Limiting

**Current Implementation (SmartMenuOptim.API):**

```csharp
// In ServiceCollectionExtensions.cs
public static IServiceCollection AddRateLimiting(this IServiceCollection services)
{
    services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("FixedPolicy", policy =>
        {
            policy.Window = TimeSpan.FromMinutes(1);
            policy.PermitLimit = 100; // 100 requests per minute
            policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            policy.QueueLimit = 10;
        });
    });
    return services;
}

// In Program.cs
app.UseRateLimiter();
```

### 5.2 CORS Configuration

```csharp
// In appsettings.json
{
  "Cors": {
    "AllowedOrigins": [
      "https://localhost:7060",
      "https://smartmenu-server.azurewebsites.net"
    ]
  }
}

// In ServiceCollectionExtensions.cs
builder.Services.AddCustomCorsPolicy(builder.Configuration);

// In Program.cs
app.UseCors("_myAllowSpecificOrigins");
```

### 5.3 Input Validation

**Use Data Annotations and FluentValidation:**

```csharp
public class CreateDishRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = null!;

    [Range(0.01, 10000)]
    public decimal Price { get; set; }

    [Required]
    public int RestaurantId { get; set; } // Validated against user's tenant
}
```

---

## 6. Azure Security Integration

### 6.1 Azure Key Vault

**Configuration (SmartMenuOptim.Server):**

```csharp
// In Program.cs
builder.AddKeyVaultConfiguration();

// Extension method implementation
public static WebApplicationBuilder AddKeyVaultConfiguration(this WebApplicationBuilder builder)
{
    var keyVaultName = builder.Configuration["KeyVaultName"];
    if (!string.IsNullOrEmpty(keyVaultName))
    {
        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
    }
    return builder;
}
```

**Secrets Stored:**
- `ConnectionStrings--DefaultConnection`
- `Azure--OpenAI--Key`
- `Azure--TextAnalytics--Key`
- `BackendApi--BaseUrl`

### 6.2 Managed Identity

**Enable for App Services:**

```bash
# Azure CLI
az webapp identity assign \
  --resource-group rg-smartmenu-prod \
  --name smartmenu-api-prod

# Grant Key Vault access
az keyvault set-policy \
  --name smartmenu-kv-prod \
  --object-id <app-identity-object-id> \
  --secret-permissions get list
```

### 6.3 PostgreSQL Security

**Firewall Configuration:**

```bash
# Allow Azure services
az postgres flexible-server firewall-rule create \
  --resource-group rg-smartmenu-prod \
  --name smartmenu-db-prod \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

**Connection String Security:**
- ✅ Stored in Azure Key Vault
- ✅ SSL Mode required
- ✅ No hardcoded credentials

---

## 7. Monitoring & Audit Logging

### 7.1 Audit Fields (EntityBase)

**All entities inherit automatic auditing:**

```csharp
public abstract class EntityBase
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

// Automatically managed in AppDbContext.SaveChangesAsync()
private void SetAuditProperties()
{
    var now = DateTime.UtcNow;
    var entries = ChangeTracker.Entries<EntityBase>()
        .Where(e => e.State == EntityState.Added || 
                    e.State == EntityState.Modified || 
                    e.State == EntityState.Deleted);

    foreach (var entry in entries)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                entry.Entity.IsDeleted = false;
                break;
            case EntityState.Modified:
                entry.Entity.UpdatedAt = now;
                break;
            case EntityState.Deleted:
                entry.State = EntityState.Modified; // Soft delete
                entry.Entity.IsDeleted = true;
                entry.Entity.UpdatedAt = now;
                break;
        }
    }
}
```

### 7.2 Application Monitoring

**Sentry Integration (SmartMenuOptim.API):**

```csharp
// In Program.cs
builder.WebHost.UseSentry(options =>
{
    options.Dsn = "https://<key>@o<org>.ingest.us.sentry.io/<project>";
    options.MaxBreadcrumbs = 50;
    options.TracesSampleRate = 1.0;
});
```

**Azure Application Insights (Recommended):**

```bash
# Add to App Services
az webapp config appsettings set \
  --name smartmenu-api-prod \
  --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=<key>"
```

---

## 8. Blazor Security Implementation

### 8.1 Authentication Configuration

**Configure authentication in `SmartMenuOptim.Server/Program.cs`:**

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc"; // Or your identity provider
});

builder.Services.AddAuthorization(options =>
{
    // Tenant-based policies
    options.AddPolicy("RestaurantUser", policy =>
        policy.RequireClaim("RestaurantId"));
    
    options.AddPolicy("RestaurantAdmin", policy =>
        policy.RequireClaim("RestaurantId")
              .RequireClaim("RestaurantRole", "Admin"));
    
    options.AddPolicy("RestaurantStaff", policy =>
        policy.RequireClaim("RestaurantId")
              .RequireClaim("ProfileType", "Staff"));
});
```

### 8.2 Claims Extensions Helper

**Create `SmartMenuOptim.Server/Extensions/ClaimsPrincipalExtensions.cs`:**

```csharp
using System.Security.Claims;

namespace SmartMenuOptim.Server.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the RestaurantId from the user's claims.
    /// Returns 0 if claim is missing or invalid.
    /// </summary>
    public static int GetRestaurantId(this ClaimsPrincipal principal)
    {
        var restaurantIdValue = principal.FindFirst("RestaurantId")?.Value;
        return int.TryParse(restaurantIdValue, out var restaurantId) ? restaurantId : 0;
    }

    /// <summary>
    /// Checks if the user has admin role for their restaurant.
    /// </summary>
    public static bool IsRestaurantAdmin(this ClaimsPrincipal principal)
    {
        return principal.HasClaim("RestaurantRole", "Admin");
    }

    /// <summary>
    /// Checks if the user is a staff member.
    /// </summary>
    public static bool IsStaffMember(this ClaimsPrincipal principal)
    {
        return principal.HasClaim("ProfileType", "Staff");
    }

    /// <summary>
    /// Gets the user's profile type (Admin, Staff, Customer).
    /// </summary>
    public static string? GetProfileType(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("ProfileType")?.Value;
    }
}
```

### 8.3 Component-Level Authorization

**Declarative authorization with `AuthorizeView`:**

```razor
@* Example: RestaurantSettings.razor *@
@page "/restaurant/settings"
@using Microsoft.AspNetCore.Authorization
@using SmartMenuOptim.Server.Extensions
@attribute [Authorize(Policy = "RestaurantAdmin")]

<AuthorizeView Policy="RestaurantAdmin">
    <Authorized>
        <h3>Restaurant Settings</h3>
        <p>Restaurant ID: @context.User.GetRestaurantId()</p>
        
        <RestaurantSettingsForm RestaurantId="@context.User.GetRestaurantId()" />
    </Authorized>
    <NotAuthorized>
        <div class="alert alert-danger">
            <strong>Access Denied</strong>
            <p>You do not have sufficient permissions to access restaurant settings.</p>
            <p>Required: Restaurant Admin role</p>
        </div>
    </NotAuthorized>
</AuthorizeView>
```

**Feature-specific authorization:**

```razor
@* Example: Menu management with granular permissions *@
<AuthorizeView Policy="RestaurantStaff">
    <Authorized>
        <button class="btn btn-primary" @onclick="ViewMenu">View Menu</button>
    </Authorized>
</AuthorizeView>

<AuthorizeView Policy="RestaurantAdmin">
    <Authorized>
        <button class="btn btn-warning" @onclick="EditMenu">Edit Menu</button>
        <button class="btn btn-danger" @onclick="DeleteDish">Delete Dish</button>
    </Authorized>
</AuthorizeView>
```

### 8.4 Route Protection

**Protect entire pages with `[Authorize]` attribute:**

```razor
@page "/restaurant-dashboard"
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize(Policy = "RestaurantUser")]

<PageTitle>Restaurant Dashboard</PageTitle>

<h3>Dashboard</h3>
<p>Welcome to your restaurant management dashboard.</p>

@code {
    // All users with RestaurantUser policy can access this page
}
```

**Multi-policy protection:**

```razor
@page "/admin/system-settings"
@attribute [Authorize(Policy = "RestaurantAdmin")]
@attribute [Authorize(Roles = "SystemAdmin")] @* Multiple attributes for AND logic *@

<h3>System Administration</h3>
```

### 8.5 Authentication State Management

**Access authentication state in components:**

```razor
@page "/profile"
@using Microsoft.AspNetCore.Components.Authorization
@using SmartMenuOptim.Server.Extensions
@inject NavigationManager NavigationManager

<AuthorizeView>
    <Authorized>
        <h3>User Profile</h3>
        <dl>
            <dt>Username:</dt>
            <dd>@context.User.Identity?.Name</dd>
            
            <dt>Restaurant ID:</dt>
            <dd>@context.User.GetRestaurantId()</dd>
            
            <dt>Profile Type:</dt>
            <dd>@context.User.GetProfileType()</dd>
            
            <dt>Is Admin:</dt>
            <dd>@context.User.IsRestaurantAdmin()</dd>
        </dl>
    </Authorized>
    <NotAuthorized>
        <p>Please log in to view your profile.</p>
    </NotAuthorized>
</AuthorizeView>

@code {
    [CascadingParameter] 
    private Task<AuthenticationState>? AuthState { get; set; }
    
    private ClaimsPrincipal? _user;

    protected override async Task OnInitializedAsync()
    {
        if (AuthState != null)
        {
            var authState = await AuthState;
            _user = authState.User;

            // Redirect if not authenticated
            if (_user?.Identity?.IsAuthenticated != true)
            {
                NavigationManager.NavigateTo("/login", forceLoad: true);
            }
        }
    }
}
```

### 8.6 Secure API Calls from Blazor

**Inject tenant context into HttpClient:**

```csharp
// SmartMenuOptim.Server/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddHttpClients(this IServiceCollection services)
{
    services.AddHttpClient("BackendAPI", (sp, client) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
        
        client.BaseAddress = new Uri(config["BackendApi:BaseUrl"]!);
        
        // Add tenant header from current user
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated == true)
        {
            var restaurantId = httpContext.User.GetRestaurantId();
            if (restaurantId > 0)
            {
                client.DefaultRequestHeaders.Add("X-Tenant-ID", restaurantId.ToString());
            }
        }
    });
    
    return services;
}
```

**Usage in Blazor components:**

```razor
@inject IHttpClientFactory HttpClientFactory

@code {
    private async Task<List<DishDTO>?> GetDishesAsync()
    {
        var client = HttpClientFactory.CreateClient("BackendAPI");
        
        // Tenant header automatically included from service registration
        var response = await client.GetAsync("/api/dishes");
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<DishDTO>>();
        }
        
        return null;
    }
}
```

### 8.7 Best Practices for Blazor Security

**State Management:**
- ✅ Store sensitive claims and tokens **server-side only** (Blazor Server advantage)
- ✅ Use `HttpContext.Items` for request-scoped tenant data
- ❌ **Never** store secrets in browser local storage or session storage
- ✅ Use `CascadingAuthenticationState` for auth state propagation

**Performance:**
- ✅ Cache authorization decisions with `IMemoryCache` (5-minute sliding expiration)
- ✅ Use `AuthorizeView` for declarative UI—compiled and optimized by Blazor
- ✅ Lazy load admin components only for authorized users

**Error Handling:**
- ✅ Handle `AuthenticationState` exceptions gracefully
- ✅ Provide clear error messages for insufficient permissions
- ✅ Log authorization failures to Sentry/Application Insights

**Anti-CSRF:**
- ✅ Blazor Server automatically includes anti-forgery tokens in forms
- ✅ For manual forms, use `<EditForm>` or `@inject IJSRuntime` with tokens

### 8.8 Testing Blazor Authorization

**Unit test for claims extensions:**

```csharp
[Fact]
public void GetRestaurantId_ShouldReturnCorrectId()
{
    // Arrange
    var claims = new List<Claim>
    {
        new Claim("RestaurantId", "42")
    };
    var identity = new ClaimsIdentity(claims, "TestAuth");
    var user = new ClaimsPrincipal(identity);

    // Act
    var restaurantId = user.GetRestaurantId();

    // Assert
    Assert.Equal(42, restaurantId);
}
```

**Integration test for protected pages:**

```csharp
[Fact]
public async Task RestaurantDashboard_ShouldRedirectUnauthorizedUsers()
{
    // Arrange
    var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    // Act
    var response = await client.GetAsync("/restaurant-dashboard");

    // Assert
    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    Assert.Contains("/login", response.Headers.Location?.ToString());
}
```

### 8.9 Security Implementation Checklists

**Core Security Setup:**
- [ ] `TenantResolverMiddleware` implemented and registered in `Program.cs`
- [ ] Claims-based authorization policies defined
- [ ] Repository methods consistently filter by `RestaurantId`
- [ ] Global entity access properly restricted to authorized contexts
- [ ] Logging and monitoring for security events in place
- [ ] `TenantAuthorizationHandler` created and registered

**Blazor-Specific Implementation:**
- [ ] Authentication configured in `SmartMenuOptim.Server/Program.cs`
- [ ] Authorization policies defined for Blazor (RestaurantUser, RestaurantAdmin, RestaurantStaff)
- [ ] `AuthorizeView` and `[Authorize]` attributes protect components/routes
- [ ] `ClaimsPrincipalExtensions.cs` helper created in `Extensions/` folder
- [ ] Authentication state correctly handled in components (`CascadingParameter`)
- [ ] Secure state management practices followed (no secrets in browser storage)
- [ ] Tenant context automatically injected into HttpClient headers

**API Security:**
- [ ] Rate limiting enabled and configured
- [ ] CORS policies defined and applied
- [ ] Input validation using Data Annotations or FluentValidation
- [ ] Anti-forgery tokens included in form posts
- [ ] API endpoints protected with `[Authorize(Policy = "...")]` attributes

**Data Access Security:**
- [ ] All tenant-specific queries filter by `RestaurantId` first
- [ ] `AsNoTracking()` used for read-only queries
- [ ] No global queries on tenant entities without filters
- [ ] Authorization checks on global entity access

---

## 9. Testing & Validation

### 9.1 Tenant Isolation Unit Test

```csharp
[Fact]
public async Task GetDishes_ShouldReturnOnlyRestaurantDishes()
{
    // Arrange
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase("TestDb")
        .Options;

    using var context = new AppDbContext(options);
    
    // Seed data for two restaurants
    context.Dishes.AddRange(
        new Dish { Id = 1, Name = "Dish1", RestaurantId = 1 },
        new Dish { Id = 2, Name = "Dish2", RestaurantId = 2 }
    );
    await context.SaveChangesAsync();

    var repository = new Repository<Dish>(context);

    // Act
    var dishes = await repository.Query()
        .Where(d => d.RestaurantId == 1)
        .ToListAsync();

    // Assert
    Assert.Single(dishes);
    Assert.Equal("Dish1", dishes[0].Name);
}
```

### 9.2 API Tenant Isolation Integration Test

```csharp
[Fact]
public async Task Api_ShouldEnforceTenantIsolation()
{
    // Arrange
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Tenant-ID", "1");

    // Act
    var response = await client.GetAsync("/api/dishes");
    var dishes = await response.Content.ReadFromJsonAsync<List<DishDTO>>();

    // Assert
    response.EnsureSuccessStatusCode();
    Assert.All(dishes!, d => Assert.Equal(1, d.RestaurantId));
}
```

---

## 10. Operations & Maintenance

### 10.1 Incident Response

### Security Breach Protocol

1. **Detection:** Monitor Sentry/Application Insights for anomalies
2. **Containment:** 
   - Revoke affected user sessions
   - Rotate API keys in Azure Key Vault
   - Enable additional firewall rules
3. **Investigation:** 
   - Review audit logs via Application Insights
   - Check PostgreSQL query logs
4. **Recovery:**
   - Restore from backups if needed
   - Update affected users
5. **Post-Incident:**
   - Document findings
   - Update security procedures
   - Implement additional safeguards

### 10.2 Contact Points
- **Azure Support:** [Azure Portal](https://portal.azure.com) → Support
- **Database Issues:** `docs/database/migrations/MIGRATION GUIDE.md`
- **Permission System:** `docs/security/Permission-System-Design.md`

### 10.3 Maintenance Procedures

**Monthly Tasks:**
- Security audits and vulnerability scans
- Review Sentry/Application Insights logs
- Update NuGet packages
- Review and rotate API keys

**Quarterly Tasks:**
- Test disaster recovery procedures
- Review and update security policies
- Performance optimization review

---

## 11. Compliance & Standards

### GDPR Considerations
- ✅ Right to erasure: Soft-delete implementation
- ✅ Data portability: Export APIs via controllers
- ✅ Audit trails: EntityBase tracking
- ⚠️ Data retention: Define policies per tenant

### Audit Requirements
- All entity changes tracked via `CreatedAt`/`UpdatedAt`
- User actions logged to Sentry/Application Insights
- Database-level logging enabled on Azure PostgreSQL

---

## Appendix: Quick Reference

### Key Files
- **AppDbContext:** `SmartMenuOptim.Shared/Data/Context/AppDbContext.cs`
- **TenantResolverMiddleware:** `SmartMenuOptim.Infrastructure/Middlewares/TenantResolverMiddleware.cs`
- **ServiceCollectionExtensions:** `SmartMenuOptim.API/Extensions/ServiceCollectionExtensions.cs`
- **Repository:** `SmartMenuOptim.Shared/Data/Repositories/Repository.cs`

### Related Documentation
- **Permission System:** `docs/security/Permission-System-Design.md`
- **Database Migrations:** `docs/database/migrations/MIGRATION GUIDE.md`
- **Azure Setup:** `docs/deployment/AZURE-SETUP-GUIDE.md`

### Document Metadata
- **Version:** 3.0
- **Last Updated:** January 2025
- **Status:** ✅ Unified - All security guidance consolidated
- **Git Branch:** `env-dev/feature/authoritation-implement`
- **Changelog:** Added Section 8 (Blazor Security), consolidated from separate documents




