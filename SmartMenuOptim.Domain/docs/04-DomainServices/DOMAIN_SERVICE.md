# Domain Services - Clean Architecture Guide

## Overview

This folder contains **Domain Services** that encapsulate pure domain logic which doesn't naturally fit within a single aggregate or value object. 
Domain services are essential when business operations span multiple aggregates or require specialized domain expertise.
This is a core domain knowledge area for SmartMenuOptim, focusing on menu optimization, pricing strategies, inventory forecasting, and customer insights - no application or infrastructure concerns.

---

## What Are Domain Services?

**Domain Services** are stateless objects that:
- Contain domain logic that doesn't belong to a specific entity
- Orchestrate operations across multiple domain objects
- Implement complex business rules and calculations
- Express domain concepts that aren't naturally modeled as entities or value objects
- Are named using domain language (ubiquitous language)

**Key Characteristic:** Pure domain logic with NO infrastructure dependencies.

---

## Domain Service Characteristics

### ✅ **What makes it a Domain Service:**
- **Stateless**: Contains no internal state, operates purely on input parameters
- **Pure Domain Logic**: Handles complex pricing calculations that don't naturally belong to a single entity (like Dish or Menu)
- **Complex Calculations**: Business algorithms too complex for entities - sophisticated pricing strategies, forecasting models, optimization algorithms
- **Cross-Aggregate Operations**: Works with multiple domain objects (Dish, Menu, SaleRecord)
- **Business Rules**: Implements complex pricing strategies and business calculations
- **Domain Language**: Uses ubiquitous language (Money, Dish, Menu, etc.)
- **No Infrastructure Dependencies**: Contains only business logic, no database access or external API calls

### **Purpose**

This service encapsulates complex domain logic that would be inappropriate to put in:
- **Entities** (too complex for a single Dish or Menu entity)
- **Value Objects** (involves multiple aggregates)
- **Application Services** (this is pure business logic, not workflow orchestration)

### **Domain Service vs Other Types**

| Type | Purpose | Dependencies | State |
|------|---------|--------------|-------|
| **Domain Service** ← This one | Pure business logic | Domain objects only | Stateless |
| **Application Service** | Workflow orchestration | Repositories, infrastructure | Can have transaction state |
| **Infrastructure Service** | Technical concerns | External systems, databases | Can be stateful |

---

## When to Use Domain Services

### ✅ **Use Domain Services When:**
1. **Cross-Aggregate Operations** - Logic spans multiple aggregates
2. **Domain Policies** - Business rules that don't fit in entities
3. **Stateless Operations** - Pure functions operating on domain objects
4. **Domain Expertise** - Specialized knowledge (pricing, inventory, analytics)

### ❌ **Do NOT Use Domain Services For:**
- **Database Access** - Use repositories (Infrastructure layer)
- **External APIs** - Use integration services (Application/Infrastructure layer)
- **Email/SMS** - Use notification services (Infrastructure layer)
- **Application Workflow** - Use application services (Application layer)
- **UI Logic** - Keep in Presentation layer
- **Data Mapping** - Use application services with AutoMapper

---

## What Should Be Included in This Folder

### Restaurant Domain Services

#### **MenuPricingService**
```csharp
/// <summary>
/// Handles complex menu pricing strategies and calculations.
/// </summary>
public class MenuPricingService
{
    // Calculate optimal pricing based on:
    // - Cost of ingredients
    // - Market demand (sales data)
    // - Competitor pricing
    // - Seasonal factors
    // - Profit margin requirements
    
    public Money CalculateOptimalPrice(Dish dish, IEnumerable<SaleRecord> salesHistory);
    public Money ApplyDynamicPricing(Menu menu, DateTime timeOfDay, DayOfWeek dayOfWeek);
    public Money CalculateBulkDiscount(IEnumerable<Dish> dishes, int quantity);
}
```

#### **InventoryForecastingService**
```csharp
/// <summary>
/// Predicts inventory needs based on sales patterns.
/// </summary>
public class InventoryForecastingService
{
    // Forecast ingredient requirements based on:
    // - Historical sales data
    // - Seasonal trends
    // - Menu composition
    // - Upcoming promotions
    // - Day of week patterns
    
    public Dictionary<Ingredient, int> ForecastDailyNeeds(
        Restaurant restaurant, 
        DateTime forecastDate,
        IEnumerable<SaleRecord> historicalSales);
        
    public int CalculateReorderQuantity(Ingredient ingredient, int currentStock);
}
```

#### **MenuOptimizationService**
```csharp
/// <summary>
/// Analyzes and recommends menu improvements using AI and analytics.
/// </summary>
public class MenuOptimizationService
{
    // Provide recommendations for:
    // - Underperforming dishes (low sales + high cost)
    // - Star dishes (high sales + high margin)
    // - Menu balance (variety, price points)
    // - Profitability optimization
    // - Customer preference patterns
    
    public MenuOptimizationReport AnalyzeMenuPerformance(
        Menu menu,
        IEnumerable<SaleRecord> sales,
        IEnumerable<Review> reviews);
        
    public IEnumerable<Dish> RecommendDishesToRemove(Menu menu);
    public IEnumerable<DishRecommendation> SuggestNewDishes(Restaurant restaurant);
}
```

#### **ReviewSentimentAnalysisService**
```csharp
/// <summary>
/// Analyzes customer review sentiment and extracts insights.
/// </summary>
public class ReviewSentimentAnalysisService
{
    // Domain logic for:
    // - Sentiment score calculation (0.0 - 1.0)
    // - Keyword extraction (popular terms)
    // - Trend identification (improving/declining)
    // - Review categorization (positive/negative/neutral)
    
    public double CalculateSentimentScore(string reviewText);
    public IEnumerable<string> ExtractKeywords(IEnumerable<Review> reviews);
    public SentimentTrend AnalyzeTrend(IEnumerable<Review> reviews, TimeSpan period);
}
```

#### **RevenueAnalysisService**
```csharp
/// <summary>
/// Calculates revenue metrics and financial KPIs for the restaurant.
/// </summary>
public class RevenueAnalysisService
{
    // Calculate:
    // - Total revenue by period (day, week, month)
    // - Average transaction value
    // - Revenue per dish/category
    // - Profit margins
    // - Growth rates
    
    public Money CalculateTotalRevenue(
        IEnumerable<SaleRecord> sales, 
        DateRange period);
        
    public decimal CalculateAverageTransactionValue(IEnumerable<SaleRecord> sales);
    public RevenueReport GenerateRevenueReport(Restaurant restaurant, DateRange period);
}
```

#### **DishPopularityRankingService**
```csharp
/// <summary>
/// Ranks dishes by popularity using multiple factors.
/// </summary>
public class DishPopularityRankingService
{
    // Rank dishes based on:
    // - Sales volume
    // - Revenue generated
    // - Review ratings
    // - Repeat purchase rate
    // - Time-based trends
    
    public IEnumerable<DishRanking> RankDishesByPopularity(
        IEnumerable<Dish> dishes,
        IEnumerable<SaleRecord> sales,
        IEnumerable<Review> reviews);
        
    public IEnumerable<Dish> GetTrendingDishes(Restaurant restaurant, TimeSpan period);
}
```

#### **PromotionEligibilityService**
```csharp
/// <summary>
/// Determines if customers/dishes qualify for promotions.
/// </summary>
public class PromotionEligibilityService
{
    // Check eligibility based on:
    // - Customer loyalty status
    // - Purchase history
    // - Time constraints
    // - Dish availability
    // - Minimum order requirements
    
    public bool IsEligibleForPromotion(Customer customer, Promotion promotion);
    public Money CalculateDiscountedPrice(Dish dish, Promotion promotion);
    public IEnumerable<Promotion> GetApplicablePromotions(Customer customer, IEnumerable<Dish> dishes);
}
```

#### **TableAvailabilityService** (if implementing reservations)
```csharp
/// <summary>
/// Manages table availability and seating optimization.
/// </summary>
public class TableAvailabilityService
{
    // Handle:
    // - Table availability checking
    // - Seating capacity calculations
    // - Table assignment optimization
    // - Reservation conflict detection
    
    public bool IsTableAvailable(Table table, DateTime startTime, TimeSpan duration);
    public IEnumerable<Table> FindAvailableTables(Restaurant restaurant, int partySize, DateTime desiredTime);
    public Table AssignOptimalTable(int partySize, IEnumerable<Table> availableTables);
}
```

#### **MenuCompositionValidator**. 
This service validates that a menu meets certain business rules and standards, such as having a balanced variety of dishes, appropriate price points, and no duplicate items. It ensures that the menu is well-structured and appealing to customers while adhering to the restaurant's strategic goals.
```csharp
/// <summary>
/// Validates menu composition against business rules.
/// </summary>
public class MenuCompositionValidator
{
    // Validate:
    // - Menu has minimum dish variety
    // - Categories are balanced
    // - Price ranges are appropriate
    // - No duplicate dishes
    // - Seasonal items are current
    
    public ValidationResult ValidateMenuComposition(Menu menu);
    public bool HasAdequateVariety(Menu menu);
    public bool HasBalancedPricePoints(Menu menu);
}
```

---

## What Should NOT Be Included

❌ **Application Services** - Workflow orchestration (Application layer)  
❌ **Repositories** - Data access logic (Infrastructure layer)  
❌ **Integration Services** - External API calls (Infrastructure layer)  
❌ **Notification Services** - Email/SMS sending (Infrastructure layer)  
❌ **Authentication Services** - User authentication (Infrastructure layer)  
❌ **Logging Services** - System logging (Infrastructure layer)  
❌ **Caching Services** - Data caching (Infrastructure layer)  
❌ **File Storage Services** - File uploads (Infrastructure layer)  
❌ **Payment Processing** - Payment gateway integration (Infrastructure layer)

---

## Domain Service Structure Template

```csharp
namespace SmartMenuOptim.Domain.Services
{
    /// <summary>
    /// [Description of the domain service's responsibility using ubiquitous language]
    /// </summary>
    /// <remarks>
    /// Domain Service Characteristics:
    /// - Stateless: No internal state, operates on parameters
    /// - Pure Domain Logic: No infrastructure dependencies
    /// - Business-Focused: Implements core business rules
    /// 
    /// Use Cases:
    /// - [List specific business scenarios where this service is used]
    /// 
    /// Example:
    /// <code>
    /// var service = new [ServiceName]();
    /// var result = service.[MethodName](parameters);
    /// </code>
    /// </remarks>
    public class [ServiceName]
    {
        // === Dependencies (Other Domain Services or Value Objects Only) ===
        
        // NO repositories, NO infrastructure services
        // Only other domain services or simple dependencies
        
        // === Public Methods (Domain Operations) ===
        
        /// <summary>
        /// [Description of what this operation does in business terms]
        /// </summary>
        /// <param name="parameter">[Business meaning of parameter]</param>
        /// <returns>[Business meaning of return value]</returns>
        /// <exception cref="DomainException">Thrown when [business rule violation]</exception>
        public [ReturnType] [MethodName]([Parameters])
        {
            // 1. Validate inputs (guard clauses)
            ValidateInputs(parameters);
            
            // 2. Execute domain logic
            var result = PerformDomainCalculation(parameters);
            
            // 3. Return result (domain object, value object, or primitive)
            return result;
        }
        
        // === Private Helper Methods (Internal Logic) ===
        
        private void ValidateInputs([Parameters])
        {
            // Guard clauses for business rule validation
            if (invalidCondition)
                throw new DomainException("Business rule violated");
        }
        
        private [ReturnType] PerformDomainCalculation([Parameters])
        {
            // Pure domain logic - no side effects
            // No database access, no external API calls
            return calculatedResult;
        }
    }
}
```

---

## Example: MenuPricingService Implementation

```csharp
namespace SmartMenuOptim.Domain.Services
{
    /// <summary>
    /// Calculates optimal pricing for dishes based on multiple business factors.
    /// </summary>
    /// <remarks>
    /// This service encapsulates complex pricing algorithms that don't belong to
    /// a single entity. It considers cost, demand, competition, and market dynamics.
    /// 
    /// Pricing Strategies Supported:
    /// - Cost-plus pricing (ingredient cost + markup)
    /// - Demand-based pricing (sales volume analysis)
    /// - Competitive pricing (market positioning)
    /// - Dynamic pricing (time-of-day, day-of-week adjustments)
    /// </remarks>
    public class MenuPricingService
    {
        private const decimal DefaultMarkupPercentage = 0.65m; // 65% markup
        private const decimal MinimumProfitMargin = 0.30m; // 30% minimum margin
        
        /// <summary>
        /// Calculates optimal price for a dish based on costs and market factors.
        /// </summary>
        /// <param name="ingredientCost">Total cost of ingredients</param>
        /// <param name="salesHistory">Historical sales data for demand analysis</param>
        /// <param name="competitorPrices">Market prices for similar dishes (optional)</param>
        /// <returns>Recommended price as Money value object</returns>
        /// <exception cref="ArgumentNullException">When ingredientCost is null</exception>
        /// <exception cref="DomainException">When cost would result in negative margin</exception>
        public Money CalculateOptimalPrice(
            Money ingredientCost,
            IEnumerable<SaleRecord> salesHistory,
            IEnumerable<Money>? competitorPrices = null)
        {
            // Validate inputs
            if (ingredientCost == null)
                throw new ArgumentNullException(nameof(ingredientCost));
            
            if (ingredientCost.Amount <= 0)
                throw new DomainException("Ingredient cost must be positive");
            
            // Calculate base price (cost-plus)
            var basePrice = CalculateCostPlusPrice(ingredientCost);
            
            // Adjust for demand (if sales history available)
            if (salesHistory?.Any() == true)
            {
                var demandMultiplier = CalculateDemandMultiplier(salesHistory);
                basePrice = basePrice * demandMultiplier;
            }
            
            // Adjust for competition (if competitor data available)
            if (competitorPrices?.Any() == true)
            {
                basePrice = AdjustForCompetition(basePrice, competitorPrices);
            }
            
            // Ensure minimum profit margin
            var minimumPrice = ingredientCost / (1 - MinimumProfitMargin);
            if (basePrice.Amount < minimumPrice.Amount)
            {
                basePrice = minimumPrice;
            }
            
            return basePrice;
        }
        
        /// <summary>
        /// Applies dynamic pricing adjustments based on time factors.
        /// </summary>
        /// <param name="basePrice">The standard menu price</param>
        /// <param name="timeOfDay">Current time of day</param>
        /// <param name="dayOfWeek">Current day of week</param>
        /// <returns>Adjusted price with time-based factors</returns>
        public Money ApplyDynamicPricing(Money basePrice, DateTime timeOfDay, DayOfWeek dayOfWeek)
        {
            var multiplier = 1.0m;
            
            // Peak hours (lunch 12-2pm, dinner 6-9pm): +10%
            var hour = timeOfDay.Hour;
            if ((hour >= 12 && hour < 14) || (hour >= 18 && hour < 21))
            {
                multiplier += 0.10m;
            }
            
            // Off-peak hours (3-5pm): -15% to drive traffic
            if (hour >= 15 && hour < 17)
            {
                multiplier -= 0.15m;
            }
            
            // Weekend premium (Friday-Sunday): +5%
            if (dayOfWeek >= DayOfWeek.Friday && dayOfWeek <= DayOfWeek.Sunday)
            {
                multiplier += 0.05m;
            }
            
            return new Money(basePrice.Amount * multiplier, basePrice.Currency);
        }
        
        // === Private Helper Methods ===
        
        private Money CalculateCostPlusPrice(Money cost)
        {
            return new Money(
                cost.Amount * (1 + DefaultMarkupPercentage),
                cost.Currency
            );
        }
        
        private decimal CalculateDemandMultiplier(IEnumerable<SaleRecord> salesHistory)
        {
            // High demand (>50 sales/week): +20% premium
            // Medium demand (20-50 sales/week): no change
            // Low demand (<20 sales/week): -10% to stimulate sales
            
            var totalSales = salesHistory.Sum(s => s.QuantitySold);
            
            return totalSales switch
            {
                > 50 => 1.20m,
                < 20 => 0.90m,
                _ => 1.00m
            };
        }
        
        private Money AdjustForCompetition(Money currentPrice, IEnumerable<Money> competitorPrices)
        {
            var avgCompetitorPrice = new Money(
                competitorPrices.Average(p => p.Amount),
                currentPrice.Currency
            );
            
            // Position slightly below competition for value perception
            var targetPrice = avgCompetitorPrice.Amount * 0.95m;
            
            // Take weighted average (70% our calculation, 30% competitive)
            var adjustedAmount = (currentPrice.Amount * 0.7m) + (targetPrice * 0.3m);
            
            return new Money(adjustedAmount, currentPrice.Currency);
        }
    }
}
```

---

## Domain Service Best Practices

### 1. **Stateless Design**
```csharp
// ✅ GOOD: Stateless, operates on parameters
public Money CalculatePrice(Money cost, decimal markup)
{
    return new Money(cost.Amount * (1 + markup), cost.Currency);
}

// ❌ BAD: Stateful, stores internal data
private Money _lastCalculatedPrice; // Don't do this!
public Money CalculatePrice(Money cost)
{
    _lastCalculatedPrice = new Money(cost.Amount * 1.5m, cost.Currency);
    return _lastCalculatedPrice;
}
```

### 2. **Pure Domain Logic (No Infrastructure)**
```csharp
// ✅ GOOD: Pure domain calculation
public Money CalculateRevenue(IEnumerable<SaleRecord> sales)
{
    return new Money(
        sales.Sum(s => s.SaleAmount.Amount),
        "USD"
    );
}

// ❌ BAD: Calls infrastructure (database)
public async Task<Money> CalculateRevenue(int restaurantId)
{
    var sales = await _repository.GetSalesAsync(restaurantId); // NO!
    return CalculateTotalRevenue(sales);
}
```

### 3. **Express Domain Concepts**
```csharp
// ✅ GOOD: Uses ubiquitous language
public IEnumerable<Dish> IdentifyUnderperformingDishes(
    Menu menu,
    RevenueThreshold threshold)
{
    // Domain logic here
}

// ❌ BAD: Generic, non-domain naming
public IEnumerable<Dish> FilterDishes(
    Menu menu,
    Func<Dish, bool> predicate)
{
    // Too generic, doesn't express domain concept
}
```

### 4. **Constructor Injection for Dependencies**
```csharp
// ✅ GOOD: Inject other domain services if needed
public class MenuOptimizationService
{
    private readonly MenuPricingService _pricingService;
    private readonly DishPopularityRankingService _rankingService;
    
    public MenuOptimizationService(
        MenuPricingService pricingService,
        DishPopularityRankingService rankingService)
    {
        _pricingService = pricingService;
        _rankingService = rankingService;
    }
}
```

### 5. **Rich Return Types**
```csharp
// ✅ GOOD: Return domain objects or value objects
public MenuAnalysisReport AnalyzeMenu(Menu menu)
{
    return new MenuAnalysisReport
    {
        TotalRevenue = CalculateRevenue(menu.Dishes),
        TopPerformers = GetTopDishes(menu.Dishes),
        Recommendations = GenerateRecommendations(menu)
    };
}

// ❌ AVOID: Returning primitive obsession
public (decimal, List<int>, string) AnalyzeMenu(Menu menu)
{
    // Unclear what these values represent
}
```

---

## Domain Service vs. Application Service

| Aspect | Domain Service | Application Service |
|--------|----------------|---------------------|
| **Layer** | Domain | Application |
| **Purpose** | Domain logic | Workflow orchestration |
| **Dependencies** | Domain objects only | Repositories, infrastructure |
| **State** | Stateless | Can maintain transaction state |
| **Example** | `MenuPricingService` | `MenuManagementService` |
| **Contains** | Business rules | Use case coordination |

### Example Comparison:

**Domain Service (Domain Layer):**
```csharp
public class MenuPricingService
{
    // Pure calculation, no database access
    public Money CalculateOptimalPrice(Money cost, IEnumerable<SaleRecord> sales)
    {
        // Business logic here
        return optimizedPrice;
    }
}
```

**Application Service (Application Layer):**
```csharp
public class MenuManagementService
{
    private readonly IMenuRepository _menuRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly MenuPricingService _pricingService; // Uses domain service
    
    // Orchestrates workflow using domain service
    public async Task<Money> GetOptimalPriceForDishAsync(int dishId)
    {
        var dish = await _menuRepository.GetDishAsync(dishId);
        var sales = await _saleRepository.GetSalesForDishAsync(dishId);
        
        // Delegates to domain service
        return _pricingService.CalculateOptimalPrice(dish.Cost, sales);
    }
}
```

---

## Testing Domain Services

Domain services are easy to test because they're stateless and have no infrastructure dependencies:

```csharp
[Fact]
public void CalculateOptimalPrice_WithHighDemand_AppliesPremium()
{
    // Arrange
    var service = new MenuPricingService();
    var cost = new Money(5.00m, "USD");
    var highDemandSales = CreateSaleRecords(quantity: 100); // 100 sales
    
    // Act
    var result = service.CalculateOptimalPrice(cost, highDemandSales);
    
    // Assert
    Assert.True(result.Amount > cost.Amount * 1.5m); // Should have premium
}
```

---

## Location in Clean Architecture

1. Keep interfaces and implementations of domain services in the Domain layer.
2. Avoid any references to infrastructure or application layers.
3.Choose implementation details location based on dependency direction (infrastructure depends on domain, not vice versa; application depends on domain, not vice vers; presentation depends on application, not vice versa).
  - if pure domain logic, keep in domain layer - Domain Services folder. Never put application or infrastructure code here.
  - if needing infrastructure (e.g., email sending), define interface in domain layer, implement in infrastructure layer.(`SmartMenuOptim/Infrastructure/Services/AdvancedPricingService.cs`) Never put infrastructure code in domain layer.
  - if needing application logic (e.g., workflow, transactions, events, orchestration), implement in application layer. Never call application services from domain layer.
  - if needing presentation logic (e.g., UI concerns) access, implement in presentation layer. Never call domain services directly from presentation layer.

## Integration with Other Layers

**Domain Layer (this folder):**
- Define domain services with pure business logic

**Application Layer:**
- Call domain services from application services, such as use cases that orchestrate domain operations.
- Provide data from repositories
- Coordinate transactions

**Infrastructure Layer:**
- Register domain services in DI container
- No direct interaction needed
- When needed, use factories or adapters to provide dependencies
- Implementations that may need external resources should be in infrastructure services, not domain services, such as email sending or data access, API calls, Azure services, etc.

**Presentation Layer:**
- Never call domain services directly
- Always go through application services by instance: call application services that in turn call domain services.


---

## Common Mistakes to Avoid

❌ **Putting Repository Calls in Domain Services**
```csharp
// WRONG!
public class OrderService
{
    private readonly IOrderRepository _repo;
    
    public async Task<decimal> GetTotalRevenue() // DON'T DO THIS
    {
        var orders = await _repo.GetAllAsync();
        return orders.Sum(o => o.Total);
    }
}
```

❌ **Making Domain Services Stateful**
```csharp
// WRONG!
public class PricingService
{
    private decimal _lastPrice; // Domain services should be stateless!
}
```

❌ **Using Domain Services for Application Workflow**
```csharp
// WRONG! This is an application service, not domain service
public class MenuService
{
    public async Task CreateMenuAndNotifyStaff(Menu menu)
    {
        await SaveToDatabase(menu);
        await SendEmailNotification(); // Application concern!
    }
}
```

---

## Next Steps

1. Identify domain logic in entities that should move to services
2. Create domain services for cross-aggregate operations
3. Ensure services are stateless and infrastructure-free
4. Write comprehensive unit tests
5. Register services in DI container (Infrastructure layer)
6. Document service responsibilities using ubiquitous language

---

## SOLID Principles & Domain Services

Domain services in SmartMenuOptim follow **SOLID principles** to ensure clean, maintainable, and testable code:

### Real-World Example: `ReviewSentimentAnalysisService`

This service demonstrates all five SOLID principles in practice:

**🔹 Single Responsibility (SRP)**
- `ReviewSentimentAnalysisService` = Business logic ONLY (categorization, metrics, anomaly detection)
- `SentimentService` = Infrastructure ONLY (Azure AI integration)
- Each class has ONE reason to change

**🔹 Open/Closed (OCP)**
- Can swap Azure AI for Google/AWS/Local ML without changing domain code
- Extension via new `ISentimentAnalyzer` implementations

**🔹 Liskov Substitution (LSP)**
- Any `ISentimentAnalyzer` implementation works interchangeably
- Mock for testing, Azure for production - domain service doesn't care

**🔹 Interface Segregation (ISP)**
- `ISentimentAnalyzer` has only 2 focused methods
- No "fat" interfaces with unused dependencies

**🔹 Dependency Inversion (DIP)**
- Domain depends on `ISentimentAnalyzer` (abstraction)
- Infrastructure implements `ISentimentAnalyzer` (concrete)
- High-level modules don't depend on low-level modules

### 📚 Comprehensive SOLID Analysis

For a detailed explanation of SOLID principles with real code examples, diagrams, and implementation guidance, see:

**→ [SOLID Principles in Practice - Full Analysis](/docs/architecture/CLEAN_ARCHITECTURE_FULL_ANALYSIS.md#-solid-principles-in-practice)**

This comprehensive guide includes:
- ✅ Plain-language explanations of each principle
- ✅ Real-world analogies and examples
- ✅ Code samples from actual SmartMenuOptim services
- ✅ Architecture diagrams and dependency flows
- ✅ Benefits, anti-patterns, and best practices
- ✅ Hexagonal Architecture (Ports & Adapters) pattern

---

## References

- **Domain-Driven Design** by Eric Evans (Chapter 5: A Model Expressed in Software)
- **Implementing Domain-Driven Design** by Vaughn Vernon
- **Clean Architecture** by Robert C. Martin
- **Microsoft Architecture Guide**: [Domain Services](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice)
- **SOLID Principles**: [Clean Architecture Full Analysis](/docs/architecture/CLEAN_ARCHITECTURE_FULL_ANALYSIS.md#-solid-principles-in-practice)

---

*This folder represents the Domain Services of SmartMenuOptim according to Clean Architecture and DDD principles.*
