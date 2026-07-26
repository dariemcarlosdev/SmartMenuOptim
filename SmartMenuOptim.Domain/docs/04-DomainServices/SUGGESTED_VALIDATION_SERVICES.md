# Suggested ValidationResult Value Objects & Domain Services

Based on analysis of your aggregates, here are recommended validation services following the same pattern as `MenuCompositionValidatorService` and `MenuValidationResult`.

---

## 📊 Overview: Validation Services by Aggregate

| Aggregate | ValidationResult Value Object | Domain Service | Priority |
|-----------|------------------------------|----------------|----------|
| **Order** | `OrderValidationResult` | `OrderCompositionValidatorService` | 🔴 High |
| **Reservation** | `ReservationValidationResult` | `ReservationValidatorService` | 🔴 High |
| **Promotion** | `PromotionValidationResult` | `PromotionValidatorService` | 🟡 Medium |
| **Restaurant** | `RestaurantReadinessResult` | `RestaurantReadinessValidatorService` | 🟡 Medium |
| **Dish** | `DishValidationResult` | `DishCompositionValidatorService` | 🟡 Medium |
| **CustomerLoyalty** | `LoyaltyValidationResult` | `LoyaltyProgramValidatorService` | 🟢 Low |

---

## 1️⃣ OrderValidationResult & OrderCompositionValidatorService

### Purpose
Validates that an order meets business rules before submission, ensuring order integrity and operational feasibility.

### Business Rules to Validate
```
✅ Order has at least one item
✅ All items reference active dishes from the same restaurant
✅ Order total is calculated correctly (matches sum of items)
✅ Customer exists and is active
✅ Order status transitions are valid (Pending → Confirmed → Preparing, etc.)
✅ Special instructions don't exceed limits
⚠️ Order total within reasonable range (warning if unusually high/low)
⚠️ Items don't exceed kitchen capacity (warning)
```

### Value Object: `OrderValidationResult.cs`
```csharp
namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents the result of an order validation operation.
/// </summary>
/// <remarks>
/// <para>Returned by <c>OrderCompositionValidatorService</c> to validate order integrity.</para>
/// 
/// <para><strong>Business Rules Validated:</strong></para>
/// <list type="bullet">
///   <item><description>Order has at least one item</description></item>
///   <item><description>All items from same restaurant</description></item>
///   <item><description>Order total matches item subtotals</description></item>
///   <item><description>Valid status transitions</description></item>
///   <item><description>Customer is active</description></item>
/// </list>
/// 
/// <para><strong>Clean Architecture - Domain Layer Placement:</strong></para>
/// <para>Located in Domain Layer as a Value Object (immutable, value equality, no dependencies).</para>
/// </remarks>
public sealed record OrderValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; }
    public IReadOnlyList<string> Warnings { get; init; }
    public string Summary { get; init; }
    public decimal? CalculatedTotal { get; init; } // Bonus: shows calculated total for verification

    public static OrderValidationResult Success(decimal calculatedTotal, IEnumerable<string>? warnings = null);
    public static OrderValidationResult Failure(IEnumerable<string> errors, IEnumerable<string>? warnings = null);
}
```

### Domain Service: `OrderCompositionValidatorService.cs`
```csharp
namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Validates order composition against business rules before submission.
/// </summary>
public class OrderCompositionValidatorService
{
    private const int MinimumItemCount = 1;
    private const decimal MaxOrderTotal = 10000m; // Warning threshold
    private const int MaxSpecialInstructionLength = 500;

    public OrderValidationResult ValidateOrderComposition(Order order);
    public bool HasValidItems(Order order);
    public bool HasCorrectTotal(Order order);
    public bool HasValidStatusTransition(Order order, int newStatusId);
}
```

---

## 2️⃣ ReservationValidationResult & ReservationValidatorService

### Purpose
Validates reservation requests against availability, business hours, and capacity constraints.

### Business Rules to Validate
```
✅ Reservation time is within business hours
✅ Table has sufficient capacity for party size
✅ No conflicting reservations for the same table/time
✅ Reservation is in the future (not past)
✅ Customer information is complete
✅ Party size is within table limits (1-20 typical)
⚠️ Reservation is not too far in advance (e.g., > 30 days)
⚠️ Peak time warning (high demand periods)
```

### Value Object: `ReservationValidationResult.cs`
```csharp
namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents the result of a reservation validation operation.
/// </summary>
/// <remarks>
/// <para>Returned by <c>ReservationValidatorService</c> to validate booking feasibility.</para>
/// 
/// <para><strong>Business Rules Validated:</strong></para>
/// <list type="bullet">
///   <item><description>Within business hours</description></item>
///   <item><description>Table capacity sufficient</description></item>
///   <item><description>No time conflicts</description></item>
///   <item><description>Future date required</description></item>
///   <item><description>Valid party size</description></item>
/// </list>
/// </remarks>
public sealed record ReservationValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; }
    public IReadOnlyList<string> Warnings { get; init; }
    public string Summary { get; init; }
    public IReadOnlyList<Table>? AvailableTables { get; init; } // Bonus: alternative tables if requested unavailable
    public TimeSpan? SuggestedAlternativeTime { get; init; } // Bonus: next available slot

    public static ReservationValidationResult Success(IEnumerable<string>? warnings = null);
    public static ReservationValidationResult Failure(IEnumerable<string> errors, 
        IEnumerable<Table>? alternativeTables = null, 
        TimeSpan? suggestedTime = null);
}
```

### Domain Service: `ReservationValidatorService.cs`
```csharp
namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Validates reservation requests against availability and business constraints.
/// </summary>
public class ReservationValidatorService
{
    private const int MaxAdvanceBookingDays = 30;
    private const int MinPartySize = 1;
    private const int MaxPartySize = 20;
    private const int DefaultReservationDurationMinutes = 90;

    public ReservationValidationResult ValidateReservation(
        Table table, 
        DateTime reservationTime, 
        int partySize,
        IEnumerable<Reservation> existingReservations,
        IEnumerable<BusinessHours> businessHours);
    
    public bool IsWithinBusinessHours(DateTime time, IEnumerable<BusinessHours> hours);
    public bool HasCapacity(Table table, int partySize);
    public bool HasConflict(Table table, DateTime time, IEnumerable<Reservation> existing);
}
```

---

## 3️⃣ PromotionValidationResult & PromotionValidatorService

### Purpose
Validates promotion configuration and applicability to orders.

### Business Rules to Validate
```
✅ Valid date range (ValidTo > ValidFrom)
✅ Discount amount is positive and reasonable
✅ Promotion is not expired
✅ Promotion is active
✅ Date range doesn't exceed maximum (e.g., 1 year)
⚠️ Overlapping promotions warning
⚠️ High discount amount warning (> 50% of typical order)
```

### Value Object: `PromotionValidationResult.cs`
```csharp
namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents the result of a promotion validation operation.
/// </summary>
public sealed record PromotionValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; }
    public IReadOnlyList<string> Warnings { get; init; }
    public string Summary { get; init; }
    public bool IsApplicableNow { get; init; } // Bonus: can be used right now
    public int? DaysUntilExpiry { get; init; } // Bonus: urgency indicator

    public static PromotionValidationResult Success(bool isApplicableNow, int? daysUntilExpiry, IEnumerable<string>? warnings = null);
    public static PromotionValidationResult Failure(IEnumerable<string> errors);
}
```

### Domain Service: `PromotionValidatorService.cs`
```csharp
namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Validates promotion configuration and applicability.
/// </summary>
public class PromotionValidatorService
{
    private const int MaxPromotionDurationDays = 365;
    private const decimal MaxDiscountAmount = 1000m;
    private const decimal HighDiscountThreshold = 50m; // Warning if > $50

    public PromotionValidationResult ValidatePromotion(Promotion promotion);
    public PromotionValidationResult ValidateApplicability(Promotion promotion, Order order);
    public bool IsCurrentlyActive(Promotion promotion);
    public bool HasValidDateRange(Promotion promotion);
}
```

---

## 4️⃣ RestaurantReadinessResult & RestaurantReadinessValidatorService

### Purpose
Validates that a restaurant is fully configured and ready to accept orders.

### Business Rules to Validate
```
✅ Has valid contact information (email, phone, address)
✅ Has at least one business hours entry
✅ Has at least one active menu
✅ Has at least one active dish
✅ Has valid timezone configured
✅ Owner is assigned and active
⚠️ Limited menu variety warning
⚠️ No tables configured (dine-in not available)
⚠️ No staff assigned
```

### Value Object: `RestaurantReadinessResult.cs`
```csharp
namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents the operational readiness of a restaurant.
/// </summary>
public sealed record RestaurantReadinessResult
{
    public bool IsReady { get; init; }
    public IReadOnlyList<string> BlockingIssues { get; init; } // Must fix before accepting orders
    public IReadOnlyList<string> Recommendations { get; init; } // Should fix for better operations
    public string Summary { get; init; }
    public ReadinessLevel Level { get; init; } // NotReady, MinimallyReady, FullyReady

    public static RestaurantReadinessResult Ready(ReadinessLevel level, IEnumerable<string>? recommendations = null);
    public static RestaurantReadinessResult NotReady(IEnumerable<string> blockingIssues, IEnumerable<string>? recommendations = null);
}

public enum ReadinessLevel
{
    NotReady = 0,
    MinimallyReady = 1, // Can accept orders but missing optional features
    FullyReady = 2      // All features configured
}
```

### Domain Service: `RestaurantReadinessValidatorService.cs`
```csharp
namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Validates restaurant operational readiness.
/// </summary>
public class RestaurantReadinessValidatorService
{
    private const int MinimumActiveMenus = 1;
    private const int MinimumActiveDishes = 3;
    private const int MinimumBusinessHoursDays = 1;

    public RestaurantReadinessResult ValidateReadiness(Restaurant restaurant, 
        IEnumerable<Menu> menus, 
        IEnumerable<Dish> dishes,
        IEnumerable<Table>? tables = null);
    
    public bool HasValidContactInfo(Restaurant restaurant);
    public bool HasBusinessHours(Restaurant restaurant);
    public bool HasActiveMenus(IEnumerable<Menu> menus);
}
```

---

## 5️⃣ DishValidationResult & DishCompositionValidatorService

### Purpose
Validates dish configuration for completeness and business compliance.

### Business Rules to Validate
```
✅ Has valid name (3-100 characters)
✅ Has positive price
✅ Is assigned to a category
✅ Has preparation time set
✅ Price is within reasonable range
⚠️ Missing nutritional information (calories)
⚠️ Missing allergen information
⚠️ No image uploaded
⚠️ Very short description
```

### Value Object: `DishValidationResult.cs`
```csharp
namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents the result of a dish validation operation.
/// </summary>
public sealed record DishValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; }
    public IReadOnlyList<string> Warnings { get; init; }
    public string Summary { get; init; }
    public int CompletenessScore { get; init; } // 0-100% based on filled fields

    public static DishValidationResult Success(int completenessScore, IEnumerable<string>? warnings = null);
    public static DishValidationResult Failure(IEnumerable<string> errors, int completenessScore);
}
```

### Domain Service: `DishCompositionValidatorService.cs`
```csharp
namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Validates dish configuration for completeness and compliance.
/// </summary>
public class DishCompositionValidatorService
{
    private const decimal MinPrice = 0.01m;
    private const decimal MaxPrice = 10000m;
    private const int MinNameLength = 3;
    private const int MaxNameLength = 100;
    private const int MinDescriptionLength = 10;

    public DishValidationResult ValidateDish(Dish dish);
    public bool HasRequiredFields(Dish dish);
    public int CalculateCompletenessScore(Dish dish);
    public bool HasValidPricing(Dish dish);
}
```

---

## 6️⃣ LoyaltyValidationResult & LoyaltyProgramValidatorService

### Purpose
Validates loyalty program operations and redemption requests.

### Business Rules to Validate
```
✅ Customer has sufficient points for redemption
✅ Redemption amount is positive
✅ Customer loyalty account is active
✅ Points don't exceed maximum allowed
⚠️ Customer approaching tier upgrade
⚠️ Points expiring soon
```

### Value Object: `LoyaltyValidationResult.cs`
```csharp
namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents the result of a loyalty program validation operation.
/// </summary>
public sealed record LoyaltyValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; }
    public IReadOnlyList<string> Warnings { get; init; }
    public string Summary { get; init; }
    public int CurrentPoints { get; init; }
    public int PointsToNextTier { get; init; } // Bonus: gamification hint

    public static LoyaltyValidationResult Success(int currentPoints, int pointsToNextTier, IEnumerable<string>? warnings = null);
    public static LoyaltyValidationResult Failure(IEnumerable<string> errors, int currentPoints);
}
```

---

## 📁 Suggested File Structure

```
SmartMenuOptim.Domain/
├── ValueObjects/
│   ├── MenuValidationResult.cs          ✅ EXISTS
│   ├── OrderValidationResult.cs         🆕 NEW
│   ├── ReservationValidationResult.cs   🆕 NEW
│   ├── PromotionValidationResult.cs     🆕 NEW
│   ├── RestaurantReadinessResult.cs     🆕 NEW
│   ├── DishValidationResult.cs          🆕 NEW
│   └── LoyaltyValidationResult.cs       🆕 NEW
│
├── Services/
│   ├── MenuCompositionValidatorService.cs       ✅ EXISTS
│   ├── OrderCompositionValidatorService.cs      🆕 NEW
│   ├── ReservationValidatorService.cs           🆕 NEW
│   ├── PromotionValidatorService.cs             🆕 NEW
│   ├── RestaurantReadinessValidatorService.cs   🆕 NEW
│   ├── DishCompositionValidatorService.cs       🆕 NEW
│   └── LoyaltyProgramValidatorService.cs        🆕 NEW
```

---

## 🎯 Implementation Priority

### Phase 1: High Priority (Core Operations)
1. **OrderCompositionValidatorService** - Critical for order submission workflow
2. **ReservationValidatorService** - Essential for booking functionality

### Phase 2: Medium Priority (Quality Assurance)
3. **PromotionValidatorService** - Important for marketing features
4. **RestaurantReadinessValidatorService** - Onboarding and setup validation
5. **DishCompositionValidatorService** - Content quality assurance

### Phase 3: Lower Priority (Enhancement)
6. **LoyaltyProgramValidatorService** - Loyalty program enhancements

---

## 🔧 DI Registration Pattern

All services follow the same registration pattern in `SmartMenuOptim.Domain\Extensions\ServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddDomainServices(this IServiceCollection services)
{
    // Existing services...
    services.AddSingleton<MenuCompositionValidatorService>();
    
    // New validation services (all Singleton - stateless)
    services.AddSingleton<OrderCompositionValidatorService>();
    services.AddSingleton<ReservationValidatorService>();
    services.AddSingleton<PromotionValidatorService>();
    services.AddSingleton<RestaurantReadinessValidatorService>();
    services.AddSingleton<DishCompositionValidatorService>();
    services.AddSingleton<LoyaltyProgramValidatorService>();
    
    return services;
}
```

---

## ✅ Summary

| Service | Returns | Key Validations |
|---------|---------|-----------------|
| `MenuCompositionValidatorService` | `MenuValidationResult` | Variety, categories, pricing, duplicates, seasonal |
| `OrderCompositionValidatorService` | `OrderValidationResult` | Items, totals, status, customer, restaurant scope |
| `ReservationValidatorService` | `ReservationValidationResult` | Hours, capacity, conflicts, party size |
| `PromotionValidatorService` | `PromotionValidationResult` | Date range, discount, applicability |
| `RestaurantReadinessValidatorService` | `RestaurantReadinessResult` | Contact, hours, menus, dishes, timezone |
| `DishCompositionValidatorService` | `DishValidationResult` | Name, price, category, completeness |
| `LoyaltyProgramValidatorService` | `LoyaltyValidationResult` | Points, redemption, tier |

All services follow Clean Architecture principles:
- ✅ Stateless
- ✅ No infrastructure dependencies
- ✅ Pure domain logic
- ✅ Located in Domain Layer
- ✅ Return immutable Value Objects
