# Before vs After: Repository Pattern Refactoring

## 🔄 Side-by-Side Comparison

### **Scenario 1: Get All Dishes with Category and Restaurant**

#### ❌ BEFORE (Infrastructure Leakage)
```csharp
// Application/Services/DishService.cs
public class DishService
{
    private readonly IUnityOfWork _unitOfWork;

    public async Task<IEnumerable<DishDto>> GetAllDishesAsync()
    {
        // ❌ EF Core-specific Include pattern exposed to application layer
        var dishes = await _unitOfWork.Dishes.GetAllAsync(
            d => d.Category,        // EF Core Expression
            d => d.Restaurant       // EF Core Expression
        );
        
        return _mapper.Map<IEnumerable<DishDto>>(dishes);
    }
}
```

**Problems:**
- Application layer knows about navigation properties (EF Core concept)
- Can't unit test without EF Core InMemory DB
- Breaks if we switch to Dapper, MongoDB, etc.

---

#### ✅ AFTER (Clean Architecture)
```csharp
// Domain/Specifications/DishSpecifications/AllDishesWithDetailsSpec.cs
public class AllDishesWithDetailsSpec : BaseSpecification<Dish>
{
    public AllDishesWithDetailsSpec()
    {
        AddInclude(d => d.Category);
        AddInclude(d => d.Restaurant);
        ApplyOrderBy(d => d.Name);
    }
}

// Application/Services/DishService.cs
public class DishService
{
    private readonly IUnityOfWork _unitOfWork;

    public async Task<IEnumerable<DishDto>> GetAllDishesAsync()
    {
        // ✅ Domain-centric query
        var spec = new AllDishesWithDetailsSpec();
        var dishes = await _unitOfWork.Dishes.FindAsync(spec);
        
        return _mapper.Map<IEnumerable<DishDto>>(dishes);
    }
}
```

**Benefits:**
- Query logic in Domain layer (where it belongs)
- Easy to unit test (mock `FindAsync`)
- ORM-agnostic
- Reusable specification

---

### **Scenario 2: Get Active Dishes by Restaurant**

#### ❌ BEFORE
```csharp
public async Task<IEnumerable<DishDto>> GetActiveDishesByRestaurantAsync(int restaurantId)
{
    // ❌ Query logic scattered in service layer
    var allDishes = await _unitOfWork.Dishes.GetAllAsync(
        d => d.Category,
        d => d.Restaurant
    );
    
    // ❌ Filtering in memory (inefficient!)
    var activeDishes = allDishes
        .Where(d => d.RestaurantId == restaurantId && d.IsAvailable)
        .OrderBy(d => d.Name);
    
    return _mapper.Map<IEnumerable<DishDto>>(activeDishes);
}
```

**Problems:**
- Loads ALL dishes from database
- Filters in memory (performance issue)
- Query logic in application layer
- Not reusable

---

#### ✅ AFTER
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

// Application/Services/DishService.cs
public async Task<IEnumerable<DishDto>> GetActiveDishesByRestaurantAsync(int restaurantId)
{
    // ✅ Efficient, domain-centric query
    var spec = new ActiveDishesByRestaurantSpec(restaurantId);
    var dishes = await _unitOfWork.Dishes.FindAsync(spec);
    
    return _mapper.Map<IEnumerable<DishDto>>(dishes);
}
```

**Benefits:**
- Filters in database (efficient SQL WHERE clause)
- Specification is reusable
- Testable without database
- Clear business intent

---

### **Scenario 3: Paginated Dishes with Search**

#### ❌ BEFORE
```csharp
public async Task<PagedResult<DishDto>> SearchDishesAsync(
    string searchTerm, 
    int pageIndex, 
    int pageSize)
{
    // ❌ Manual query building with IQueryable
    var query = _unitOfWork.Dishes.Query()
        .Include(d => d.Category)
        .Include(d => d.Restaurant)
        .Where(d => d.Name.Contains(searchTerm) || d.Description.Contains(searchTerm))
        .OrderBy(d => d.Name);
    
    var totalCount = await query.CountAsync();
    
    var items = await query
        .Skip(pageIndex * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return new PagedResult<DishDto>
    {
        Items = _mapper.Map<IEnumerable<DishDto>>(items),
        TotalCount = totalCount,
        PageIndex = pageIndex,
        PageSize = pageSize
    };
}
```

**Problems:**
- EF Core methods (`Include`, `CountAsync`, `ToListAsync`) in application layer
- Can't unit test without EF Core
- Query logic not reusable

---

#### ✅ AFTER
```csharp
// Domain/Specifications/DishSpecifications/DishSearchSpec.cs
public class DishSearchSpec : BaseSpecification<Dish>
{
    public DishSearchSpec(string searchTerm, int pageIndex, int pageSize)
        : base(d => d.Name.Contains(searchTerm) || d.Description.Contains(searchTerm))
    {
        AddInclude(d => d.Category);
        AddInclude(d => d.Restaurant);
        ApplyOrderBy(d => d.Name);
        ApplyPaging(pageIndex * pageSize, pageSize);
    }
}

public class DishSearchCountSpec : BaseSpecification<Dish>
{
    public DishSearchCountSpec(string searchTerm)
        : base(d => d.Name.Contains(searchTerm) || d.Description.Contains(searchTerm))
    {
    }
}

// Application/Services/DishService.cs
public async Task<PagedResult<DishDto>> SearchDishesAsync(
    string searchTerm, 
    int pageIndex, 
    int pageSize)
{
    // ✅ Clean, declarative queries
    var countSpec = new DishSearchCountSpec(searchTerm);
    var totalCount = await _unitOfWork.Dishes.CountAsync(countSpec);
    
    var spec = new DishSearchSpec(searchTerm, pageIndex, pageSize);
    var items = await _unitOfWork.Dishes.FindAsync(spec);
    
    return new PagedResult<DishDto>
    {
        Items = _mapper.Map<IEnumerable<DishDto>>(items),
        TotalCount = totalCount,
        PageIndex = pageIndex,
        PageSize = pageSize
    };
}
```

**Benefits:**
- Specifications are testable POCOs
- Reusable across different services
- Easy to mock in unit tests
- Clear separation of concerns

---

### **Scenario 4: Unit Testing**

#### ❌ BEFORE (Difficult)
```csharp
[Fact]
public async Task GetAllDishes_ReturnsAllDishes()
{
    // ❌ Complex setup - need EF Core InMemory or Moq gymnastics
    var mockRepo = new Mock<IRepositoryWithIncludes<Dish>>();
    mockRepo
        .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Dish, object>>[]>()))
        .ReturnsAsync(new List<Dish> 
        { 
            new Dish { Id = 1, Name = "Test" } 
        });
    
    var mockUoW = new Mock<IUnityOfWork>();
    mockUoW.Setup(u => u.Dishes).Returns(mockRepo.Object);
    
    var service = new DishService(mockUoW.Object, _mapper);
    
    // Act
    var result = await service.GetAllDishesAsync();
    
    // Assert
    Assert.NotEmpty(result);
}
```

---

#### ✅ AFTER (Simple)
```csharp
[Fact]
public async Task GetAllDishes_ReturnsAllDishes()
{
    // ✅ Simple setup - just mock the interface
    var mockRepo = new Mock<IRepository<Dish>>();
    mockRepo
        .Setup(r => r.FindAsync(It.IsAny<ISpecification<Dish>>()))
        .ReturnsAsync(new List<Dish> 
        { 
            new Dish { Id = 1, Name = "Test" } 
        });
    
    var mockUoW = new Mock<IUnityOfWork>();
    mockUoW.Setup(u => u.Dishes).Returns(mockRepo.Object);
    
    var service = new DishService(mockUoW.Object, _mapper);
    
    // Act
    var result = await service.GetAllDishesAsync();
    
    // Assert
    Assert.NotEmpty(result);
    
    // ✅ Can even verify the correct specification was used
    mockRepo.Verify(r => 
        r.FindAsync(It.IsAny<AllDishesWithDetailsSpec>()), 
        Times.Once);
}

[Fact]
public void ActiveDishesByRestaurantSpec_FiltersCorrectly()
{
    // ✅ Test specification logic in isolation (no database!)
    var spec = new ActiveDishesByRestaurantSpec(restaurantId: 1);
    
    var dishes = new List<Dish>
    {
        new Dish { Id = 1, RestaurantId = 1, IsAvailable = true },
        new Dish { Id = 2, RestaurantId = 1, IsAvailable = false },
        new Dish { Id = 3, RestaurantId = 2, IsAvailable = true },
    };
    
    var filtered = dishes.AsQueryable()
        .Where(spec.Criteria)
        .ToList();
    
    Assert.Single(filtered);
    Assert.Equal(1, filtered[0].Id);
}
```

---

## 📊 Architecture Comparison

### **Before - Layered Architecture (Violated)**
```
┌─────────────────────────────────────┐
│  Application Layer (Services)       │
│  ❌ Knows about EF Core Include()   │
│  ❌ Builds queries with IQueryable  │
│  ❌ Uses Expression<Func<T,object>> │
└─────────────────────────────────────┘
              │ depends on
┌─────────────────────────────────────┐
│  Domain Layer                        │
│  ❌ IRepositoryWithIncludes<T>      │
│  ❌ Exposes EF Core patterns        │
└─────────────────────────────────────┘
              │ depends on
┌─────────────────────────────────────┐
│  Infrastructure Layer (EF Core)     │
│  Repository<T> implementation       │
└─────────────────────────────────────┘

❌ PROBLEM: Domain depends on Infrastructure!
```

### **After - Clean Architecture (Correct)**
```
┌─────────────────────────────────────┐
│  Application Layer (Services)       │
│  ✅ Uses domain specifications      │
│  ✅ No infrastructure knowledge     │
└─────────────────────────────────────┘
              │ depends on
┌─────────────────────────────────────┐
│  Domain Layer                        │
│  ✅ IRepository<T>                  │
│  ✅ ISpecification<T>               │
│  ✅ Pure business abstractions      │
└─────────────────────────────────────┘
              ↑ implements
┌─────────────────────────────────────┐
│  Infrastructure Layer (EF Core)     │
│  ✅ Repository<T> translates specs  │
│  ✅ ApplySpecification() method     │
└─────────────────────────────────────┘

✅ CORRECT: Infrastructure depends on Domain!
```

---

## 📈 Performance Comparison

### **Before - In-Memory Filtering**
```csharp
// ❌ Loads all 10,000 dishes from database
var allDishes = await _unitOfWork.Dishes.GetAllAsync(includes);

// ❌ Filters 10,000 records in memory
var filtered = allDishes
    .Where(d => d.RestaurantId == 1 && d.IsAvailable);

// SQL executed:
// SELECT * FROM Dishes 
// INNER JOIN Categories ON ...
// INNER JOIN Restaurants ON ...
// (returns 10,000 rows to .NET)
```

### **After - Database Filtering**
```csharp
// ✅ Filters in database, returns only 50 dishes
var spec = new ActiveDishesByRestaurantSpec(restaurantId: 1);
var dishes = await _unitOfWork.Dishes.FindAsync(spec);

// SQL executed:
// SELECT * FROM Dishes 
// INNER JOIN Categories ON ...
// INNER JOIN Restaurants ON ...
// WHERE RestaurantId = 1 AND IsAvailable = 1
// (returns only 50 matching rows)
```

**Performance Improvement:** ~99.5% reduction in data transfer!

---

## ✅ Clean Architecture Checklist

| Principle | Before | After |
|-----------|--------|-------|
| **Dependency Rule** | ❌ Domain → Infrastructure | ✅ Infrastructure → Domain |
| **Testability** | ❌ Requires EF Core | ✅ Pure unit tests |
| **Separation of Concerns** | ❌ Query logic scattered | ✅ Encapsulated in specs |
| **Open/Closed** | ❌ Hard to extend | ✅ Add new specs easily |
| **DRY** | ❌ Duplicated queries | ✅ Reusable specifications |
| **Single Responsibility** | ❌ Services build queries | ✅ Specs handle queries |

---

## 🎯 Conclusion

The refactoring successfully:
- ✅ Removed EF Core coupling from Domain layer
- ✅ Implemented Specification Pattern correctly
- ✅ Improved testability dramatically
- ✅ Followed Clean Architecture principles
- ✅ Maintained backward compatibility (Query() still available)
- ✅ Improved performance (database-level filtering)

**Result:** A truly infrastructure-agnostic, testable, and maintainable repository layer!
