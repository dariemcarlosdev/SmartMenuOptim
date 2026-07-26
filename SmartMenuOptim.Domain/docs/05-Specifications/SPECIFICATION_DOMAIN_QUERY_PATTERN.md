# Specifications - Domain Query Pattern

## 📚 Overview

This folder contains **Specification Pattern** implementations that encapsulate query logic in a domain-centric, reusable, and testable way. Specifications allow us to define complex query criteria, filtering, sorting, and includes without coupling our Domain layer to infrastructure concerns like Entity Framework Core.

---

## 🎯 What is the Specification Pattern?

The **Specification Pattern** is a domain-driven design pattern that encapsulates business rules and query logic into reusable, composable objects. Instead of scattering query logic across services and repositories, we centralize it in specification classes that express clear business intent.

### **Key Benefits**

✅ **Domain-Centric** - Query logic lives in the Domain layer where it belongs  
✅ **Infrastructure-Agnostic** - No dependency on ORM or database details  
✅ **Reusable** - Same specification can be used across multiple services  
✅ **Testable** - Specifications are POCOs that can be tested without a database  
✅ **Composable** - Can be combined to create complex queries  
✅ **Maintainable** - Query changes are isolated to specification classes  

---

## 🏗️ Architecture

### **Clean Architecture Compliance**

```
┌─────────────────────────────────────────────┐
│  Application Layer (Services)               │
│  ✅ Uses specifications to query data       │
└─────────────────────────────────────────────┘
                    ↓ depends on
┌─────────────────────────────────────────────┐
│  Domain Layer (Specifications)              │
│  ✅ ISpecification<T>                       │
│  ✅ BaseSpecification<T>                    │
│  ✅ Concrete specifications (DishSpec, etc)│
└─────────────────────────────────────────────┘
                    ↑ implements
┌─────────────────────────────────────────────┐
│  Infrastructure Layer (Repository)          │
│  ✅ Translates specs to EF Core queries    │
└─────────────────────────────────────────────┘
```

**Why This Matters:**
- Domain layer has NO knowledge of EF Core, SQL, or any infrastructure
- Infrastructure layer translates domain specifications into database queries
- Application layer uses domain concepts, not database implementation details

---

## 📁 Folder Structure

```
Specifications/
├── ISpecification.cs                    # Core abstraction
├── BaseSpecification.cs                 # Base implementation
├── README.md                            # This file
├── DishSpecifications/
│   └── DishWithDetailsSpecification.cs  # Dish query specifications
├── ReviewSpecifications/
│   ├── ReviewWithDetailsSpecification.cs    # Review with includes
│   └── FilteredReviewsSpecification.cs      # Filtered review queries
└── SaleRecordSpecifications/
    └── SaleRecordWithDetailsSpecification.cs # Sale record analytics queries
```

---

## 🔧 Core Components

### **1. ISpecification\<T\>**

The core abstraction defining what a specification provides:

```csharp
public interface ISpecification<T> where T : class
{
    Expression<Func<T, bool>>? Criteria { get; }         // WHERE clause
    List<Expression<Func<T, object>>> Includes { get; }  // Navigation properties
    List<string> IncludeStrings { get; }                 // String-based includes
    Expression<Func<T, object>>? OrderBy { get; }        // ORDER BY ASC
    Expression<Func<T, object>>? OrderByDescending { get; } // ORDER BY DESC
    int? Take { get; }                                    // LIMIT
    int? Skip { get; }                                    // OFFSET
    bool IsPagingEnabled { get; }                        // Pagination flag
}
```

### **2. BaseSpecification\<T\>**

Base implementation providing a fluent API for building specifications:

```csharp
public abstract class BaseSpecification<T> : ISpecification<T> where T : class
{
    protected BaseSpecification() { }
    protected BaseSpecification(Expression<Func<T, bool>> criteria) { }
    
    protected void AddInclude(Expression<Func<T, object>> includeExpression) { }
    protected void AddInclude(string includeString) { }
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression) { }
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression) { }
    protected void ApplyPaging(int skip, int take) { }
}
```

---

## 📖 Specifications Reference

### **1. DishWithDetailsSpecification**

**Location:** `DishSpecifications/DishWithDetailsSpecification.cs`

**Purpose:** Retrieve dishes with complete related data (Category, Restaurant)

**Constructors:**
```csharp
// Get a specific dish by ID with all details
new DishWithDetailsSpecification(dishId: 5)

// Get all active dishes for a restaurant
new DishWithDetailsSpecification(restaurantId: 1, activeOnly: true)
```

**Example Usage:**
```csharp
// In a service
var spec = new DishWithDetailsSpecification(dishId: 10);
var dish = await _unitOfWork.Dishes.FirstOrDefaultAsync(spec);
```

**What It Includes:**
- ✅ Dish.Category
- ✅ Dish.Restaurant
- ✅ Optional filtering by availability
- ✅ Ordered by name

---

### **2. ReviewWithDetailsSpecification**

**Location:** `ReviewSpecifications/ReviewWithDetailsSpecification.cs`

**Purpose:** Retrieve reviews with customer and dish information

**Constructors:**
```csharp
// Get all reviews with details
new ReviewWithDetailsSpecification()

// Get a specific review by ID
new ReviewWithDetailsSpecification(reviewId: 5)

// Get reviews for a specific dish
new ReviewWithDetailsSpecification(dishName: "Burger", caseSensitive: false)

// Get reviews by sentiment score
new ReviewWithDetailsSpecification(targetSentiment: 0.8, tolerance: 0.03)
```

**Example Usage:**
```csharp
// In ReviewsController
var spec = new ReviewWithDetailsSpecification(reviewId);
var review = await _unitOfWork.Reviews.FirstOrDefaultAsync(spec);
```

**What It Includes:**
- ✅ Review.Customer
- ✅ Review.Dish
- ✅ Ordered by SentimentScore (descending)

---

### **3. FilteredReviewsSpecification**

**Location:** `ReviewSpecifications/FilteredReviewsSpecification.cs`

**Purpose:** Flexible filtering for reviews with optional parameters

**Constructor:**
```csharp
new FilteredReviewsSpecification(
    dishName: "Pizza",        // Optional: filter by dish name
    targetSentiment: 0.75,    // Optional: filter by sentiment
    sentimentTolerance: 0.03  // Optional: sentiment tolerance
)
```

**Example Usage:**
```csharp
// In ReviewsController - filter by both criteria
var spec = new FilteredReviewsSpecification(dishName: "Burger", targetSentiment: 0.8);
var reviews = await _unitOfWork.Reviews.FindAsync(spec);

// Filter by dish name only
var spec = new FilteredReviewsSpecification(dishName: "Pizza");

// Filter by sentiment only
var spec = new FilteredReviewsSpecification(targetSentiment: 0.9);

// Get all reviews
var spec = new FilteredReviewsSpecification();
```

**What It Includes:**
- ✅ Review.Customer
- ✅ Review.Dish
- ✅ Filters in database (not memory)
- ✅ Ordered by SentimentScore (descending)

---

### **4. SaleRecordWithDetailsSpecification**

**Location:** `SaleRecordSpecifications/SaleRecordWithDetailsSpecification.cs`

**Purpose:** Comprehensive sale record queries for reporting and analytics

**Constructors:**
```csharp
// Get all sale records with complete details
new SaleRecordWithDetailsSpecification()

// Get a specific sale record by ID
new SaleRecordWithDetailsSpecification(saleRecordId: 5)

// Get sale records for a specific dish
new SaleRecordWithDetailsSpecification(dishId: 10, includeDetails: true)

// Get sale records for a date range
new SaleRecordWithDetailsSpecification(
    startDate: DateTime.UtcNow.AddDays(-30),
    endDate: DateTime.UtcNow
)
```

**Example Usage:**
```csharp
// In SaleRecordsController
var spec = new SaleRecordWithDetailsSpecification();
var saleRecords = await _unitOfWork.SaleRecords.FindAsync(spec);

// Last 30 days sales
var startDate = DateTime.UtcNow.AddDays(-30);
var endDate = DateTime.UtcNow;
var spec = new SaleRecordWithDetailsSpecification(startDate, endDate);
var recentSales = await _unitOfWork.SaleRecords.FindAsync(spec);
```

**What It Includes:**
- ✅ SaleRecord.Dish
- ✅ Dish.Category (nested)
- ✅ Dish.Reviews (nested)
- ✅ Dish.Restaurant (nested)
- ✅ For date range: ordered by SaleDate (descending)

**Special Note:** Demonstrates **string-based includes** for nested navigation properties:
```csharp
AddInclude("Dish.Category");    // Nested include
AddInclude("Dish.Reviews");     // Nested include
AddInclude("Dish.Restaurant");  // Nested include
```

---

## 🎓 How to Create a Specification

### **Step 1: Create the Specification Class**

```csharp
using SmartMenuOptim.Domain.Entities.YourEntity;

namespace SmartMenuOptim.Domain.Specifications.YourEntitySpecifications
{
    /// <summary>
    /// Specification for [describe purpose].
    /// </summary>
    public class YourEntitySpec : BaseSpecification<YourEntity>
    {
        /// <summary>
        /// Initializes a specification with [description].
        /// </summary>
        /// <param name="someParam">Parameter description.</param>
        public YourEntitySpec(int someParam)
            : base(e => e.SomeProperty == someParam) // Filtering criteria
        {
            // Add navigation properties to include
            AddInclude(e => e.RelatedEntity);
            
            // For nested properties, use string-based includes
            AddInclude("RelatedEntity.NestedProperty");
            
            // Add ordering
            ApplyOrderBy(e => e.Name);
            // OR
            ApplyOrderByDescending(e => e.CreatedDate);
            
            // Add pagination (optional)
            ApplyPaging(skip: 0, take: 20);
        }
    }
}
```

### **Step 2: Use in Service or Controller**

```csharp
public class YourService
{
    private readonly IUnityOfWork _unitOfWork;
    
    public async Task<IEnumerable<YourDto>> GetDataAsync(int someParam)
    {
        // Create the specification
        var spec = new YourEntitySpec(someParam);
        
        // Use it with the repository
        var entities = await _unitOfWork.YourEntities.FindAsync(spec);
        
        // Map to DTOs
        return _mapper.Map<IEnumerable<YourDto>>(entities);
    }
}
```

---

## 🔥 Common Patterns

### **Pattern 1: Simple Filtering**
```csharp
public class ActiveDishesSpec : BaseSpecification<Dish>
{
    public ActiveDishesSpec() 
        : base(d => d.IsAvailable)
    {
        ApplyOrderBy(d => d.Name);
    }
}
```

### **Pattern 2: Filtering with Includes**
```csharp
public class DishByIdSpec : BaseSpecification<Dish>
{
    public DishByIdSpec(int id) 
        : base(d => d.Id == id)
    {
        AddInclude(d => d.Category);
        AddInclude(d => d.Restaurant);
    }
}
```

### **Pattern 3: Complex Filtering**
```csharp
public class DishSearchSpec : BaseSpecification<Dish>
{
    public DishSearchSpec(string searchTerm, decimal? minPrice, decimal? maxPrice)
        : base(d => 
            (string.IsNullOrEmpty(searchTerm) || d.Name.Contains(searchTerm)) &&
            (!minPrice.HasValue || d.Price >= minPrice) &&
            (!maxPrice.HasValue || d.Price <= maxPrice))
    {
        AddInclude(d => d.Category);
        ApplyOrderBy(d => d.Price);
    }
}
```

### **Pattern 4: Pagination**
```csharp
public class PagedDishesSpec : BaseSpecification<Dish>
{
    public PagedDishesSpec(int pageIndex, int pageSize)
    {
        ApplyOrderBy(d => d.Name);
        ApplyPaging(skip: pageIndex * pageSize, take: pageSize);
    }
}
```

### **Pattern 5: Nested Includes (String-Based)**
```csharp
public class OrderWithDetailsSpec : BaseSpecification<Order>
{
    public OrderWithDetailsSpec(int orderId)
        : base(o => o.Id == orderId)
    {
        AddInclude(o => o.Customer);
        AddInclude("OrderItems.Dish");              // Nested
        AddInclude("OrderItems.Dish.Category");     // Deep nesting
    }
}
```

---

## ✅ Best Practices

### **DO:**

1. ✅ **Name Descriptively**
   ```csharp
   ActiveDishesByRestaurantSpec
   ExpiredPromotionsSpec
   CustomerOrderHistoryWithDetailsSpec
   ```

2. ✅ **One Specification = One Query Intent**
   - Each spec should represent a specific business query
   - Don't create overly generic specifications

3. ✅ **Use Constructor Parameters for Flexibility**
   ```csharp
   public class DishSpec : BaseSpecification<Dish>
   {
       public DishSpec(int restaurantId, bool activeOnly = true)
           : base(d => d.RestaurantId == restaurantId && 
                      (!activeOnly || d.IsAvailable))
       { }
   }
   ```

4. ✅ **Add XML Documentation**
   - Explain what the specification does
   - Document constructor parameters
   - Describe included navigation properties

5. ✅ **Use String-Based Includes for Nested Properties**
   ```csharp
   AddInclude("Dish.Category");        // ✅ Good for nested
   AddInclude(d => d.Dish);            // ✅ Good for direct
   ```

### **DON'T:**

1. ❌ **Don't Put Business Logic in Specifications**
   - Specifications are for querying, not domain logic
   - Keep complex calculations in domain entities

2. ❌ **Don't Create a "Get Everything" Specification**
   - Avoid specifications that try to handle all scenarios
   - Create focused, purpose-driven specs

3. ❌ **Don't Duplicate Logic**
   - If multiple specs share logic, consider refactoring
   - Use inheritance or composition

4. ❌ **Don't Mix Concerns**
   - Keep query logic separate from business rules
   - Specifications are about "what to retrieve", not "what to do"

---

## 🧪 Testing Specifications

### **Unit Test Example**

```csharp
[Fact]
public void ActiveDishesSpec_FiltersCorrectly()
{
    // Arrange
    var spec = new ActiveDishesSpec();
    var testData = new List<Dish>
    {
        new Dish { Id = 1, IsAvailable = true },
        new Dish { Id = 2, IsAvailable = false },
        new Dish { Id = 3, IsAvailable = true },
    };
    
    // Act
    var result = testData.AsQueryable()
        .Where(spec.Criteria)
        .ToList();
    
    // Assert
    Assert.Equal(2, result.Count);
    Assert.All(result, d => Assert.True(d.IsAvailable));
}

[Fact]
public void DishWithDetailsSpec_HasCorrectIncludes()
{
    // Arrange
    var spec = new DishWithDetailsSpecification(dishId: 1);
    
    // Assert
    Assert.NotNull(spec.Criteria);
    Assert.Equal(2, spec.Includes.Count); // Category and Restaurant
}
```

---

## 📊 Performance Considerations

### **Efficient Database Queries**

Specifications translate to efficient SQL:

```csharp
// Specification
var spec = new DishWithDetailsSpecification(restaurantId: 1);
var dishes = await _unitOfWork.Dishes.FindAsync(spec);

// Generated SQL (by Infrastructure)
SELECT d.*, c.*, r.*
FROM Dishes d
LEFT JOIN Categories c ON d.CategoryId = c.Id
LEFT JOIN Restaurants r ON d.RestaurantId = r.Id
WHERE d.RestaurantId = 1
ORDER BY d.Name
```

**Benefits:**
- ✅ Single database query (no N+1 problem)
- ✅ Filters in database (not in memory)
- ✅ Proper SQL JOINs for includes
- ✅ AsNoTracking() applied for read-only queries

---

## 🎯 When to Use Specifications

### **Use Specifications When:**

✅ You need to filter entities with complex criteria  
✅ You need to include related navigation properties  
✅ You need to reuse the same query in multiple places  
✅ You want to keep query logic in the Domain layer  
✅ You need testable query logic without a database  

### **Use Simple Repository Methods When:**

✅ Getting a single entity by ID (no includes)  
✅ Getting all entities without filtering  
✅ Simple, one-time queries  

---

## 📚 Additional Resources

- [Repository Refactoring Guide](../../docs/architecture/REPOSITORY_REFACTORING_GUIDE.md)
- [Before/After Comparison](../../docs/architecture/BEFORE_AFTER_COMPARISON.md)
- [Quick Reference](../../docs/architecture/SPECIFICATION_QUICK_REFERENCE.md)
- [Reviews Controller Migration](../../docs/architecture/REVIEWS_CONTROLLER_MIGRATION.md)
- [SaleRecords Controller Migration](../../docs/architecture/SALERECORDS_CONTROLLER_MIGRATION.md)

---

## 🎓 Summary

The Specification Pattern in this project provides:

1. **Clean Architecture Compliance** - Domain stays infrastructure-agnostic
2. **Testability** - Specifications are testable POCOs
3. **Reusability** - Same spec across multiple services
4. **Maintainability** - Query logic centralized and focused
5. **Performance** - Translates to efficient database queries
6. **Expressiveness** - Clear business intent in code

**Remember:** Specifications are about **expressing WHAT data you need**, not **HOW to get it**. The Infrastructure layer handles the HOW.

---

**Happy Querying! 🚀**
