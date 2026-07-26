# Issue#: 004_DATA_UI

| Field | Value |
|-------|-------|
| **Date** | 2026-03-15 |
| **Severity** | 🟡 Medium |
| **Status** | ✅ Resolved |

**Description**: Order List page was hardcoded to always load orders for Restaurant ID 1 (`DemoRestaurantId = 1`). Users could not view orders for other restaurants. The Order Form also hardcoded `RestaurantId = 1` and `CustomerId = 1`.

**Root Cause**: Temporary demo constants were used during initial UI development and never replaced with dynamic restaurant selection.

**Resolution**: Replaced hardcoded restaurant IDs with a dynamic restaurant dropdown loaded from `IRestaurantClientService`. Order List supports `?restaurantId=` query parameter for deep-linking. Order Form renders a restaurant `<select>` dropdown instead of a raw ID input.

**References**:

| File | Change Reason |
|------|---------------|
| `SmartMenuOptim.Server/Features/Orders/Components/OrderList.razor.cs` | Removed `DemoRestaurantId` constant; added restaurant dropdown state and `[SupplyParameterFromQuery]` for deep-linking |
| `SmartMenuOptim.Server/Features/Orders/Components/OrderList.razor` | Added restaurant `<select>` dropdown with loading/empty states |
| `SmartMenuOptim.Server/Features/Orders/Components/OrderForm.razor.cs` | Removed hardcoded IDs; added `IRestaurantClientService` injection with restaurant dropdown state |
| `SmartMenuOptim.Server/Features/Orders/Components/OrderForm.razor` | Replaced `InputNumber` for Restaurant ID with `InputSelect` dropdown |