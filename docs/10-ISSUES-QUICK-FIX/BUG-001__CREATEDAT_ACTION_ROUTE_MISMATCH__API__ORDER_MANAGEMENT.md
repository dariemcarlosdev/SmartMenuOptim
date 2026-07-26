# BUG-001 — `CreatedAtAction` Route Mismatch Due to `SuppressAsyncSuffixInActionNames`

**Tracker ID**: ORD-001  
**Layer**: SmartMenuOptim.API  
**Feature**: Order Management — `OrdersController.CreateAsync`  
**Severity**: High  
**Status**: ✅ Fixed  
**Date Found**: 2026-03-21  
**Date Fixed**: 2026-03-21  
**Branch**: `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`

---

## Summary

Creating a new order via `POST /api/v1/orders` threw `InvalidOperationException: No route matches the supplied values`. The `201 Created` response with `Location` header could not be generated because `CreatedAtAction` could not resolve the target action.

---

## Root Cause

ASP.NET Core's default `SuppressAsyncSuffixInActionNames = true` strips the "Async" suffix when generating route names. The controller used `CreatedAtAction(nameof(GetByIdAsync), ...)`, but the route system registered the action as `"GetById"` (without "Async"). The `nameof()` operator returns the full C# method name `"GetByIdAsync"`, creating a mismatch.

### Affected Controllers

The same bug existed in three controllers:
- `OrdersController` (discovered first)
- `RestaurantsController`
- `DishesController`

---

## Fix Applied

**File**: `SmartMenuOptim.API/Extensions/ServiceCollectionExtensions.cs`

Set `SuppressAsyncSuffixInActionNames = false` globally in `AddControllers()` options:

```csharp
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});
```

This ensures `nameof(GetByIdAsync)` matches the route-registered action name.

---

## Files Modified

| File | Change |
|------|--------|
| `API/Extensions/ServiceCollectionExtensions.cs` | Added `SuppressAsyncSuffixInActionNames = false` to `AddControllers` options |

---

## Verification

- `POST /api/v1/orders` returns `201 Created` with correct `Location` header
- Same fix resolves identical issue in `RestaurantsController` and `DishesController`

---

## Related Issues

- [BUG-002](./BUG-002__DISH_CLIENT_SERVICE_NOT_REGISTERED__UI__ORDER_MANAGEMENT.md) — DI registration (same feature)
