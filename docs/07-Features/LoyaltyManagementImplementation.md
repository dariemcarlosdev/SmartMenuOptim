# Loyalty Management System Implementation Guide

## Overview
This guide outlines the implementation steps for the Smart Menu Optimization Loyalty Management System, integrated with the Profile Management System in a Blazor-based architecture.

## Project Structure Integration
```
SmartMenuOptim/
??? SmartMenuOptim.API/
?   ??? Controllers/
?       ??? LoyaltyController.cs
??? SmartMenuOptim.Server/
?   ??? Components/
?       ??? Loyalty/
?           ??? LoyaltyDashboard.razor
?           ??? PointsDisplay.razor
??? SmartMenuOptim.Shared/
?   ??? Models/
?       ??? Loyalty/
?           ??? LoyaltyTransaction.cs
?           ??? CustomerLoyalty.cs
??? SmartMenuOptim.Tests/
    ??? Loyalty/
        ??? LoyaltyTests.cs
```

## 1. Database Schema

### 1.1 Entity Definitions
```csharp
// CustomerLoyalty.cs
public class CustomerLoyalty : EntityBase
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int RestaurantId { get; set; }
    public int Points { get; set; }
    public int LifetimePoints { get; set; }
    public LoyaltyTier Tier { get; set; }
    public DateTime LastActivity { get; set; }

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual Restaurant Restaurant { get; set; } = null!;
    public virtual ICollection<LoyaltyTransaction> Transactions { get; set; } = [];
}

// LoyaltyTransaction.cs
public class LoyaltyTransaction : EntityBase
{
    public int Id { get; set; }
    public int CustomerLoyaltyId { get; set; }
    public int CustomerId { get; set; }
    public int RestaurantId { get; set; }
    public int PointsChange { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int BalanceAfter { get; set; }
    
    // Navigation properties
    public virtual CustomerLoyalty CustomerLoyalty { get; set; } = null!;
}
```

### 1.2 Entity Configuration
```csharp
// AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<CustomerLoyalty>()
        .HasIndex(cl => new { cl.RestaurantId, cl.CustomerId })
        .IsUnique()
        .HasDatabaseName("IX_CustomerLoyalties_Restaurant_Customer_Unique");

    modelBuilder.Entity<LoyaltyTransaction>()
        .HasIndex(lt => new { lt.RestaurantId, lt.CustomerLoyaltyId, lt.CreatedAt })
        .HasDatabaseName("IX_LoyaltyTransactions_Restaurant_CustomerLoyalty_Date");
}
```

## 2. Service Layer Implementation

### 2.1 Loyalty Service Interface
```csharp
public interface ILoyaltyService
{
    Task<Result<CustomerLoyalty>> GetCustomerLoyalty(int customerId, int restaurantId);
    Task<Result<int>> AddPoints(int customerId, int restaurantId, int points, string description);
    Task<Result<int>> RedeemPoints(int customerId, int restaurantId, int points, string reward);
    Task<Result<LoyaltyTier>> CalculateTier(int customerId, int restaurantId);
}
```

### 2.2 Loyalty Service Implementation
```csharp
public class LoyaltyService : ILoyaltyService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LoyaltyService> _logger;

    public async Task<Result<int>> AddPoints(int customerId, int restaurantId, int points, string description)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var loyalty = await _context.CustomerLoyalties
                .FirstOrDefaultAsync(cl => cl.CustomerId == customerId && 
                                         cl.RestaurantId == restaurantId);

            if (loyalty == null)
            {
                loyalty = new CustomerLoyalty
                {
                    CustomerId = customerId,
                    RestaurantId = restaurantId,
                    Points = 0,
                    LifetimePoints = 0,
                    Tier = LoyaltyTier.Bronze,
                    LastActivity = DateTime.UtcNow
                };
                _context.CustomerLoyalties.Add(loyalty);
            }

            loyalty.Points += points;
            loyalty.LifetimePoints += points;
            loyalty.LastActivity = DateTime.UtcNow;

            var transaction = new LoyaltyTransaction
            {
                CustomerLoyaltyId = loyalty.Id,
                CustomerId = customerId,
                RestaurantId = restaurantId,
                PointsChange = points,
                Type = LoyaltyTransactionType.OrderEarning,
                Description = description,
                BalanceAfter = loyalty.Points
            };

            _context.LoyaltyTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result<int>.Success(loyalty.Points);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error adding loyalty points");
            return Result<int>.Failure("Failed to add points");
        }
    }
}
```

## 3. Blazor Components

### 3.1 Loyalty Dashboard
```razor
@* Components/Loyalty/LoyaltyDashboard.razor *@
@inherits ComponentBase
@inject ILoyaltyService LoyaltyService

<div class="loyalty-dashboard">
    <h2>Loyalty Status</h2>
    
    <div class="tier-info">
        <h3>Current Tier: @loyalty.Tier</h3>
        <div class="progress">
            <div class="progress-bar" style="width: @GetProgressPercentage()%">
                @loyalty.Points / @GetPointsForNextTier() points
            </div>
        </div>
    </div>

    <div class="transactions">
        <h3>Recent Activity</h3>
        <div class="transaction-list">
            @foreach (var transaction in recentTransactions)
            {
                <div class="transaction-item">
                    <span class="@GetTransactionClass(transaction.Type)">
                        @(transaction.PointsChange > 0 ? "+" : "")@transaction.PointsChange
                    </span>
                    <span class="description">@transaction.Description</span>
                    <span class="date">@transaction.CreatedAt.ToLocalTime().ToString("g")</span>
                </div>
            }
        </div>
    </div>
</div>

@code {
    private CustomerLoyalty loyalty = new();
    private List<LoyaltyTransaction> recentTransactions = [];

    [Parameter]
    public int CustomerId { get; set; }

    [Parameter]
    public int RestaurantId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadLoyaltyData();
    }

    private async Task LoadLoyaltyData()
    {
        var result = await LoyaltyService.GetCustomerLoyalty(CustomerId, RestaurantId);
        if (result.IsSuccess)
        {
            loyalty = result.Value;
            // Load recent transactions
            recentTransactions = await LoyaltyService.GetRecentTransactions(CustomerId, RestaurantId);
        }
    }
}
```

## 4. Integration Testing

### 4.1 Loyalty Service Tests
```csharp
public class LoyaltyServiceTests : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task AddPoints_ShouldIncrementBalance()
    {
        // Arrange
        await using var context = new AppDbContext(_options);
        var service = new LoyaltyService(context);

        // Act
        var result = await service.AddPoints(1, 1, 100, "Test purchase");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value);
    }

    [Fact]
    public async Task CalculateTier_ShouldUpdateCorrectly()
    {
        // Arrange
        await using var context = new AppDbContext(_options);
        var service = new LoyaltyService(context);

        // Act
        await service.AddPoints(1, 1, 1000, "Large purchase");
        var result = await service.CalculateTier(1, 1);

        // Assert
        Assert.Equal(LoyaltyTier.Gold, result.Value);
    }
}
```

## 5. API Endpoints

### 5.1 Loyalty Controller
```csharp
[ApiController]
[Route("api/[controller]")]
public class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;

    [HttpPost("points/add")]
    public async Task<ActionResult<int>> AddPoints(AddPointsRequest request)
    {
        var result = await _loyaltyService.AddPoints(
            request.CustomerId,
            request.RestaurantId,
            request.Points,
            request.Description);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{customerId}/{restaurantId}")]
    public async Task<ActionResult<CustomerLoyalty>> GetLoyalty(int customerId, int restaurantId)
    {
        var result = await _loyaltyService.GetCustomerLoyalty(customerId, restaurantId);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}
```

## 6. Security Considerations

### 6.1 Authorization Policies
```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManageLoyalty", policy =>
        policy.RequireRole("Admin", "Manager")
              .RequireClaim("Permission", "ManageLoyalty"));
    
    options.AddPolicy("ViewLoyalty", policy =>
        policy.RequireAuthenticatedUser());
});
```

### 6.2 Data Validation
```csharp
public class AddPointsRequest
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int RestaurantId { get; set; }

    [Range(1, 10000)]
    public int Points { get; set; }

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;
}
```

## 7. Performance Optimization

### 7.1 Caching Strategy
```csharp
public class CachedLoyaltyService : ILoyaltyService
{
    private readonly IMemoryCache _cache;
    private readonly ILoyaltyService _innerService;

    public async Task<Result<CustomerLoyalty>> GetCustomerLoyalty(int customerId, int restaurantId)
    {
        var cacheKey = $"loyalty_{customerId}_{restaurantId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            return await _innerService.GetCustomerLoyalty(customerId, restaurantId);
        });
    }
}
```

## 8. Monitoring

### 8.1 Custom Metrics
```csharp
public class LoyaltyMetrics
{
    private readonly IMetricsRoot _metrics;

    public void RecordPointsEarned(int points, string restaurantId)
    {
        _metrics.Measure.Counter.Increment("loyalty_points_earned", points,
            new MetricTags("restaurant", restaurantId));
    }

    public void RecordPointsRedeemed(int points, string restaurantId)
    {
        _metrics.Measure.Counter.Increment("loyalty_points_redeemed", points,
            new MetricTags("restaurant", restaurantId));
    }
}
```

## Implementation Checklist

### Phase 1: Foundation
- [ ] Database schema implementation
- [ ] Basic CRUD operations
- [ ] Service layer implementation
- [ ] API endpoints

### Phase 2: Features
- [ ] Points calculation
- [ ] Tier management
- [ ] Reward redemption
- [ ] Transaction history

### Phase 3: Integration
- [ ] Profile system integration
- [ ] Restaurant integration
- [ ] Order system integration
- [ ] Notification system

### Phase 4: UI/UX
- [ ] Dashboard components
- [ ] Transaction history display
- [ ] Reward catalog
- [ ] Points summary

## Support and Maintenance

### Monitoring
1. Transaction volume metrics
2. Points balance auditing
3. Tier transition tracking
4. System performance metrics

### Regular Tasks
1. Points expiration processing
2. Tier recalculation
3. Transaction cleanup
4. Performance optimization

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0   | TBD  | Initial implementation |
| 1.1.0   | TBD  | Points expiration |
| 1.2.0   | TBD  | Advanced rewards |