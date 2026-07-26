# Issue#: 008_INSIGHTS-NO-RECOMMENDATIONS__API_UI

| Field | Value |
|-------|-------|
| **Date** | 2026-07-17 |
| **Severity** | 🔴 High |
| **Status** | ✅ Resolved |
| **Layer** | API (validation) + Application (DTO) + UI (Blazor Server) |
| **Feature** | AI Menu Insights — Top Dish Recommendations (`/insights`) |

**Description**: The `/insights` page always rendered *"No recommendations available at this time. Please try again later. 🔁"* even though the DB held real data (20 reviews, 439 sales). The Server console logged:

```
fail: SmartMenuOptim.Server.Features.AI.Services.AIClientService[0]
      AI recommendation failed with status code BadRequest
```

**Root Cause** (two layers — one masked the other):

1. **Real blocker — auto-400 at model binding.** `AiController` is `[ApiController]` (`AiController.cs:52`). That attribute makes ASP.NET Core **recursively validate the request body** — including every element of `List<ReviewDTO>` — *before* the action body runs. `ReviewDTO.CustomerName` carried `[Required]`, but customer-linked reviews store `CustomerName = ""` (identity lives on `CustomerId`; the `Review` ctor hard-sets empty name). `RequiredAttribute` rejects empty strings, so the framework returned HTTP 400 before any recommendation code executed. The client logged `BadRequest` and the page fell to its `recommendations == null` branch.

2. **Masked secondary bug — CustomerName filter gate.** Even once the request bound, the `Recommend` filter required `!string.IsNullOrWhiteSpace(r.CustomerName)`, which would drop every review (all empty names) and yield `[]`. This was "Fix A" but it never ran because bug #1 rejected the request first.

**Common misdiagnosis**: The `AddDays(-7/-90/-360)` date filter lives only in `AiController.GetUnderperformingDishesAsync` — a **different** endpoint. `/insights` does not use it. The date window was never the cause.

**Resolution**:

1. **Fix A** — removed `&& !string.IsNullOrWhiteSpace(r.CustomerName)` from the `Recommend` filter (`AiController.cs:465`). Sentiment + non-empty comment + non-empty dish name are the valid gates; customer name is not.
2. **Fix B (the real fix)** — removed `[Required]` from `ReviewDTO.CustomerName` (kept `[StringLength(100)]`). Safe because `ReviewDTO` is a transport/read DTO: the review-submit form uses a separate `ReviewFormModel` (which keeps its own `[Required] CustomerName`), and the create POST binds the domain `Review` entity, not `ReviewDTO`. Name is genuinely optional.
3. **Cleanup — dead endpoint** — the legacy `Recommend_v1` (`AiController.cs:372`, never called by the Server) still had the same `CustomerName` filter gate; removed it and added an explanatory comment for consistency.
4. **Cleanup — code-behind** — `Insights.razor` held an inline `@code` block (violates the mandatory three-file Blazor pattern). Split logic into `Insights.razor.cs` (`sealed partial class Insights : ComponentBase`, services via `[Inject]`); removed `@code`, `@inject`, and `@using` from the `.razor`.

**Why `[Required]` on a DTO auto-400s the whole endpoint**:

```csharp
// [ApiController] validates List<ReviewDTO> recursively at binding time.
// Any element failing a data annotation => 400 before the action runs.
[Required] public string CustomerName { get; set; } = string.Empty; // ← rejects "" => auto-400
// Fix B: drop [Required]; keep length bound only.
[StringLength(100)] public string CustomerName { get; set; } = string.Empty;
```

**References**:

| File | Change Reason |
|------|---------------|
| `SmartMenuOptim.Application/Features/Reviews/DTOs/ReviewDTO.cs` | **Fix B** — removed `[Required]` from `CustomerName` (kept `[StringLength(100)]`); added comment explaining transport-DTO + `[ApiController]` auto-validation |
| `SmartMenuOptim.API/Features/Ai/v1/AiController.cs` | **Fix A** — dropped `CustomerName` gate from `Recommend` filter (line 465); **cleanup** — dropped same gate from dead `Recommend_v1` filter (line 389) + comment |
| `SmartMenuOptim.Server/Features/AI/Components/Insights.razor` | **Cleanup** — removed inline `@code`, `@inject`, `@using` |
| `SmartMenuOptim.Server/Features/AI/Components/Insights.razor.cs` | **Cleanup (new)** — code-behind: `sealed partial class Insights : ComponentBase`, `[Inject]` services, `OnInitializedAsync` data load |

**Notes**:

- Confirm data non-empty: `SELECT COUNT(*) FROM "Reviews";` (20) and `SELECT COUNT(*) FROM "SaleRecords";` (439) — never an empty-list guard trip.
- Other `ReviewDTO` constraints (`Comment` `[Required]`, `Rating` `[Range(1,5)]`, `DishName` `[Required]`, `DishId` `[Range(1,int.MaxValue)]`) all pass against seeded data — only `CustomerName [Required]` broke binding.
- Pattern: never put `[Required]` on a field that is legitimately empty in stored data when the DTO is used as an `[ApiController]` request body — auto-validation will 400 the whole call. Input validation belongs on the input model (`ReviewFormModel`), not the transport/read DTO.
- Builds: API 0 errors (14 pre-existing warnings), Server 0 errors (10 pre-existing warnings).
