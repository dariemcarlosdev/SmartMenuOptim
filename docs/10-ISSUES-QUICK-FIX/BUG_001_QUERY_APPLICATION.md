# Issue#: 001_QUERY_APPLICATION

| Field | Value |
|-------|-------|
| **Date** | 2026-03-15 |
| **Severity** | 🟠 High |
| **Status** | ✅ Resolved |

**Description**: Order detail page for Order #2 displayed zero items, despite the `OrderItems` database table containing two records with `OrderId = 2`. All other order data (status, customer, total) loaded correctly — only the `OrderItems` child collection was empty.

**Root Cause**: EF Core global query filters propagate to `.Include()` child collections. `AppDbContext` registers `HasQueryFilter(e => !e.IsDeleted)` on the `OrderItem` entity (line 766). When `OrderService.GetByIdAsync` executed `.Include(o => o.OrderItems)`, EF Core applied the `IsDeleted` filter to **both** the parent `Order` and the child `OrderItem` rows independently. This caused child items with `IsDeleted = true` to be silently excluded, even though the parent Order was not deleted.

**Resolution**: Added `.IgnoreQueryFilters()` to all `OrderService` query methods. The service already applies `!o.IsDeleted` explicitly in every `Where` clause on the parent Order entity, making the global filter redundant for Order queries and harmful for child entity includes.

**References**:

| File | Change Reason |
|------|---------------|
| `SmartMenuOptim.Application/Features/Orders/Services/OrderService.cs` | Added `.IgnoreQueryFilters()` to 7 query methods to prevent global filters from silently excluding child `OrderItem` records loaded via `.Include()` |
| `SmartMenuOptim.Infrastructure/Persistence/Context/AppDbContext.cs` | Root cause location — `OrderItem` global query filter at line 766 (no change made; filter kept for other consumers) |