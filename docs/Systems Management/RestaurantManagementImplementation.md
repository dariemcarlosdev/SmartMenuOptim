# Restaurant Management System Implementation Guide

## Overview
This guide outlines the implementation steps for the Smart Menu Optimization Restaurant Management System, integrated with the Profile Management System in a Blazor-based architecture.

## Project Structure Integration
```
SmartMenuOptim/
??? SmartMenuOptim.API/
?   ??? Controllers/
?       ??? RestaurantController.cs
?       ??? MenuController.cs
?       ??? CategoryController.cs
??? SmartMenuOptim.Server/
?   ??? Components/
?       ??? Restaurant/
?           ??? RestaurantDashboard.razor
?           ??? MenuEditor.razor
?           ??? CategoryManager.razor
??? SmartMenuOptim.Shared/
?   ??? Models/
?       ??? Restaurant/
?           ??? Restaurant.cs
?           ??? Menu.cs
?           ??? Category.cs
??? SmartMenuOptim.Tests/
    ??? Restaurant/
        ??? RestaurantTests.cs
```

## 1. Entity Definitions

### 1.1 Restaurant Entity
```csharp
public class Restaurant : EntityBase
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public int OwnerId { get; set; }
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Address { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string TimeZoneId { get; set; } = "UTC";
    
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    [ForeignKey(nameof(OwnerId))]
    public virtual AdminUser Owner { get; set; } = null!;
    public virtual ICollection<StaffMember> StaffMembers { get; set; } = [];
    public virtual ICollection<Menu> Menus { get; set; } = [];
    public virtual ICollection<Category> Categories { get; set; } = [];
    public virtual ICollection<Dish> Dishes { get; set; } = [];
    public virtual ICollection<Table> Tables { get; set; } = [];
}
```

### 1.2 Menu Entity
```csharp
public class Menu : EntityBase
{
    public int Id { get; set; }
    
    [Required]
    public int RestaurantId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public TimeSpan AvailableFrom { get; set; }
    
    [Required]
    public TimeSpan AvailableTo { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public virtual Restaurant Restaurant { get; set; } = null!;
    public virtual ICollection<Dish> Dishes { get; set; } = [];
}
```

### 1.3 Category Entity
```csharp
public class Category : EntityBase
{
    public int Id { get; set; }
    
    [Required]
    public int RestaurantId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public int DisplayOrder { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public virtual Restaurant Restaurant { get; set; } = null!;
    public virtual ICollection<Dish> Dishes { get; set; } = [];
}
```

## 2. Service Layer Implementation

### 2.1 Restaurant Service Interface
```csharp
public interface IRestaurantService
{
    Task<Result<Restaurant>> CreateRestaurant(RestaurantCreateDto dto);
    Task<Result<Restaurant>> UpdateRestaurant(RestaurantUpdateDto dto);
    Task<Result<bool>> DeleteRestaurant(int id);
    Task<Result<Restaurant>> GetRestaurant(int id);
    Task<Result<List<Restaurant>>> GetRestaurantsByOwner(int ownerId);
    Task<Result<bool>> UpdateRestaurantStatus(int id, bool isActive);
}
```

### 2.2 Restaurant Service Implementation
```csharp
public class RestaurantService : IRestaurantService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RestaurantService> _logger;
    private readonly IAuthorizationService _authorizationService;

    public async Task<Result<Restaurant>> CreateRestaurant(RestaurantCreateDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var restaurant = new Restaurant
            {
                Name = dto.Name,
                OwnerId = dto.OwnerId,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                Description = dto.Description,
                TimeZoneId = dto.TimeZoneId
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result<Restaurant>.Success(restaurant);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating restaurant");
            return Result<Restaurant>.Failure("Failed to create restaurant");
        }
    }
}
```

## 3. Blazor Components

### 3.1 Restaurant Dashboard
```razor
@* Components/Restaurant/RestaurantDashboard.razor *@
@page "/restaurant/{RestaurantId:int}"
@attribute [Authorize(Policy = "RestaurantAccess")]
@inject IRestaurantService RestaurantService
@inject IMenuService MenuService

<div class="restaurant-dashboard">
    @if (_loading)
    {
        <LoadingSpinner />
    }
    else if (_restaurant != null)
    {
        <div class="restaurant-header">
            <h1>@_restaurant.Name</h1>
            <div class="restaurant-status @(_restaurant.IsActive ? "active" : "inactive")">
                @(_restaurant.IsActive ? "Active" : "Inactive")
            </div>
        </div>

        <div class="quick-stats">
            <StatsCard Title="Today's Orders" Value="@_todayOrders.ToString()" />
            <StatsCard Title="Active Tables" Value="@_activeTables.ToString()" />
            <StatsCard Title="Staff On Duty" Value="@_staffOnDuty.ToString()" />
        </div>

        <div class="main-content">
            <ActiveMenus RestaurantId="@RestaurantId" />
            <CurrentOrders RestaurantId="@RestaurantId" />
            <StaffSchedule RestaurantId="@RestaurantId" />
        </div>
    }
</div>

@code {
    [Parameter] public int RestaurantId { get; set; }
    
    private Restaurant? _restaurant;
    private bool _loading = true;
    private int _todayOrders;
    private int _activeTables;
    private int _staffOnDuty;

    protected override async Task OnInitializedAsync()
    {
        await LoadRestaurantData();
    }

    private async Task LoadRestaurantData()
    {
        try
        {
            var result = await RestaurantService.GetRestaurant(RestaurantId);
            if (result.IsSuccess)
            {
                _restaurant = result.Value;
                await LoadDashboardStats();
            }
        }
        finally
        {
            _loading = false;
        }
    }
}
```

## 4. API Controllers

### 4.1 Restaurant Controller
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RestaurantManagement")]
public class RestaurantController : ControllerBase
{
    private readonly IRestaurantService _restaurantService;
    private readonly ILogger<RestaurantController> _logger;

    [HttpPost]
    public async Task<ActionResult<Restaurant>> Create(RestaurantCreateDto dto)
    {
        var result = await _restaurantService.CreateRestaurant(dto);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Restaurant>> Get(int id)
    {
        var result = await _restaurantService.GetRestaurant(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Restaurant>> Update(int id, RestaurantUpdateDto dto)
    {
        if (id != dto.Id)
            return BadRequest("ID mismatch");

        var result = await _restaurantService.UpdateRestaurant(dto);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
```

## 5. Integration Testing

### 5.1 Restaurant Service Tests
```csharp
public class RestaurantServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;
    
    [Fact]
    public async Task CreateRestaurant_WithValidData_ShouldSucceed()
    {
        // Arrange
        var service = new RestaurantService(_fixture.CreateContext());
        var dto = new RestaurantCreateDto
        {
            Name = "Test Restaurant",
            OwnerId = 1,
            Email = "test@restaurant.com"
        };

        // Act
        var result = await service.CreateRestaurant(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(dto.Name, result.Value.Name);
    }
}
```

## 6. Security Considerations

### 6.1 Authorization Policies
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RestaurantManagement", policy =>
        policy.RequireRole("Admin", "Owner")
              .RequireClaim("Permission", "ManageRestaurants"));

    options.AddPolicy("RestaurantAccess", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new RestaurantAccessRequirement()));
});
```

### 6.2 Restaurant Access Handler
```csharp
public class RestaurantAccessHandler : AuthorizationHandler<RestaurantAccessRequirement>
{
    private readonly AppDbContext _context;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RestaurantAccessRequirement requirement)
    {
        var user = context.User;
        var restaurantId = GetRestaurantIdFromRequest();

        if (await HasRestaurantAccess(user, restaurantId))
            context.Succeed(requirement);
    }
}
```

## 7. Performance Optimization

### 7.1 Database Indexes
```csharp
modelBuilder.Entity<Restaurant>()
    .HasIndex(r => r.OwnerId)
    .HasDatabaseName("IX_Restaurants_OwnerId");

modelBuilder.Entity<Restaurant>()
    .HasIndex(r => new { r.IsActive, r.TimeZoneId })
    .HasDatabaseName("IX_Restaurants_Status_TimeZone");
```

### 7.2 Caching Implementation
```csharp
public class CachedRestaurantService : IRestaurantService
{
    private readonly IMemoryCache _cache;
    private readonly IRestaurantService _inner;
    
    public async Task<Result<Restaurant>> GetRestaurant(int id)
    {
        var cacheKey = $"restaurant_{id}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            return await _inner.GetRestaurant(id);
        });
    }
}
```

## Implementation Checklist

### Phase 1: Core Setup
- [ ] Entity definitions
- [ ] Database migrations
- [ ] Basic CRUD operations
- [ ] API endpoints

### Phase 2: Features
- [ ] Menu management
- [ ] Category management
- [ ] Table management
- [ ] Staff assignment

### Phase 3: Integration
- [ ] Profile system integration
- [ ] Order system integration
- [ ] Inventory integration
- [ ] Loyalty system integration

### Phase 4: UI/UX
- [ ] Restaurant dashboard
- [ ] Menu editor
- [ ] Category manager
- [ ] Staff scheduler

## Monitoring and Maintenance

### Key Metrics
1. Restaurant performance metrics
2. Menu item popularity
3. Staff efficiency
4. Table turnover rate

### Regular Tasks
1. Menu optimization
2. Category reorganization
3. Staff schedule optimization
4. Performance review

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0   | TBD  | Initial implementation |
| 1.1.0   | TBD  | Menu management enhancement |
| 1.2.0   | TBD  | Staff scheduling optimization |