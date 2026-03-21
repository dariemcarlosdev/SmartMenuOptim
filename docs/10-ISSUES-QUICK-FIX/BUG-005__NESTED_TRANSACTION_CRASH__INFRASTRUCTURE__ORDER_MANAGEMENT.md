# BUG-005 — Nested Transaction Crash in `UnityOfWork.SaveChangesAsync()`

**Tracker ID**: ORD-003c  
**Layer**: SmartMenuOptim.Infrastructure  
**Feature**: Order Management / Domain Event Dispatch — `UnityOfWork`  
**Severity**: Critical  
**Status**: ✅ Fixed  
**Date Found**: 2026-03-21  
**Date Fixed**: 2026-03-21  
**Branch**: `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`

---

## Summary

After fixing BUG-003 and BUG-004, completing an order caused the `SaleRecordedHandler` to crash with `InvalidOperationException: The connection is already in a transaction and cannot participate in another transaction`. The handler failed after 3 retry attempts and was sent to the dead letter queue.

---

## Error Message

```
fail: SmartMenuOptim.Application.Handlers.SaleEventHandlers.SaleRecordedHandler[0]
      SaleRecordedHandler failed after 3 attempts for EventId=4c8561d8-...
      System.InvalidOperationException: The connection is already in a transaction
      and cannot participate in another transaction.
         at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.EnsureNoTransactions()
         at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.BeginTransactionAsync(...)
         at SmartMenuOptim.Infrastructure.Persistence.Repositories.UnityOfWork.SaveChangesAsync()
         at SmartMenuOptim.Application.Handlers.SaleEventHandlers.SaleRecordedHandler.PersistSaleRecordAsync(...)
```

---

## Root Cause

`UnityOfWork.SaveChangesAsync()` **unconditionally** called `BeginTransactionAsync()`. Domain event handlers are dispatched by `AppDbContext.SaveChangesAsync()` **inside** the existing transaction from the outer `UoW.SaveChangesAsync()` call. When `SaleRecordedHandler` called `_unitOfWork.SaveChangesAsync()`, it attempted to start a second transaction on the same connection.

### Call Chain Showing the Crash

```
OrderService.UpdateStatusAsync()
  → UoW.SaveChangesAsync()                          ← BeginTransactionAsync() — TXN #1
    → AppDbContext.SaveChangesAsync()
      → base.SaveChangesAsync()                      ← Order persisted (not committed yet)
      → _domainEventDispatcher.DispatchEventsAsync() ← Still inside TXN #1
        → SaleRecordedHandler.PersistSaleRecordAsync()
          → _unitOfWork.SaleRecords.AddAsync(...)    ← Entity added to change tracker
          → UoW.SaveChangesAsync()                   ← BeginTransactionAsync() — 💥 CRASH
    → transaction.CommitAsync()                      ← Never reached
```

### Code Before Fix

```csharp
public async Task<int> SaveChangesAsync()
{
    // Always starts a new transaction — no check for existing one
    using (var transaction = await _context.Database.BeginTransactionAsync())
    {
        try
        {
            var result = await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

---

## Fix Applied

**File**: `SmartMenuOptim.Infrastructure/Persistence/Repositories/UnityOfWork.cs`

Added a `CurrentTransaction` check before `BeginTransactionAsync()`. If a transaction is already active (e.g., called from within a domain event handler), the method participates in the existing transaction instead of starting a new one.

### Code After Fix

```csharp
public async Task<int> SaveChangesAsync()
{
    // If already inside a transaction (e.g., domain event handler),
    // participate in it instead of starting a new one.
    if (_context.Database.CurrentTransaction != null)
    {
        return await _context.SaveChangesAsync();
    }

    // No active transaction — wrap in explicit transaction for atomicity.
    using (var transaction = await _context.Database.BeginTransactionAsync())
    {
        try
        {
            var result = await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

### Call Chain After Fix

```
OrderService.UpdateStatusAsync()
  → UoW.SaveChangesAsync()                          ← BeginTransactionAsync() — TXN #1
    → AppDbContext.SaveChangesAsync()
      → base.SaveChangesAsync()                      ← Order persisted
      → _domainEventDispatcher.DispatchEventsAsync()
        → SaleRecordedHandler.PersistSaleRecordAsync()
          → _unitOfWork.SaleRecords.AddAsync(...)
          → UoW.SaveChangesAsync()
            → CurrentTransaction != null             ← ✅ Detected active TXN
            → _context.SaveChangesAsync()            ← Participates in TXN #1
    → transaction.CommitAsync()                      ← ✅ Both Order + SaleRecord committed
```

---

## Files Modified

| File | Change |
|------|--------|
| `Infrastructure/Persistence/Repositories/UnityOfWork.cs` | Added `_context.Database.CurrentTransaction != null` check before `BeginTransactionAsync()`; XML doc updated to document transaction-aware behavior |

---

## Verification

- Completing an order persists both `Order` status change and `SaleRecord` entities atomically
- If `SaleRecordedHandler` throws, the entire transaction rolls back (order status + sale records)
- Normal service calls (outside event handlers) still create explicit transactions as before
- No more `InvalidOperationException` in handler logs
- No more DLQ entries for `SaleRecordedEvent`

---

## Architectural Impact

This fix is **infrastructure-wide** — it affects all future domain event handlers that persist entities via `_unitOfWork.SaveChangesAsync()`. Any handler dispatched during the `AppDbContext.SaveChangesAsync()` event dispatch cycle will now correctly participate in the existing transaction.

---

## Related Issues

- [BUG-003](./BUG-003__UPDATE_STATUS_BYPASSED_DOMAIN_METHODS__APPLICATION__ORDER_MANAGEMENT.md) — No events raised (upstream)
- [BUG-004](./BUG-004__SALE_HANDLER_NO_PERSISTENCE__APPLICATION__ORDER_MANAGEMENT.md) — Handler doesn't persist (upstream — this bug surfaced after BUG-004 was fixed)
