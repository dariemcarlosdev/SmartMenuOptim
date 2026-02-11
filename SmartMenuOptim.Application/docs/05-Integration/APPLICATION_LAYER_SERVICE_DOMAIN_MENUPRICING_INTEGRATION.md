# Application Layer Services - MenuPricingService Integration

This directory contains Application Layer services that demonstrate real-world use cases of the `MenuPricingService` domain service, following Clean Architecture principles.

## 📁 Directory Structure

```
SmartMenuOptim.Application/
├── Services/
│   ├── Pricing/
│   │   ├── DishPricingApplicationService.cs
│   │   └── CompetitivePricingApplicationService.cs
│   ├── Orders/
│   │   └── OrderPricingApplicationService.cs
│   ├── Analytics/
│   │   └── MenuAnalyticsApplicationService.cs
│   ├── Promotions/
│   │   └── PromotionPricingApplicationService.cs
│   ├── Catering/
│   │   └── CateringPricingApplicationService.cs
│   └── Testing/
│       └── PriceTestingApplicationService.cs
```

## 🏗️ Clean Architecture Alignment

### Layer Responsibilities

| Layer | Responsibility | Example |
|-------|---------------|---------|
| **Domain** | Business rules & logic | `MenuPricingService` calculates optimal prices |
| **Application** | Workflow orchestration | These services coordinate domain + infrastructure |
| **Infrastructure** | External concerns | Repositories, APIs, Email, PDF generation |
| **Presentation** | UI/API endpoints | Blazor components, API controllers |

### Dependency Flow
```
Presentation → Application → Domain
                ↓
            Infrastructure
```

**Key Principle:** Domain layer has NO dependencies. Application layer orchestrates Domain + Infrastructure.

---

## 📋 Services Overview

### 1. **DishPricingApplicationService**
**Purpose:** Create and price new dishes with optimal pricing

**Use Cases:**
- ✅ Create new dish with cost-plus pricing
- ✅ Update dish pricing when costs change
- ✅ Validate pricing meets business requirements

**Example:**
```csharp
// Restaurant manager adds "Grilled Salmon" with $8 ingredient cost
// Result: Dish created with $12.99 price (65% markup + psychological pricing)
```

**Key Methods:**
- `CreateDishWithOptimalPricingAsync()` - Creates dish with calculated pricing
- `RecalculateDishPricingAsync()` - Updates price when costs change

---

### 2. **CompetitivePricingApplicationService**
**Purpose:** Analyze competitor pricing and market positioning

**Use Cases:**
- ✅ Compare dish prices with competitors
- ✅ Identify overpriced/underpriced items
- ✅ Generate market positioning reports

**Example:**
```csharp
// Analyze "Margherita Pizza" vs 5 competitors
// Result: Currently $16.99, market average $15.19
// Recommendation: Reduce to $14.99 to match market
```

**Key Methods:**
- `AnalyzeCompetitivePricingAsync()` - Single dish analysis
- `AnalyzeCategoryPricingAsync()` - Category-wide analysis

**External Integration:**
- Fetches competitor data from external APIs
- Uses domain service for price calculations

---

### 3. **OrderPricingApplicationService**
**Purpose:** Calculate order prices with dynamic adjustments

**Use Cases:**
- ✅ Real-time order pricing with dynamic factors
- ✅ Apply time-of-day pricing (peak/off-peak)
- ✅ Customer-specific pricing (loyalty discounts)
- ✅ Generate price quotes for future orders

**Example:**
```csharp
// Friday 7:30 PM order for Gold member
// Ribeye Steak: $28.99 base
//   + High demand: +20% → $34.79
//   + Peak dinner: +10% → $38.27
//   + Weekend: +5% → $40.18
//   - Gold loyalty: -10% → $36.16
// Final: $36.16 per steak
```

**Key Methods:**
- `CalculateOrderPriceAsync()` - Complete order pricing
- `GenerateFuturePriceQuoteAsync()` - Quote for future events

**Pricing Factors Applied:**
1. Demand-based (sales velocity)
2. Dynamic pricing (time + day of week)
3. Loyalty discounts (tier-based)

---

### 4. **MenuAnalyticsApplicationService**
**Purpose:** Menu analysis and optimization recommendations

**Use Cases:**
- ✅ Analyze menu price distribution
- ✅ Identify price imbalances in categories
- ✅ Price anchoring recommendations
- ✅ Comprehensive optimization reports

**Example:**
```csharp
// Analyze "Dinner Menu" with 25 dishes
// Results:
// - Average: $18.50, Spread: 1.62 (healthy)
// - Anchor: Surf & Turf ($49.99) - Effective
// - Issues: Wings overpriced by 53%, Chicken underpriced by 30%
// - Recommendations: 3 price adjustments
// - Impact: +$1,250/month
```

**Key Methods:**
- `GenerateMenuOptimizationReportAsync()` - Complete analysis

**Provides:**
- Statistical analysis (average, median, spread)
- Category balancing recommendations
- Price anchoring strategy
- Revenue impact estimates

---

### 5. **PromotionPricingApplicationService**
**Purpose:** Manage promotional pricing and happy hours

**Use Cases:**
- ✅ Generate happy hour menus
- ✅ Create flash sales
- ✅ Event-based pricing
- ✅ Promotional campaigns

**Example:**
```csharp
// Happy Hour: Mon-Fri 3-6 PM
// Buffalo Wings: $8.99 → $6.99 (save $2.00)
// Nachos: $10.99 → $8.49 (save $2.50)
// Average savings: $2.35 across 15 items
// Marketing: "Save $2.35 on apps, Mon-Fri 3-6 PM!"
```

**Key Methods:**
- `GenerateHappyHourMenuAsync()` - Create happy hour menu
- `CreateFlashSaleAsync()` - Limited-time promotions

**Integrations:**
- Notification service (customer alerts)
- Scheduling service (auto-start/stop)

---

### 6. **CateringPricingApplicationService**
**Purpose:** Bulk order and catering quote generation

**Use Cases:**
- ✅ Generate catering quotes
- ✅ Calculate bulk discounts
- ✅ Event planning with deposits
- ✅ PDF quote generation

**Example:**
```csharp
// Corporate lunch for 100 people
// Food: $2,797.50
// Bulk discount (15%): -$419.63
// Service fees: +$225.00
// Total: $2,602.87 ($26.03/person)
// Includes: Delivery, Setup, Servingware
```

**Key Methods:**
- `CalculateCateringQuoteAsync()` - Complete quote generation

**Features:**
- Tiered bulk discounts (10-50+ servings)
- Service fee calculation
- Deposit management
- PDF generation & email delivery

---

### 7. **PriceTestingApplicationService**
**Purpose:** A/B testing for price optimization

**Use Cases:**
- ✅ Design price experiments
- ✅ Track sales by variant
- ✅ Statistical analysis
- ✅ Recommendation engine

**Example:**
```csharp
// Test "Chicken Parmesan" pricing
// Control: $14.99 (120 sales, $1,799 revenue)
// Lower: $12.99 (105 sales, $1,364 revenue)
// Higher: $15.99 (85 sales, $1,359 revenue)
// 
// Winner: Lower price ($12.99)
// Revenue: +13.7% vs control
// Recommendation: Adopt $12.99 price
// Impact: +$1,232/month
```

**Key Methods:**
- `CreatePriceTestAsync()` - Setup test with variants
- `AnalyzePriceTestResultsAsync()` - Statistical analysis

**Analysis Includes:**
- Revenue comparison
- Sales velocity changes
- Statistical significance
- Price elasticity calculation
- Implementation recommendations

---

## 🎯 Common Patterns

### 1. Constructor Injection
All services use dependency injection:
```csharp
public class DishPricingApplicationService
{
    private readonly IDishRepository _dishRepository;
    private readonly MenuPricingService _pricingService;
    
    public DishPricingApplicationService(
        IDishRepository dishRepository,
        MenuPricingService pricingService)
    {
        _dishRepository = dishRepository;
        _pricingService = pricingService;
    }
}
```

### 2. Request/Response DTOs
Clean input/output contracts:
```csharp
public record CreateDishRequest(
    string DishName,
    decimal IngredientCost,
    int CategoryId
);

public record CreateDishResult
{
    public bool Success { get; init; }
    public Money CalculatedPrice { get; init; }
    // ...
}
```

### 3. Async Operations
All operations are async for scalability:
```csharp
public async Task<CreateDishResult> CreateDishWithOptimalPricingAsync(
    CreateDishRequest request,
    CancellationToken cancellationToken = default)
{
    // ...
}
```

### 4. Domain Service Integration
Application services orchestrate, domain services contain logic:
```csharp
// Application Layer: Orchestration
var dish = await _dishRepository.GetByIdAsync(dishId);
var salesHistory = await _salesRepository.GetRecentSalesAsync(dishId);

// Domain Layer: Business Logic
var optimalPrice = _pricingService.CalculateOptimalPrice(dish, salesHistory);
```

### 5. Error Handling
Proper exception handling with logging:
```csharp
try
{
    _logger.LogInformation("Creating dish {DishName}", request.DishName);
    // ... operation
    _logger.LogInformation("Dish created successfully");
    return CreateDishResult.Success(...);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error creating dish");
    throw;
}
```

---

## 🔧 Registration in DI Container

```csharp
// Startup.cs or Program.cs
services.AddScoped<DishPricingApplicationService>();
services.AddScoped<CompetitivePricingApplicationService>();
services.AddScoped<OrderPricingApplicationService>();
services.AddScoped<MenuAnalyticsApplicationService>();
services.AddScoped<PromotionPricingApplicationService>();
services.AddScoped<CateringPricingApplicationService>();
services.AddScoped<PriceTestingApplicationService>();

// Domain Service (stateless, can be singleton)
services.AddSingleton<MenuPricingService>();
```

---

## 📊 Usage in Blazor Components

```csharp
@page "/dishes/create"
@inject DishPricingApplicationService PricingService

<EditForm Model="@model" OnValidSubmit="CreateDish">
    <InputText @bind-Value="model.DishName" />
    <InputNumber @bind-Value="model.IngredientCost" />
    <button type="submit">Create Dish</button>
</EditForm>

@code {
    private CreateDishRequest model = new();

    private async Task CreateDish()
    {
        var result = await PricingService.CreateDishWithOptimalPricingAsync(model);
        
        if (result.Success)
        {
            // Show success message
            // Display calculated price: result.CalculatedPrice
        }
    }
}
```

---

## 📖 Usage in API Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class DishesController : ControllerBase
{
    private readonly DishPricingApplicationService _pricingService;

    public DishesController(DishPricingApplicationService pricingService)
    {
        _pricingService = pricingService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDish(CreateDishRequest request)
    {
        var result = await _pricingService.CreateDishWithOptimalPricingAsync(request);
        
        if (!result.Success)
            return BadRequest(result.Errors);
        
        return CreatedAtAction(
            nameof(GetDish), 
            new { id = result.DishId }, 
            result
        );
    }
}
```

---

## ✅ Best Practices Demonstrated

1. **Separation of Concerns**
   - Domain: Pure business logic
   - Application: Workflow orchestration
   - Infrastructure: External integrations

2. **Single Responsibility**
   - Each service has one clear purpose
   - Methods are focused and cohesive

3. **Dependency Inversion**
   - Depend on abstractions (interfaces)
   - Domain layer has no dependencies

4. **Immutability**
   - DTOs use records (immutable by default)
   - Value objects (Money) are immutable

5. **Testability**
   - All dependencies injected
   - Easy to mock for unit testing
   - Domain logic isolated and testable

6. **Documentation**
   - Comprehensive XML comments
   - Real-world examples
   - Usage patterns explained

---

## 🧪 Testing Example

```csharp
public class DishPricingApplicationServiceTests
{
    [Fact]
    public async Task CreateDish_WithValidCost_CalculatesOptimalPrice()
    {
        // Arrange
        var mockRepo = new Mock<IDishRepository>();
        var pricingService = new MenuPricingService();
        var service = new DishPricingApplicationService(
            mockRepo.Object, 
            pricingService
        );
        
        var request = new CreateDishRequest(
            DishName: "Test Dish",
            IngredientCost: 10.00m,
            CategoryId: 1
        );

        // Act
        var result = await service.CreateDishWithOptimalPricingAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(16.49m, result.CalculatedPrice.Amount); // 10 * 1.65 → 16.49
        Assert.True(result.ProfitMargin >= 30); // Minimum margin met
    }
}
```

---

## 📚 Related Documentation

- [Domain Service Guide](../../SmartMenuOptim.Domain/docs/DOMAIN_SERVICE.md)
- [Clean Architecture Analysis](../../docs/architecture/CLEAN_ARCHITECTURE_FULL_ANALYSIS.md)
- [MenuPricingService Implementation](../../SmartMenuOptim.Domain/Services/MenuPricingService.cs)
- [Pricing Value Objects](../../SmartMenuOptim.Domain/ValueObjects/PricingValueObjects.cs)

---

## 🚀 Implementation Status

All services are **commented implementations** showing:
- ✅ Proper structure and patterns
- ✅ Real-world use cases
- ✅ Complete examples with data
- ✅ Clean Architecture principles
- ✅ Best practices

To implement:
1. Uncomment service code
2. Implement repository interfaces
3. Add integration services
4. Register in DI container
5. Create Blazor components or API endpoints
6. Write unit and integration tests

---

**Created:** January 2024  
**Author:** SmartMenuOptimizer Development Team  
**Version:** 1.0.0
