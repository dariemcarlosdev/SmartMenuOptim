# Quick Reference: Using the New Repository Pattern

## 🚀 Quick Start

### **1. Create a Specification**
```csharp
// Domain/Specifications/YourEntitySpecifications/YourSpec.cs
public class YourSpec : BaseSpecification<YourEntity>
{
    public YourSpec(int someId)
        : base(x => x.SomeProperty == someId) // Filtering
    {
        AddInclude(x => x.NavigationProperty);    // Eager load
        ApplyOrderBy(x => x.Name);                // Sort
    }
}
```

### **2. Use in Service**
```csharp
var spec = new YourSpec(someId);
var results = await _unitOfWork.YourEntities.FindAsync(spec);
```

---

## 📚 Common Patterns

### **Get Single Entity with Includes**
```csharp
public class DishByIdSpec : BaseSpecification<Dish>
{
    public DishByIdSpec(int id) : base(d => d.Id == id)
    {
        AddInclude(d => d.Category);
        AddInclude(d => d.Restaurant);
    }
}

// Usage
var spec = new DishByIdSpec(dishId);
var dish = await _unitOfWork.Dishes.FirstOrDefaultAsync(spec);
```

### **Get Filtered Collection**
```csharp
public class ActiveDishesSpec : BaseSpecification<Dish>
{
    public ActiveDishesSpec() : base(d => d.IsAvailable)
    {
        ApplyOrderBy(d => d.Name);
    }
}

// Usage
var spec = new ActiveDishesSpec();
var dishes = await _unitOfWork.Dishes.FindAsync(spec);
```

### **Get with Pagination**
```csharp
public class PagedDishesSpec : BaseSpecification<Dish>
{
    public PagedDishesSpec(int page, int pageSize)
    {
        ApplyPaging(skip: page * pageSize, take: pageSize);
        ApplyOrderByDescending(d => d.CreatedAt);
    }
}

// Usage
var spec = new PagedDishesSpec(pageIndex: 0, pageSize: 10);
var dishes = await _unitOfWork.Dishes.FindAsync(spec);
```

### **Complex Filtering**
```csharp
public class DishSearchSpec : BaseSpecification<Dish>
{
    public DishSearchSpec(int restaurantId, decimal? minPrice, decimal? maxPrice, string? searchTerm)
        : base(d => d.RestaurantId == restaurantId 
                 && (!minPrice.HasValue || d.Price >= minPrice)
                 && (!maxPrice.HasValue || d.Price <= maxPrice)
                 && (string.IsNullOrEmpty(searchTerm) || d.Name.Contains(searchTerm)))
    {
        AddInclude(d => d.Category);
        ApplyOrderBy(d => d.Price);
    }
}
```

### **Nested Includes (String-based)**
```csharp
public class OrderWithDetailsSpec : BaseSpecification<Order>
{
    public OrderWithDetailsSpec(int orderId) : base(o => o.Id == orderId)
    {
        AddInclude("OrderItems.Dish");           // Nested include
        AddInclude("OrderItems.Dish.Category");  // Deep nesting
        AddInclude("Customer");
    }
}
```

### **Count with Filter**
```csharp
public class ActiveDishCountSpec : BaseSpecification<Dish>
{
    public ActiveDishCountSpec(int restaurantId)
        : base(d => d.RestaurantId == restaurantId && d.IsAvailable)
    {
        // No includes needed for count
    }
}

// Usage
var spec = new ActiveDishCountSpec(restaurantId);
var count = await _unitOfWork.Dishes.CountAsync(spec);
```

---

## 🎯 Repository Methods Reference

### **IRepository<T> Methods**

| Method | Use Case | Returns |
|--------|----------|---------|
| `FindAsync(spec)` | Get multiple entities | `IEnumerable<T>` |
| `FirstOrDefaultAsync(spec)` | Get single entity | `T?` |
| `CountAsync(spec)` | Count matching entities | `int` |
| `GetByIdAsync(id)` | Get by primary key | `T?` |
| `GetAllAsync()` | Get all (use sparingly!) | `IEnumerable<T>` |
| `Query()` | Advanced LINQ (avoid if possible) | `IQueryable<T>` |
| `AddAsync(entity)` | Insert | `Task` |
| `Update(entity)` | Update | `void` |
| `Delete(entity)` | Delete | `void` |
| `ExistsAsync(id)` | Check existence | `bool` |

---

## ✅ Best Practices

### **DO:**
✅ Create specifications for reusable queries  
✅ Name specifications descriptively (`ActiveDishesByRestaurantSpec`)  
✅ Put specifications in `Domain/Specifications/{Entity}Specifications/`  
✅ Use `FindAsync()` for collections  
✅ Use `FirstOrDefaultAsync()` for single entities  
✅ Use `CountAsync()` for counting  
✅ Keep specifications focused (one query intent)  

### **DON'T:**
❌ Use `GetAllAsync()` and filter in memory  
❌ Use `Query()` in application layer  
❌ Put query logic in services  
❌ Create generic "get everything" specifications  
❌ Use `Expression<Func<T, object>>[]` directly  

---

## 🔄 Migration Examples

### **Old Pattern → New Pattern**

#### ❌ OLD
```csharp
var dishes = await _unitOfWork.Dishes.GetAllAsync(
    d => d.Category,
    d => d.Restaurant
);
```

#### ✅ NEW
```csharp
var spec = new DishWithDetailsSpec();
var dishes = await _unitOfWork.Dishes.FindAsync(spec);
```

---

#### ❌ OLD
```csharp
var dish = await _unitOfWork.Dishes.GetByIdAsync(id,
    d => d.Category,
    d => d.Restaurant
);
```

#### ✅ NEW
```csharp
var spec = new DishByIdSpec(id);
var dish = await _unitOfWork.Dishes.FirstOrDefaultAsync(spec);
```

---

#### ❌ OLD
```csharp
var allDishes = await _unitOfWork.Dishes.GetAllAsync();
var filtered = allDishes
    .Where(d => d.RestaurantId == restaurantId && d.IsAvailable)
    .OrderBy(d => d.Name);
```

#### ✅ NEW
```csharp
var spec = new ActiveDishesByRestaurantSpec(restaurantId);
var filtered = await _unitOfWork.Dishes.FindAsync(spec);
```

---

## 🧪 Testing

### **Test a Specification**
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
    };
    
    // Act
    var result = testData.AsQueryable()
        .Where(spec.Criteria)
        .ToList();
    
    // Assert
    Assert.Single(result);
    Assert.Equal(1, result[0].Id);
}
```

### **Mock in Service Test**
```csharp
[Fact]
public async Task GetActiveDishes_ReturnsActiveDishes()
{
    // Arrange
    var mockRepo = new Mock<IRepository<Dish>>();
    mockRepo
        .Setup(r => r.FindAsync(It.IsAny<ISpecification<Dish>>()))
        .ReturnsAsync(new List<Dish> { /* test data */ });
    
    var mockUoW = new Mock<IUnityOfWork>();
    mockUoW.Setup(u => u.Dishes).Returns(mockRepo.Object);
    
    var service = new DishService(mockUoW.Object);
    
    // Act
    var result = await service.GetActiveDishesAsync();
    
    // Assert
    Assert.NotNull(result);
}
```

---

## 📖 BaseSpecification Protected Methods

```csharp
protected void AddInclude(Expression<Func<T, object>> includeExpression)
protected void AddInclude(string includeString)
protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
protected void ApplyPaging(int skip, int take)
```

---

## 🆘 Troubleshooting

### **"How do I add multiple filters?"**
```csharp
// Combine in base constructor
public class ComplexSpec : BaseSpecification<Dish>
{
    public ComplexSpec(int restaurantId, bool activeOnly)
        : base(d => d.RestaurantId == restaurantId 
                 && (!activeOnly || d.IsAvailable)
                 && d.Price > 0)
    { }
}
```

### **"How do I handle optional filters?"**
```csharp
public class FlexibleSpec : BaseSpecification<Dish>
{
    public FlexibleSpec(int restaurantId, string? searchTerm = null)
        : base(d => d.RestaurantId == restaurantId 
                 && (string.IsNullOrEmpty(searchTerm) || d.Name.Contains(searchTerm)))
    { }
}
```

### **"How do I do OR conditions?"**
```csharp
public class OrSpec : BaseSpecification<Dish>
{
    public OrSpec(string term)
        : base(d => d.Name.Contains(term) || d.Description.Contains(term))
    { }
}
```

---

## 📝 Specification Template

```csharp
using SmartMenuOptim.Domain.Specifications;

namespace SmartMenuOptim.Domain.Specifications.{Entity}Specifications
{
    /// <summary>
    /// Specification for [describe what this specification does].
    /// </summary>
    public class {Name}Spec : BaseSpecification<{Entity}>
    {
        /// <summary>
        /// Initializes a new instance of the specification.
        /// </summary>
        /// <param name="paramName">Parameter description.</param>
        public {Name}Spec(/* parameters */)
            : base(/* criteria expression */)
        {
            // AddInclude() calls
            // ApplyOrderBy() calls
            // ApplyPaging() calls
        }
    }
}
```

---

## 🎓 Remember

1. **Specifications = Domain Queries**  
   They live in the Domain layer and express business intent

2. **One Spec = One Query Purpose**  
   Create specific specs, not generic ones

3. **Test Specifications Independently**  
   They're POCOs - easy to test!

4. **Infrastructure Translates**  
   Specs are abstract - EF Core translates them

5. **Keep It Simple**  
   Start with basic specs, add complexity as needed

---

**Happy Coding! 🚀**
