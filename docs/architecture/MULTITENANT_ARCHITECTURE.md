# SmartMenuOptim Multi-Tenant Architecture

## Overview

SmartMenuOptimizer implements a hierarchical multi-tenant architecture where restaurants serve as the primary tenants, and admin users can manage multiple restaurants. This document provides comprehensive architectural guidance for the multi-tenant implementation, including which entities should be tenant-specific, best practices for data isolation, and extensibility patterns.

## Current Domain Entities

The following entities are currently defined in the `SmartMenuOptim.Shared.Data.Entities` folder:

- **AdminUser**: Represents an admin user for business/admin logic and sensitive features. Not tenant-specific; acts as owner/manager of one or more restaurants (tenants).
- **BusinessRule**: (If used) Represents business rules or policies in the system.
- **Category**: Represents a category of dishes (e.g., Italian, Salad) for a specific restaurant. Tenant-specific.
- **Customer**: Represents a customer in the system. Global (shared tenancy); can interact with multiple restaurants using the same account.
- **Dish**: Represents a dish offered by a restaurant. Tenant-specific.
- **InsightResponse**: (If used) Represents AI or analytics insights returned to the system.
- **Restaurant**: Represents a restaurant (tenant) in the system. Root tenant entity.
- **Review**: Represents a customer review for a dish in a specific restaurant. Tenant-specific.
- **SaleRecord**: Represents a sales record for a dish. Tenant-specific (by association with Dish/Restaurant).
- **UserBase**: Abstract base class for shared user properties.

> _Note: Some entities (e.g., BusinessRule, InsightResponse) may be utility or supporting types. Review their usage for tenancy relevance as the app evolves._

## Multi-Tenant Entity Reference

The following entities are (or can be) tenant-specific in a multi-tenant restaurant application:

- **Menu**: Each restaurant (tenant) can have its own set of menus (e.g., breakfast, lunch, dinner, seasonal).
- **Ingredient**: If ingredients are managed per restaurant (e.g., inventory, supplier), they should be tenant-specific.
- **Order**: Orders placed by customers are specific to a restaurant.
- **OrderItem**: Items within an order, linked to dishes of a specific restaurant.
- **Reservation**: Table reservations are specific to a restaurant.
- **Table**: Physical tables in a restaurant, if you manage seating/floor plans.
- **Promotion/Discount**: Special offers or discounts that apply only to a specific restaurant.
- **Staff/User**: Employees or users (e.g., waiters, managers) assigned to a specific restaurant.
- **Notification**: System or user notifications scoped to a restaurant.
- **Payment/Transaction**: Payments processed for orders in a specific restaurant.
- **Customer Loyalty Program**: If loyalty points or rewards are tracked per restaurant.

> **Best Practice:**
> Any entity that represents data or business logic unique to a single restaurant (tenant) should be considered tenant-specific to ensure proper data isolation and multi-tenancy support.

## Core Components

### 1. Tenant Hierarchy

graph TD A["AdminUser (Global)"] -->|owns| B["Restaurant (Tenant)"] B -->|contains| C["Categories"] B -->|contains| D["Dishes"] B -->|contains| E["Reviews"] B -->|contains| F["SaleRecords"]

### 2. Base Classes and Inheritance

At the core of the tenancy model is the `TenantEntityBase` abstract class. All tenant-specific entities **must** inherit from this class to enforce data isolation.

- Automatic tenant association (`RestaurantId`)
- Audit timestamps (`CreatedAt`, `UpdatedAt`)
- Soft deletion support (`IsDeleted`)
- Concurrency control (`RowVersion`)


#### TenantEntityBase

// SmartMenuOptim.Shared\Data\Entities\Base\TenantEntityBase.cs

public abstract class TenantEntityBase
{

// Primary key for all entities
public int Id { get; set; }

// Audit timestamps
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

// Soft deletion flag
public bool IsDeleted { get; set; } = false;

// Concurrency control
[Timestamp]
public byte[]? RowVersion { get; set; }

// Tenant relationship
public int RestaurantId { get; set; }
public Restaurant? Restaurant { get; set; }
}


# ------------------------------------------------------------------------------------------------------------

## Data Isolation

### Entity Types

**Global Entities**

- `AdminUser`: Not tenant-specific, can manage multiple restaurants
  - Owns/manages multiple restaurants
  - Contains global configuration settings

public class AdminUser 
{ 
public int Id { get; set; }
public string Username { get; set; }
public string Role { get; set; }
public List<string> Permissions { get; set; }
public virtual ICollection<Restaurant> ManagedRestaurants { get; set; }
}

**Tenant-Specific Entities** (all inherit `TenantEntityBase`)

All inherit from `TenantEntityBase`:
- `Category`: Menu categories for a restaurant
- `Dish`: Restaurant-specific dishes
- `Review`: Customer reviews for dishes
- `SaleRecord`: Sales tracking per dish

public class Restaurant : TenantEntityBase 
{ 
public string Name { get; set; }
public int OwnerId { get; set; }
public AdminUser Owner { get; set; }
public virtual ICollection<Category> Categories { get; set; }
public virtual ICollection<Dish> Dishes { get; set; }
public virtual ICollection<Review> Reviews { get; set; }
}

public class Review : TenantEntityBase 
{ 
public int DishId { get; set; }
public string Comment { get; set; }
public int Rating { get; set; }
public double SentimentScore { get; set; }
public virtual Dish Dish { get; set; }
}


### Tenant Isolation Patterns

Database Level**
- Every tenant-specific entity has a mandatory `RestaurantId` foreign key
- Relationships are configured with cascade delete within tenant scope
- Foreign key constraints ensure data integrity

**Application Level**
- Base repository pattern enforces tenant isolation
- Automatic filtering based on RestaurantId
- Soft deletion support through IsDeleted flag


# ------------------------------------------------------------------------------------------------------------


## Data Isolation and Access

### Tenant-Aware Repository

A generic repository pattern is used to ensure all database operations are scoped to the current tenant. It automatically filters queries by `RestaurantId` and manages soft deletes.

#### Repository Implementation

// SmartMenuOptim.Shared\Data\Repositories\Repository.cs (**Conceptual Example**)

public class TenantRepository<TEntity> where TEntity : TenantEntityBase 
{ 

private readonly AppDbContext _context; 
private readonly DbSet<TEntity> _dbSet;

public TenantRepository(AppDbContext context)
{
    _context = context;
    _dbSet = context.Set<TEntity>();
}

// Get entities for specific tenant with filtering
public async Task<IEnumerable<TEntity>> GetAllForTenantAsync( int restaurantId, Expression<Func<TEntity, bool>>? filter = null)
{
   // Automatically filters by the tenant ID and soft-delete flag
    IQueryable<TEntity> query = _dbSet
        .Where(e => e.RestaurantId == restaurantId && !e.IsDeleted);

    if (filter != null)
        query = query.Where(filter);

    return await query.ToListAsync();
}

// Get by ID within tenant context
public async Task<T?> GetByIdForTenantAsync(int id, int restaurantId)
{
    return await _dbSet
        .FirstOrDefaultAsync(e => e.Id == id && e.RestaurantId == restaurantId && !e.IsDeleted);
}

// Soft delete within tenant context
public async Task SoftDeleteForTenantAsync(int id, int restaurantId)
{
    var entity = await GetByIdForTenantAsync(id, restaurantId);
    if (entity != null)
    {
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

// Create within tenant context
public async Task<TEntity> CreateAsync(TEntity entity, int restaurantId)
{
    entity.RestaurantId = restaurantId;
    entity.CreatedAt = DateTime.UtcNow;
    entity.UpdatedAt = DateTime.UtcNow;
    
    await _dbSet.AddAsync(entity);
    await _context.SaveChangesAsync();
    return entity;
}

// Update within tenant context
public async Task<bool> UpdateAsync(TEntity entity, int restaurantId)
{
    if (entity.RestaurantId != restaurantId)
        return false;

    entity.UpdatedAt = DateTime.UtcNow;
    _context.Entry(entity).State = EntityState.Modified;
    await _context.SaveChangesAsync();
    return true;
}


}

### Global Query Filters (EF Core)

To provide an even stronger layer of data isolation, EF Core's query filters can be configured in the `AppDbContext` to automatically apply tenant filtering to all queries for entities inheriting from `TenantEntityBase`.

// SmartMenuOptim.Shared\Data\Context\AppDbContext.cs (OnModelCreating method)

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... other configurations

// This example assumes a tenant provider service is available via DI

modelBuilder.Entity<Dish>().HasQueryFilter(p => p.RestaurantId == _tenantProvider.GetRestaurantId() && !p.IsDeleted);
modelBuilder.Entity<Category>().HasQueryFilter(p => p.RestaurantId == _tenantProvider.GetRestaurantId() && !p.IsDeleted);
modelBuilder.Entity<Review>().HasQueryFilter(p => p.RestaurantId == _tenantProvider.GetRestaurantId() && !p.IsDeleted);

base.OnModelCreating(modelBuilder);
}

# ------------------------------------------------------------------------------------------------------------

## Security and Tenant Context

### Middleware for Tenant Context Implementation

A middleware is responsible for identifying the tenant from the user's claims at the beginning of each request and making the `RestaurantId` available to the application.

    // SmartMenuOptim.API\Middleware\TenantMiddleware.cs (Conceptual Example)

    public class TenantMiddleware
    {
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
    _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
    // Extract tenant ID from the authenticated user's claims
    var claim = context.User.Claims.FirstOrDefault(c => c.Type == "RestaurantId");
    if (claim != null && int.TryParse(claim.Value, out var restaurantId))
    {
        // Store the tenant ID for the duration of the request
        context.Items["RestaurantId"] = restaurantId;
    }

    await _next(context);
    }
    }

### Controller Implementation

Controllers retrieve the tenant ID from `HttpContext.Items` and pass it to the service or repository layer, ensuring all operations are performed within the correct tenant's scope.

    // SmartMenuOptim.API\Controllers\ReviewsController.cs

    [ApiController] 
    [Route("api/[controller]")]
    [Authorize]   // Ensures user is authenticated 
    public class ReviewsController : ControllerBase
    {
    private readonly TenantRepository<Review> _reviewRepository;
    public ReviewsController(TenantRepository<Review> reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetReviews()
    {
        if (!HttpContext.Items.TryGetValue("RestaurantId", out var restaurantIdObj) || !(restaurantIdObj is int restaurantId))
        {
            return Unauthorized();
        }
        var reviews = await _reviewRepository.GetAllForTenantAsync(restaurantId);
        return Ok(reviews);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] Review review)
    {
        if (!HttpContext.Items.TryGetValue("RestaurantId", out var restaurantIdObj) || !(restaurantIdObj is int restaurantId))
        {
            return Unauthorized();
        }
        var createdReview = await _reviewRepository.CreateAsync(review, restaurantId);
        return CreatedAtAction(nameof(GetReviews), new { id = createdReview.Id }, createdReview);
    }


 }

### DbContext Configuration

public class AppDbContext : DbContext
{
    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<Restaurant> Restaurants { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Dish> Dishes { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<SaleRecord> SaleRecords { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure relationships and constraints
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Restaurant)
            .WithMany(r => r.Categories)
            .HasForeignKey(c => c.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Dish>()
            .HasOne(d => d.Restaurant)
            .WithMany(r => r.Dishes)
            .HasForeignKey(d => d.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Restaurant)
            .WithMany(res => res.Reviews)
            .HasForeignKey(r => r.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SaleRecord>()
            .HasOne(s => s.Restaurant)
            .WithMany(r => r.SaleRecords)
            .HasForeignKey(s => s.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


# ------------------------------------------------------------------------------------------------------------

## Security Implementation

For enhanced security, an authorization handler ensures that users can only access resources associated with their permitted restaurants. This can be implemented using ASP.NET Core's authorization framework or custom middleware.

Authorization Heandler implementation example:

### Authorization Handler

public class TenantAuthorizationHandler : AuthorizationHandler<TenantRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        TenantRequirement requirement)
    {
        if (context.Resource is HttpContext httpContext)
        {
            if (httpContext.Items.TryGetValue("RestaurantId", out var restaurantIdObj) &&
                restaurantIdObj is int restaurantId)
            {
                var userRestaurants = GetUserRestaurantIds(context.User);
                if (userRestaurants.Contains(restaurantId))
                {
                    context.Succeed(requirement);
                }
            }
        }
        return Task.CompletedTask;
    }
    private IEnumerable<int> GetUserRestaurantIds(ClaimsPrincipal user)
    {
        return user.Claims
            .Where(c => c.Type == "RestaurantId")
            .Select(c => int.Parse(c.Value));
    }
}

# ------------------------------------------------------------------------------------------------------------

## Security Considerations


### Data Access Control
- All queries must include tenant context
- Middleware validates tenant access
- Cross-tenant access is prevented by default

### Authentication & Authorization
- AdminUsers can access multiple restaurants
- Each request is validated against permitted restaurant access
- Tenant context is required for all operations


# ---------------------------------------------------------------------------------------------------------------

## Implementation Guidelines

This section provides code snippets and patterns for implementing multi-tenancy in SmartMenuOptimizer.

### Creating New Tenant-Specific Entities

// SmartMenuOptim.Shared\Data\Entities\NewEntity.cs

public class NewEntity : TenantEntityBase { 

// Entity-specific properties

public string Name { get; set; } = string.Empty;
// Additional properties...
public string? Description { get; set; }
public decimal Price { get; set; } = 0m;
public bool IsActive { get; set; } = true;

}

### Repository Query Pattern

// SmartMenuOptim.Shared\Data\Repositories\Repository.cs

Example method to get all entities for a specific restaurant by the mmean of RestaurantId:

public async Task<IEnumerable<TEntity>> GetAllForRestaurantAsync(int restaurantId) { 

return await _dbSet .Where(e => e.RestaurantId == restaurantId && !e.IsDeleted) .ToListAsync();

}

### Soft Deletion Pattern

// SmartMenuOptim.Shared\Data\Repositories\Repository.cs

Example soft delete method:

  public async Task SoftDeleteAsync(int id, int restaurantId)
   { 
     var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id && e.RestaurantId == restaurantId);
     if (entity != null) {
     entity.IsDeleted = true;
     await _context.SaveChangesAsync(); 
     } 
   }


### Tenant Context Middleware Example

// SmartMenuOptim.API\Middleware\TenantMiddleware.cs

TenantMiddleware extracts the RestaurantId from user claims and stores it in HttpContext.Items for downstream access.

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        var claim = context.User.Claims.FirstOrDefault(c => c.Type == "RestaurantId");
        if (claim != null && int.TryParse(claim.Value, out var restaurantId))
        {
            context.Items["RestaurantId"] = restaurantId;
        }
        await _next(context);
    }
}

#### Controller Example

// SmartMenuOptim.API\Controllers\DishesController.cs

Controller retrieves the RestaurantId from HttpContext.Items and uses it to scope data access.

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DishesController : ControllerBase
{
    private readonly TenantRepository<Dish> _dishRepository;
    public DishesController(TenantRepository<Dish> dishRepository)
    {
        _dishRepository = dishRepository;
    }
    [HttpGet]
    public async Task<IActionResult> GetDishes()
    {
        if (!HttpContext.Items.TryGetValue("RestaurantId", out var restaurantIdObj) || !(restaurantIdObj is int restaurantId))
        {
            return Unauthorized();
        }
        var dishes = await _dishRepository.GetAllForTenantAsync(restaurantId);
        return Ok(dishes);
    }
}

# ------------------------------------------------------------------------------------------------------------

## Testing Multi-Tenant Systems

For integration tests, the `CustomWebApplicationFactory` is configured to generate JWTs with specific tenant claims, allowing tests to simulate requests from different tenants.

// SmartMenuOptim.Tests\IntegrationTests\Helpers\CustomWebApplicationFactory.cs

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class 
{ 
 public HttpClient CreateClientForTenant(int restaurantId)
 { 
  var client = CreateClient(); 
  // Generate a JWT with a "RestaurantId" claim 
  var token = GenerateJwtTokenForTenant(restaurantId);
  client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
  return client; 
  }
  // ... 
  }

// RecommendEndpointTests.cs (Usage Example)

[Fact]
public async Task GetRecommendations_ForTenant1_ReturnsOnlyTenant1Data()
{ 
// Arrange
var client = _factory.CreateClientForTenant(restaurantId: 1);

// Act
var response = await client.GetAsync("/api/recommendations");

// Assert
response.EnsureSuccessStatusCode();
var content = await response.Content.ReadFromJsonAsync<List<DishDto>>();
Assert.All(content, item => Assert.NotEqual(2, item.RestaurantId)); // Ensure no data from tenant 2

}

# ------------------------------------------------------------------------------------------------------------

## Best Practices

1. **Always Inherit TenantEntityBase**
   - All tenant-specific entities must inherit from TenantEntityBase
   - Ensures consistent tenant isolation

2. **Validate Tenant Context**
   - Always validate RestaurantId in requests
   - Include tenant validation in middleware

3. **Soft Deletion**
   - Use IsDeleted flag instead of physical deletion
   - Maintain data history while ensuring isolation

4. **Audit Trail**
   - Leverage CreatedAt and UpdatedAt for tracking
   - Consider adding CreatedBy and UpdatedBy for additional tracking

## Model Design Principles

- The `Restaurant` entity is the root tenant entity. All tenant-specific data should reference the restaurant.
- The `AdminUser` entity is global and acts as the owner/manager of one or more restaurants (tenants).
- The `Customer` entity is global (shared tenancy) and can interact with multiple restaurants using the same account. Relationships (e.g., reviews, orders) link the customer to a specific restaurant.
- Any entity that represents data or business logic unique to a single restaurant (tenant) should be considered tenant-specific to ensure proper data isolation and multi-tenancy support.

## Extending the Model

When adding new features or entities, always consider whether the data should be tenant-specific. Follow these guidelines:

1. **Determine Tenancy Scope**: Ask whether the data belongs to a specific restaurant or is global
2. **Inherit from TenantEntityBase**: If tenant-specific, inherit from `TenantEntityBase` to get automatic tenant isolation
3. **Add Foreign Key**: Ensure the `RestaurantId` foreign key is properly configured
4. **Update DbContext**: Configure relationships in `AppDbContext.OnModelCreating`
5. **Apply Query Filters**: Add global query filters for automatic tenant filtering
6. **Document Relationships**: Clearly document the relationship in code comments and architecture documentation

## Future Enhancements

1. **Planned Improvements**
   - Implement tenant-specific configuration
   - Add tenant-level caching
   - Enhanced audit logging

2. **Scaling Considerations**
   - Prepare for tenant-specific database sharding
   - Consider tenant-specific storage isolation
   - Plan for tenant-specific rate limiting

## Related Documentation

- [Migration Guide](../database/migrations/migration-guide.md)
- [API Documentation](../api/endpoints/README.md)
- [Security Guidelines](../security/README.md)

---
_Last updated: 2025-08-02_


