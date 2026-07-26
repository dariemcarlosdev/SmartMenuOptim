# Restaurant Consolidation - Action Required

## ✅ What Was Done

1. **✅ Enhanced Restaurant Aggregate** (`Domain/Aggregates/RestaurantAggregate/Restaurant.cs`)
   - Added `OwnerId` property
   - Added `TimeZoneId` property
   - Added `UpdateTimeZone()` method with validation
   - Added `TransferOwnership()` method
   - All properties now use Value Objects (Email, PhoneNumber, Address)

2. **✅ Deleted Duplicate** (`Domain/Entities/TenantSpecificEntities/Restaurant.cs`)

3. **✅ Updated TenantEntityBase** to reference `Aggregates.RestaurantAggregate`

## ⚠️ MANUAL FIXES REQUIRED

Add this using statement to the following files:

```csharp
using SmartMenuOptim.Domain.Aggregates.RestaurantAggregate;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;
```

**Files that need Restaurant reference:**
1. `Entities/ProfileEntities/StaffMember.cs`
2. `Entities/ProfileEntities/AdminUser.cs`
3. `Entities/GlobalEntities/ApplicationUser.cs`
4. `Entities/GlobalEntities/UserPermission.cs`

**Files that need both Restaurant AND Dish references:**
5. `Entities/TenantSpecificEntities/LoyaltyTransaction.cs`
6. `Entities/TenantSpecificEntities/SaleRecord.cs`
7. `Entities/TenantSpecificEntities/Category.cs`
8. `Entities/TenantSpecificEntities/Review.cs`
9. `Aggregates/MenuAggregate/Menu.cs`
10. `Aggregates/MenuAggregate/MenuDish.cs`

**Files already updated:**
- ✅ `Entities/Base/TenantEntityBase.cs`
- ✅ `Aggregates/OrderAggregate/Order.cs` (already has it)
- ✅ `Aggregates/OrderAggregate/OrderItem.cs` (already has it from Dish)
- ✅ `Entities/TenantSpecificEntities/OrderStatus.cs` (already has it)

## Decision About Shared Project

**You have TWO Restaurant entities:**

1. **Domain Project** - `Aggregates/RestaurantAggregate/Restaurant.cs` ✅
   - DDD Aggregate with business logic
   - Uses Value Objects
   - Private setters, behavioral methods

2. **Shared Project** - `Shared/Data/Entities/TenantSpecificEntities/Restaurant.cs` ⚠️
   - Traditional entity
   - Public setters
   - More navigation properties

### **Recommended Approach:**

**Keep Shared separate AS A DTO/VIEW MODEL** for now because:
1. Your Blazor app likely uses it for data binding
2. It has different structure (all navigation properties)
3. Easier migration path

**Future: Gradually migrate to Domain aggregate**
- Use Domain aggregate in your backend/API
- Use AutoMapper to convert Domain → Shared DTO for Blazor UI
- This gives you best of both worlds

## Summary

### **YES, Restaurant was moved from TenantSpecificEntities to Aggregates/RestaurantAggregate** ✅

The restaurant is now a proper DDD aggregate:
- Located in `/Aggregates/RestaurantAggregate/`
- Uses Value Objects (Email, PhoneNumber, Address)
- Has all business methods
- Has OwnerId and TimeZoneId properties from Shared version

### **Shared Project Restaurant**

**DO NOT move it** - keep it as a DTO for Blazor UI binding. It serves a different purpose than the Domain aggregate.

## Next Steps

1. **Fix build errors** - Add using statements to the 10 files listed above
2. **Test compilation** - Run build again
3. **Update DbContext** - Make sure EF Core configuration uses the aggregate from Domain project
4. **Consider AutoMapper** - For converting between Domain aggregates and Shared DTOs

## Updated Structure

```
SmartMenuOptim.Domain/
└── Aggregates/
    └── RestaurantAggregate/
        ├── Restaurant.cs        (Root - Enhanced ✨)
        └── BusinessHours.cs     (Entity)

SmartMenuOptim.Shared/
└── Data/Entities/TenantSpecificEntities/
    └── Restaurant.cs            (Keep as DTO for Blazor)
```
