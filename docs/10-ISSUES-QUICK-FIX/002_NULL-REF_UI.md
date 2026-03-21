# Issue#: 002_NULL-REF_UI

| Field | Value |
|-------|-------|
| **Date** | 2026-03-15 |
| **Severity** | 🟠 High |
| **Status** | ✅ Resolved |

**Description**: `System.NullReferenceException` thrown on the Restaurant List page (`/restaurants`) when loading order counts per restaurant card. The exception occurred inside `LoadOrderCountsAsync` when accessing `.Count` on a null collection.

**Root Cause**: `Result<T>.Value` can be `null` even when `IsSuccess == true`. The original code `result.IsSuccess ? result.Value : []` returned `null` on the true branch when `Value` was null, then `.Count` threw `NullReferenceException`. The same unsafe pattern also existed in `Dashboard.razor` for the Order Metrics section.

**Resolution**: Replaced unsafe ternary checks with C# pattern matching that guards against both failure and null value: `result is { IsSuccess: true, Value: not null }`. Applied consistently across both affected files.

**References**:

| File | Change Reason |
|------|---------------|
| `SmartMenuOptim.Server/Features/Restaurants/Components/RestaurantList.razor.cs` | Null-safe pattern matching for `Result<T>.Value` in order count loading |
| `SmartMenuOptim.Server/Features/Dashboard/Components/Dashboard.razor` | Same null-safe pattern matching for `Result<T>.Value` in Order Metrics `.SelectMany()` |