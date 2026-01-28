# Repository Pattern Design Documentation

## Overview
This document explains the design decisions, patterns, and best practices used in the SmartMenuOptim repository implementation.

---

## Pattern Architecture

### Interface Hierarchy
```
IRepository<T>
    ↓ inherits
IRepositoryWithIncludes<T>
```

**Why this design?**
- **Interface Segregation Principle (ISP)**: Clients that don't need includes don't have to know about them
- **Progressive Enhancement**: Start simple, add complexity only when needed
- **Type Safety**: Expression-based includes provide compile-time checking

---

## Method Signature Ambiguity - RESOLVED

### The Issue
C# doesn't allow satisfying an interface method `GetByIdAsync(int id)` with a method that has `GetByIdAsync(int id, params Expression[])`, even though `params` makes the parameter optional.

### The Solution
**Explicit Interface Implementation + Public Method**

```csharp
// Explicit implementation (for IRepository<T>)
async Task<T?> IRepository<T>.GetByIdAsync(int id)
{
    return await GetByIdAsync(id); // Delegates to public method
}

// Public method (for IRepositoryWithIncludes<T>)
public async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
{
    if (includes == null || includes.Length == 0)
    {
        return await _dbSet.FindAsync(id); // Fast path
    }
    // ... includes logic
}
```

### Why This Works

1. **Satisfies Both Interfaces**
   - `IRepository<T>.GetByIdAsync(int)` → explicit implementation
   - `IRepositoryWithIncludes<T>.GetByIdAsync(int, params)` → public implementation

2. **Performance Optimization**
   - No includes = Uses `FindAsync` (fastest, uses primary key cache)
   - With includes = Uses `FirstOrDefaultAsync` with eager loading

3. **Unified Behavior**
   - Both paths use the same public method internally
   - Consistent behavior whether cast to `IRepository<T>` or `IRepositoryWithIncludes<T>`

4. **Developer Experience**
   - When using `IRepositoryWithIncludes<T>` (most common), you see the flexible method
   - IntelliSense shows: `GetByIdAsync(int id, params Expression[])`
   - Can call with: `GetByIdAsync(5)` or `GetByIdAsync(5, x => x.Category, x => x.Reviews)`

---

## Unit of Work Pattern - RESOLVED

### Critical Rules

❌ **NEVER call `SaveChanges()` in Repository methods**
```csharp
// WRONG!
public void Update(T entity)
{
    _context.Entry(entity).State = EntityState.Modified;
    _context.SaveChanges(); // ❌ Breaks UoW pattern
}
```

✅ **Let UnitOfWork manage all transactions**
```csharp
// CORRECT!
public void Update(T entity)
{
    _context.Entry(entity).State = EntityState.Modified;
    // No SaveChanges here!
}

// In your service layer:
_unitOfWork.Dishes.Update(dish);
_unitOfWork.Reviews.Add(review);
await _unitOfWork.SaveChangesAsync(); // ✅ Single transaction
```

### Why?
- **Transaction Control**: Multiple operations can be grouped
- **Rollback Safety**: All-or-nothing approach maintains data integrity
- **Performance**: Reduces database round trips

---

## AsNoTracking Optimization - RESOLVED

### The Issue
Calling `AsNoTracking()` after each `Include()` is inefficient:
```csharp
// WRONG!
foreach (var include in includes)
{
    query = query.Include(include).AsNoTracking(); // ❌ Multiple calls
}
```

### The Solution
Apply `AsNoTracking()` once after all includes:
```csharp
// CORRECT!
foreach (var include in includes)
{
    query = query.Include(include);
}
query = query.AsNoTracking(); // ✅ Single call
```

### Performance Impact
- **Fewer intermediate objects**: EF Core creates fewer query nodes
- **Query optimization**: Better execution plan generation
- **Memory efficiency**: Less object allocations

---

## Usage Examples

### Basic CRUD Operations
```csharp
// Get by ID (uses FindAsync internally)
var dish = await _unitOfWork.Dishes.GetByIdAsync(5);

// Get with includes (uses FirstOrDefaultAsync + AsNoTracking)
var dishWithCategory = await _unitOfWork.Dishes.GetByIdAsync(5, d => d.Category);

// Get with multiple includes
var dishWithAll = await _unitOfWork.Dishes.GetByIdAsync(5, 
    d => d.Category, 
    d => d.Reviews);

// Get all
var allDishes = await _unitOfWork.Dishes.GetAllAsync();

// Get all with includes
var allDishesWithCategories = await _unitOfWork.Dishes.GetAllAsync(d => d.Category);
```

### Complex Queries
```csharp
// Using Query() for custom LINQ
var expensiveDishes = await _unitOfWork.Dishes
    .Query()
    .Where(d => d.Price > 20)
    .Include(d => d.Category)
    .OrderByDescending(d => d.Price)
    .ToListAsync();
```

### Transaction Management
```csharp
public async Task<bool> TransferMenuItemAsync(int dishId, int newCategoryId)
{
    try
    {
        var dish = await _unitOfWork.Dishes.GetByIdAsync(dishId);
        if (dish == null) return false;

        dish.CategoryId = newCategoryId;
        _unitOfWork.Dishes.Update(dish);

        // Log the change
        await _unitOfWork.BussinessRules.AddAsync(new BusinessRule 
        { 
            /* ... */ 
        });

        // All or nothing - atomic transaction
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    catch
    {
        // Transaction automatically rolled back
        throw;
    }
}
```

---

## Best Practices

### ✅ DO
- Use `GetByIdAsync(id)` for simple lookups
- Use `GetByIdAsync(id, includes)` for eager loading
- Call `SaveChangesAsync()` only in service/business logic layer
- Use `Query()` for complex custom queries
- Dispose `UnityOfWork` properly (use `using` statements or DI scopes)

### ❌ DON'T
- Don't call `SaveChanges()` in repository methods
- Don't call `AsNoTracking()` multiple times
- Don't mix tracked and non-tracked entities in the same operation
- Don't forget to await async operations
- Don't query without includes if you need navigation properties (causes N+1 queries)

---

## Future Improvements

1. **Specification Pattern**: For complex query logic reuse
2. **Caching Layer**: For frequently accessed, read-only data
3. **Bulk Operations**: For high-volume inserts/updates
4. **Soft Delete**: Mark records as deleted instead of removing
5. **Audit Logging**: Automatic tracking of changes

---

## Related Files
- `IRepository.cs` - Base repository interface
- `IRepositoryWithIncludes.cs` - Extended interface with includes support
- `Repository.cs` - Generic repository implementation
- `IUnityOfWork.cs` - Unit of Work interface
- `UnityOfWork.cs` - Unit of Work implementation

---

**Last Updated**: 2024
**Pattern Version**: 2.0 (Method Ambiguity Resolved)
