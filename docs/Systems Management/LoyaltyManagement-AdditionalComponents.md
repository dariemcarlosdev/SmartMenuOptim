# Loyalty Management System - Additional Components

## 1. Additional Entities and Configurations

These components should be added to complement the existing LoyaltyManagementImplementation.md:

### Tier Configuration
```csharp
// SmartMenuOptim.Shared/Configuration/LoyaltyConfig.cs
public class LoyaltyConfig
{
    public static Dictionary<LoyaltyTier, TierConfiguration> DefaultTierConfigs = new()
    {
        [LoyaltyTier.Bronze] = new()
        {
            MinPoints = 0,
            PointsMultiplier = 1.0m,
            Benefits = new[] { "Base points earning" }
        },
        [LoyaltyTier.Silver] = new()
        {
            MinPoints = 1000,
            PointsMultiplier = 1.2m,
            Benefits = new[] { "20% bonus points", "Priority support" }
        },
        [LoyaltyTier.Gold] = new()
        {
            MinPoints = 5000,
            PointsMultiplier = 1.5m,
            Benefits = new[] { "50% bonus points", "Exclusive rewards", "VIP support" }
        },
        [LoyaltyTier.Platinum] = new()
        {
            MinPoints = 10000,
            PointsMultiplier = 2.0m,
            Benefits = new[] { "100% bonus points", "All VIP benefits", "Personal concierge" }
        }
    };
}

public class TierConfiguration
{
    public int MinPoints { get; set; }
    public decimal PointsMultiplier { get; set; }
    public string[] Benefits { get; set; } = [];
}
```

### Points Calculation Service
```csharp
// SmartMenuOptim.Server/Services/PointsCalculationService.cs
public interface IPointsCalculationService
{
    Task<int> CalculateOrderPoints(Order order);
    Task<decimal> CalculatePointsMultiplier(int customerId, int restaurantId);
    Task<DateTime> CalculatePointsExpiration(int points);
}

public class PointsCalculationService : IPointsCalculationService
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly LoyaltyConfig _config;

    public async Task<int> CalculateOrderPoints(Order order)
    {
        var multiplier = await CalculatePointsMultiplier(
            order.CustomerId, 
            order.RestaurantId);
            
        return (int)(order.TotalAmount * multiplier);
    }
}
```

### Expiration Management
```csharp
// SmartMenuOptim.Server/Services/PointsExpirationService.cs
public interface IPointsExpirationService
{
    Task ProcessExpiringPoints();
    Task NotifyUpcomingExpirations();
}

public class PointsExpirationService : IPointsExpirationService
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly INotificationService _notificationService;
    
    public async Task ProcessExpiringPoints()
    {
        var expiringPoints = await _loyaltyService
            .GetExpiringPoints(DateTime.UtcNow);
            
        foreach (var point in expiringPoints)
        {
            await _loyaltyService.ExpirePoints(
                point.CustomerId,
                point.RestaurantId,
                point.Points);
        }
    }
}
```

### Tier Management Enhancement
```csharp
// SmartMenuOptim.Server/Services/TierManagementService.cs
public interface ITierManagementService
{
    Task<LoyaltyTier> CalculateEligibleTier(int lifetimePoints);
    Task ProcessTierUpgrades();
    Task ProcessTierDowngrades();
    Task NotifyTierChanges();
}

public class TierManagementService : ITierManagementService
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly LoyaltyConfig _config;
    
    public async Task<LoyaltyTier> CalculateEligibleTier(int lifetimePoints)
    {
        foreach (var tier in Enum.GetValues<LoyaltyTier>().Reverse())
        {
            if (lifetimePoints >= _config.DefaultTierConfigs[tier].MinPoints)
                return tier;
        }
        return LoyaltyTier.Bronze;
    }
}
```

## 2. Background Services

### Loyalty Maintenance Service
```csharp
// SmartMenuOptim.Server/Services/LoyaltyMaintenanceHostedService.cs
public class LoyaltyMaintenanceHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LoyaltyMaintenanceHostedService> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var expirationService = scope.ServiceProvider
                    .GetRequiredService<IPointsExpirationService>();
                var tierService = scope.ServiceProvider
                    .GetRequiredService<ITierManagementService>();
                    
                await expirationService.ProcessExpiringPoints();
                await tierService.ProcessTierUpgrades();
                await tierService.ProcessTierDowngrades();
                
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in loyalty maintenance");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
```

## 3. Integration Points

### Order Integration
```csharp
// SmartMenuOptim.Server/Services/OrderService.cs
public class OrderService
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly IPointsCalculationService _pointsCalculation;
    
    public async Task<Result<Order>> CompleteOrder(Order order)
    {
        // Existing order completion logic...
        
        if (order.CustomerId.HasValue)
        {
            var points = await _pointsCalculation
                .CalculateOrderPoints(order);
                
            await _loyaltyService.AddPoints(
                order.CustomerId.Value,
                order.RestaurantId,
                points,
                $"Order #{order.Id}");
        }
        
        return Result<Order>.Success(order);
    }
}
```

These additional components enhance the existing Loyalty Management System with:
1. Detailed tier configuration
2. Points calculation logic
3. Expiration management
4. Automated maintenance
5. Order integration
6. Background processing

They should be integrated with the existing implementation documented in LoyaltyManagementImplementation.md.