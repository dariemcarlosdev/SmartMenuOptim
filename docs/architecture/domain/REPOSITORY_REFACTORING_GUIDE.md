# Clean Architecture Repository Refactoring Guide

## 📋 Overview

This document explains the Clean Architecture refactoring of the repository layer, removing EF Core coupling from the Domain layer and implementing the Specification Pattern.

---

## ✅ What Was Fixed

### **Before (Architectural Violations)**

#### ❌ Problem: Infrastructure Leakage in Domain
```csharp
// Domain/Repositories/IRepositoryWithIncludes.cs - WRONG!
public interface IRepositoryWithIncludes<T> : IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
    Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);
}
```

**Issues:**
- `Expression<Func<T, object>>[]` for "includes" is **EF Core-specific**
- Domain layer knows about ORM implementation details
- Violates **Dependency Inversion Principle**
- Not testable without infrastructure

---

### **After (Clean Architecture Compliant)**

#### ✅ Solution: Specification Pattern

```csharp
// Domain/Repositories/IRepository.cs - CORRECT!
public interface IRepository<T> where T : class
{
    // Basic CRUD
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    
    // Specification-based queries (Domain-centric)
    Task<IEnumerable<T>> FindAsync(ISpecification<T> spec);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec);
    Task<int> CountAsync(ISpecification<T> spec);
    
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<bool> ExistsAsync(int id);
}
```

---

## 🎯 Key Improvements

| Aspect | Before | After |
|--------|--------|-------|
| **Domain Purity** | ❌ Coupled to EF Core | ✅ Infrastructure-agnostic |
| **Testability** | ❌ Requires EF Core mocks | ✅ Simple interface mocks |
| **Query Logic** | ❌ Scattered across services | ✅ Encapsulated in specifications |
| **Maintainability** | ❌ Tight coupling | ✅ Loose coupling |
| **DDD Compliance** | ❌ Infrastructure in Domain | ✅ Domain-centric design |

---

## 📚 Specification Pattern Usage

### **1. Create a Specification**

```csharp
// Domain/Specifications/DishSpecifications/ActiveDishesByRestaurantSpec.cs
public class ActiveDishesByRestaurantSpec : BaseSpecification<Dish>
{
    public ActiveDishesByRestaurantSpec(int restaurantId) 
        : base(d => d.RestaurantId == restaurantId && d.IsAvailable)
    {
        AddInclude(d => d.Category);
        AddInclude(d => d.Restaurant);
        ApplyOrderBy(d => d.Name);
    }
}
```

### **2. Use in Application Layer**

```csharp
// Application/Services/DishService.cs
public class DishService
{
    private readonly IUnityOfWork _unitOfWork;

    public async Task<IEnumerable<DishDto>> GetActiveDishesByRestaurantAsync(int restaurantId)
    {
        var spec = new ActiveDishesByRestaurantSpec(restaurantId);
        var dishes = await _unitOfWork.Dishes.FindAsync(spec);
        return _mapper.Map<IEnumerable<DishDto>>(dishes);
    }
}
```

### **3. Complex Specifications with Pagination**

```csharp
public class PagedDishesSpec : BaseSpecification<Dish>
{
    public PagedDishesSpec(int pageIndex, int pageSize, int restaurantId)
        : base(d => d.RestaurantId == restaurantId)
    {
        AddInclude(d => d.Category);
        ApplyOrderByDescending(d => d.CreatedAt);
        ApplyPaging(pageIndex * pageSize, pageSize);
    }
}

// Usage
var spec = new PagedDishesSpec(pageIndex: 0, pageSize: 10, restaurantId: 1);
var dishes = await _unitOfWork.Dishes.FindAsync(spec);
```

---

## 🏗️ Architecture Layers

### **Domain Layer** (No Infrastructure Dependencies)
```
SmartMenuOptim.Domain/
├── Specifications/
│   ├── ISpecification.cs          ✅ Pure abstraction
│   ├── BaseSpecification.cs       ✅ Domain logic
│   └── DishSpecifications/
│       └── DishWithDetailsSpec.cs ✅ Business rules
└── Repositories/
    ├── IRepository.cs              ✅ Infrastructure-agnostic
    └── IUnityOfWork.cs             ✅ Transaction boundary
```

### **Infrastructure Layer** (Implements Domain Contracts)
```
SmartMenuOptim.Infrastructure/
└── Persistence/
    └── Repositories/
        ├── Repository.cs           ✅ Translates specs to EF Core
        └── UnityOfWork.cs          ✅ EF Core implementation
```

---

## 🔄 Migration Guide

### **Old Pattern (Remove)**
```csharp
// ❌ OLD - Direct EF Core usage
var dishes = await _unitOfWork.Dishes.GetAllAsync(
    d => d.Category,
    d => d.Restaurant
);
```

### **New Pattern (Use)**
```csharp
// ✅ NEW - Specification pattern
var spec = new DishWithDetailsSpecification(restaurantId);
var dishes = await _unitOfWork.Dishes.FindAsync(spec);
```

---

## 📖 Common Specification Patterns

### **1. Single Entity with Includes**
```csharp
public class DishByIdSpec : BaseSpecification<Dish>
{
    public DishByIdSpec(int dishId) : base(d => d.Id == dishId)
    {
        AddInclude(d => d.Category);
        AddInclude(d => d.Restaurant);
    }
}
```

### **2. Filtered Collection**
```csharp
public class ExpensiveDishesSpec : BaseSpecification<Dish>
{
    public ExpensiveDishesSpec(decimal minPrice)
        : base(d => d.Price >= minPrice && d.IsAvailable)
    {
        ApplyOrderByDescending(d => d.Price);
    }
}
```

### **3. Search with String Includes**
```csharp
public class DishWithReviewsSpec : BaseSpecification<Dish>
{
    public DishWithReviewsSpec(int dishId) : base(d => d.Id == dishId)
    {
        AddInclude("Reviews.Customer"); // Nested include
    }
}
```

---

## 🧪 Testing Benefits

### **Before (Difficult)**
```csharp
// ❌ Required EF Core In-Memory DB or complex mocking
var mockRepo = new Mock<IRepositoryWithIncludes<Dish>>();
mockRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Dish, object>>[]>()))
    .ReturnsAsync(dishes);
```

### **After (Simple)**
```csharp
// ✅ Simple interface mocking
var mockRepo = new Mock<IRepository<Dish>>();
mockRepo.Setup(r => r.FindAsync(It.IsAny<ISpecification<Dish>>()))
    .ReturnsAsync(dishes);
```

---

## ⚠️ Important Notes

1. **Use Specifications for Complex Queries**
   - Filtering + Includes + Ordering → Use Specification
   - Simple lookups → Use `GetByIdAsync()` or `GetAllAsync()`

2. **Avoid Query() When Possible**
   - `Query()` exposes `IQueryable` (some coupling)
   - Prefer specifications for domain logic

3. **Infrastructure Translation**
   - Specifications are domain concepts
   - Infrastructure layer translates them to EF Core queries
   - This separation allows switching ORMs without changing domain

---

## 📦 Files Created

| File | Purpose |
|------|---------|
| `ISpecification.cs` | Specification contract |
| `BaseSpecification.cs` | Base implementation |
| `DishWithDetailsSpecification.cs` | Example specification |

## 📝 Files Modified

| File | Changes |
|------|---------|
| `IRepository.cs` | Added specification methods |
| `IUnityOfWork.cs` | Changed to `IRepository<T>` |
| `Repository.cs` | Implemented specification support |
| `UnityOfWork.cs` | Updated repository types |

## 🗑️ Files Removed

- `IRepositoryWithIncludes.cs` - Replaced by Specification Pattern

---

## 🎓 Best Practices

1. **Name Specifications Descriptively**
   ```csharp
   ActiveDishesByRestaurantSpec
   ExpiredPromotionsSpec
   CustomerOrderHistoryWithDetailsSpec
   ```

2. **Keep Specifications in Domain Layer**
   ```
   Domain/Specifications/{EntityName}Specifications/
   ```

3. **One Specification = One Query Intent**
   - Don't create generic "get everything" specs
   - Each spec should represent a specific business query

4. **Use Constructor Parameters for Filtering**
   ```csharp
   public class DishesInPriceRangeSpec : BaseSpecification<Dish>
   {
       public DishesInPriceRangeSpec(decimal minPrice, decimal maxPrice)
           : base(d => d.Price >= minPrice && d.Price <= maxPrice)
       { }
   }
   ```

---

## ✅ Verification Checklist

- [x] `IRepositoryWithIncludes` removed from Domain
- [x] `IRepository` uses `ISpecification<T>`
- [x] `IUnityOfWork` uses `IRepository<T>`
- [x] Infrastructure implements specification translation
- [x] Example specification created
- [x] No EF Core dependencies in Domain layer

---

## 🚀 Next Steps

1. **Update existing services** to use specifications instead of direct `GetAllAsync(includes)`
2. **Create specifications** for common query patterns
3. **Write unit tests** for specifications (they're just POCOs!)
4. **Consider adding** domain-specific repository interfaces for aggregates

---

**Result: ✅ Clean Architecture Compliant Repository Layer**

The Domain layer is now **infrastructure-agnostic**, **testable**, and follows **SOLID principles**.
