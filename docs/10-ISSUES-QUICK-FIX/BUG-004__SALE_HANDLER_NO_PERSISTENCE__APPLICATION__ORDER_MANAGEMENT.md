# BUG-004 — `SaleRecordedHandler` Never Persisted `SaleRecord` Entities

**Tracker ID**: ORD-003b  
**Layer**: SmartMenuOptim.Application  
**Feature**: Order Management / Sale Records — `SaleRecordedHandler`  
**Severity**: Critical  
**Status**: ✅ Fixed  
**Date Found**: 2026-03-21  
**Date Fixed**: 2026-03-21  
**Branch**: `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`

---

## Summary

Even after fixing BUG-003 (events now raised), `SaleRecord` entities were still not being created in the database. The `SaleRecordedHandler` only logged analytics and invalidated cache — it had **no persistence logic** and **no `IUnityOfWork` dependency**.

---

## Root Cause

The handler was implemented as an analytics-only handler. It lacked the core responsibility of creating `SaleRecord` entities from the event data.

### Code Before Fix

```csharp
public class SaleRecordedHandler : ResilientEventHandlerBase<SaleRecordedEvent>
{
    private readonly ICacheService _cacheService;        // Only cache
    private readonly ILogger<SaleRecordedHandler> _logger;
    // ❌ No IUnityOfWork — no way to persist anything

    public SaleRecordedHandler(
        ICacheService cacheService,
        ILogger<SaleRecordedHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(SaleRecordedEvent notification, ...)
    {
        LogSaleAnalytics(notification);                         // ← Log only
        await _cacheService.InvalidateAnalyticsCacheAsync(...); // ← Cache only
        // ❌ No SaleRecord creation, no AddAsync, no SaveChangesAsync
    }
}
```

### Impact

- `SaleRecords` table remained empty regardless of order completions
- AI-powered menu optimization had no data to analyze
- Revenue analytics showed $0 for all dishes
- Dish performance metrics completely absent

---

## Fix Applied

**File**: `SmartMenuOptim.Application/Handlers/SaleEventHandlers/SaleRecordedHandler.cs`

1. Injected `IUnityOfWork` into the handler constructor
2. Added `PersistSaleRecordAsync()` method that creates a `SaleRecord` entity using the domain constructor + `Money` value object
3. Persistence happens first (before analytics logging and cache invalidation)

### Code After Fix

```csharp
public class SaleRecordedHandler : ResilientEventHandlerBase<SaleRecordedEvent>
{
    private readonly IUnityOfWork _unitOfWork;           // ← NEW
    private readonly ICacheService _cacheService;
    private readonly ILogger<SaleRecordedHandler> _logger;

    public SaleRecordedHandler(
        IUnityOfWork unitOfWork,                         // ← NEW
        ICacheService cacheService,
        ILogger<SaleRecordedHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue) { ... }

    protected override async Task ProcessEventAsync(SaleRecordedEvent notification, ...)
    {
        await PersistSaleRecordAsync(notification, cancellationToken); // ← NEW (primary)
        LogSaleAnalytics(notification);                                // secondary
        await _cacheService.InvalidateAnalyticsCacheAsync(...);        // secondary
    }

    private async Task PersistSaleRecordAsync(SaleRecordedEvent notification, ...)
    {
        var saleAmount = new Money(notification.TotalAmount, notification.CurrencyCode);

        var saleRecord = new SaleRecord(
            restaurantId: notification.RestaurantId,
            dishId: notification.DishId,
            saleAmount: saleAmount,
            quantitySold: notification.QuantitySold);

        await _unitOfWork.SaleRecords.AddAsync(saleRecord);
        await _unitOfWork.SaveChangesAsync();   // ← This triggers BUG-005 before that fix
    }
}
```

---

## Files Modified

| File | Change |
|------|--------|
| `Application/Handlers/SaleEventHandlers/SaleRecordedHandler.cs` | Added `IUnityOfWork` injection; added `PersistSaleRecordAsync()` method; reordered `ProcessEventAsync` to persist first |

---

## Verification

- Completing an order creates one `SaleRecord` per `OrderItem` in the database
- `SaleRecord.SaleAmount` matches `OrderItem.Subtotal` (quantity × unit price)
- `SaleRecord.DishId` and `SaleRecord.RestaurantId` match the order data
- Analytics logging and cache invalidation still execute after persistence

---

## Related Issues

- [BUG-003](./BUG-003__UPDATE_STATUS_BYPASSED_DOMAIN_METHODS__APPLICATION__ORDER_MANAGEMENT.md) — No events raised (upstream)
- [BUG-005](./BUG-005__NESTED_TRANSACTION_CRASH__INFRASTRUCTURE__ORDER_MANAGEMENT.md) — Transaction crash (downstream — triggered by this fix)
