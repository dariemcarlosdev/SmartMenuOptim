# Issue#: 003_UI_UI

| Field | Value |
|-------|-------|
| **Date** | 2026-03-15 |
| **Severity** | 🟡 Medium |
| **Status** | ✅ Resolved |

**Description**: Restaurant cards on the Restaurant List page displayed "50 orders" in the stats row, which users confused with an actual order count. The Order List page only showed 3 orders for the same restaurant, creating a perceived data inconsistency.

**Root Cause**: The stats row rendered `@restaurant.MaxSimultaneousOrders orders` — a **capacity configuration** property (default value = 50) — with ambiguous labeling that read as an order count rather than a concurrency limit.

**Resolution**: Changed the label from `50 orders` to `Max 50 concurrent` to clearly communicate it is a capacity setting. Additionally, added a live order count indicator per restaurant card that links to the Order List filtered by that restaurant.

**References**:

| File | Change Reason |
|------|---------------|
| `SmartMenuOptim.Server/Features/Restaurants/Components/RestaurantList.razor` | Updated stats row label; added clickable order count with active-orders badge |
| `SmartMenuOptim.Server/Features/Restaurants/Components/RestaurantList.razor.cs` | Added `IOrderClientService` injection and `LoadOrderCountsAsync` to fetch live order counts per restaurant |