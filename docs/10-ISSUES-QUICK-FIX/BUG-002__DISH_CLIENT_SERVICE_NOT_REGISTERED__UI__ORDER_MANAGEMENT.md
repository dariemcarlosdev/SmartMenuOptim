# BUG-002 — `IDishClientService` Not Registered in Server DI

**Tracker ID**: ORD-002  
**Layer**: SmartMenuOptim.Server (UI)  
**Feature**: Order Management — `OrderForm` component  
**Severity**: High  
**Status**: ✅ Fixed  
**Date Found**: 2026-03-21  
**Date Fixed**: 2026-03-21  
**Branch**: `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`

---

## Summary

The `OrderForm.razor` component threw an `InvalidOperationException` at runtime because `IDishClientService` was not registered in the dependency injection container. The component needed this service to load the dish dropdown filtered by the selected restaurant.

---

## Root Cause

The `OrderForm.razor.cs` code-behind injected `IDishClientService` via `[Inject]`, but the corresponding `AddScoped<IDishClientService, DishClientService>()` registration was never added to `Server/Extensions/ServiceCollectionExtensions.cs`.

This was a simple omission during Phase 6 (Blazor UI) — the `IOrderClientService` was registered, but the dish service (added later for the restaurant-filtered dish dropdown in Phase 6.5 UX Polish) was missed.

---

## Fix Applied

**File**: `SmartMenuOptim.Server/Extensions/ServiceCollectionExtensions.cs`

Added the missing DI registration:

```csharp
services.AddScoped<IDishClientService, DishClientService>();
```

---

## Files Modified

| File | Change |
|------|--------|
| `Server/Extensions/ServiceCollectionExtensions.cs` | Added `AddScoped<IDishClientService, DishClientService>()` |

---

## Verification

- `OrderForm` loads without DI exceptions
- Dish dropdown populates when a restaurant is selected
- Dishes reload correctly when restaurant selection changes

---

## Related Issues

- [BUG-001](./BUG-001__CREATEDAT_ACTION_ROUTE_MISMATCH__API__ORDER_MANAGEMENT.md) — Route mismatch (same feature)
