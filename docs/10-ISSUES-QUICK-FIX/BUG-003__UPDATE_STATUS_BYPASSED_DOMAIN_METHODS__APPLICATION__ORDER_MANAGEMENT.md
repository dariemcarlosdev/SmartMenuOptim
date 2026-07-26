# BUG-003 — `UpdateStatusAsync` Bypassed Domain Methods for Terminal Statuses

**Tracker ID**: ORD-003a  
**Layer**: SmartMenuOptim.Application  
**Feature**: Order Management — `OrderService.UpdateStatusAsync`  
**Severity**: Critical  
**Status**: ✅ Fixed  
**Date Found**: 2026-03-21  
**Date Fixed**: 2026-03-21  
**Branch**: `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`

---

## Summary

Completing an order via the UI did not create any `SaleRecord` entities in the database. The root cause was that `OrderService.UpdateStatusAsync()` used a generic status setter for **all** status changes, including terminal states like "Completed" and "Cancelled". This bypassed the rich domain methods (`Order.Complete()`, `Order.Cancel()`) that raise domain events.

---

## Root Cause

`UpdateStatusAsync()` called `order.UpdateStatus(newStatusId)` — a simple property setter that only changes the `OrderStatusId` field. It raises **no domain events**. The domain methods `Order.Complete()` and `Order.Cancel()` encapsulate lifecycle behavior, raise `OrderCompletedEvent`, `SaleRecordedEvent`, and `OrderCancelledEvent`, but were never invoked.

### Code Before Fix

```csharp
public async Task<Result<OrderDTO>> UpdateStatusAsync(int id, int newStatusId, ...)
{
    var order = await _unitOfWork.Orders.Query()
        .Include(o => o.OrderItems)      // ← No Dish/Category includes
        .FirstOrDefaultAsync(...);

    order.UpdateStatus(newStatusId);      // ← Generic setter, NO events raised

    await _unitOfWork.SaveChangesAsync(); // ← No events dispatched
}
```

### Impact

- No `OrderCompletedEvent` raised → no completion metrics, no review scheduling
- No `SaleRecordedEvent` raised → no sale records created in database
- No `OrderCancelledEvent` raised → no loyalty point reversal, no cancellation notification
- The `Order.Complete()` method (with full event raising) existed but was dead code via this path

---

## Fix Applied

**File**: `SmartMenuOptim.Application/Features/Orders/Services/OrderService.cs`

1. Look up the target `OrderStatus` entity by ID to determine its `Name`
2. Call the appropriate domain method based on terminal status detection
3. Include `Dish` + `Category` navigation properties so `SaleRecordedEvent` has dish/category data

### Code After Fix

```csharp
public async Task<Result<OrderDTO>> UpdateStatusAsync(int id, int newStatusId, ...)
{
    // 1. Look up target status to detect terminal states
    var targetStatus = await _unitOfWork.OrderStatuses.Query()
        .FirstOrDefaultAsync(s => s.Id == newStatusId && !s.IsDeleted);

    if (targetStatus is null)
        return Result<OrderDTO>.Failure($"Order status with ID {newStatusId} not found.");

    // 2. Query with Dish + Category includes for sale event data
    var order = await _unitOfWork.Orders.Query()
        .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Dish)
                .ThenInclude(d => d.Category)
        .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

    // 3. Use domain methods for terminal statuses
    if (targetStatus.Name == "Completed")
        order.Complete(completedStatusId: newStatusId);
    else if (targetStatus.Name == "Cancelled")
        order.Cancel(cancelledStatusId: newStatusId, reason: "...", cancelledBy: CancellationSource.Staff);
    else
        order.UpdateStatus(newStatusId);

    await _unitOfWork.SaveChangesAsync();
}
```

---

## Files Modified

| File | Change |
|------|--------|
| `Application/Features/Orders/Services/OrderService.cs` | `UpdateStatusAsync` detects terminal status names → calls `Complete()`/`Cancel()` domain methods; added `OrderStatus` lookup + `Dish`/`Category` includes |

---

## Verification

- Setting order status to "Completed" raises `OrderCompletedEvent` + `SaleRecordedEvent` per item
- Setting order status to "Cancelled" raises `OrderCancelledEvent`
- Intermediate statuses (Pending → Confirmed → Preparing) still use generic `UpdateStatus()`
- `Dish.Name` and `Category.Name` populated in `SaleRecordedEvent` (not empty strings)

---

## DDD Pattern Lesson

> **Application services must detect the semantic intent of a status change and call the appropriate
> domain method.** Generic setters (`UpdateStatus`) are for intermediate transitions without business
> rules or side effects. Terminal transitions (`Complete`, `Cancel`) have domain events, invariant
> checks, and cross-aggregate effects — they must use the rich domain methods.

---

## Related Issues

- [BUG-004](./BUG-004__SALE_HANDLER_NO_PERSISTENCE__APPLICATION__ORDER_MANAGEMENT.md) — Handler doesn't persist (downstream)
- [BUG-005](./BUG-005__NESTED_TRANSACTION_CRASH__INFRASTRUCTURE__ORDER_MANAGEMENT.md) — Transaction crash (downstream)
