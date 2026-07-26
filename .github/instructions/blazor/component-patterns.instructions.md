---
applyTo: "**/*.razor, **/*.razor.cs, **/*.razor.css"
---

# Blazor Component Patterns — NexTruzt.io EscrowApp

## Mandatory Code-Behind Pattern

Every Blazor component consists of **three files**. No exceptions.

```
Components/Pages/EscrowDashboard/
├── EscrowDashboard.razor        ← Markup only (HTML + Razor directives)
├── EscrowDashboard.razor.cs     ← Logic (partial class, lifecycle, event handlers)
└── EscrowDashboard.razor.css    ← Scoped styles (Bootstrap 5 overrides only)
```

### .razor — Markup

Contains HTML, Razor directives, and component references. **No `@code {}` blocks.**

```razor
@page "/escrow/dashboard"
@attribute [Authorize(Policy = "EscrowUser")]
@attribute [StreamRendering]

<PageTitle>@Localizer["EscrowDashboard.Title"]</PageTitle>

<div class="container-fluid mt-3">
    <h1 class="mb-4">@Localizer["EscrowDashboard.Heading"]</h1>

    @if (_transactions is null)
    {
        <div class="d-flex justify-content-center">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">@Localizer["Loading"]</span>
            </div>
        </div>
    }
    else
    {
        <table class="table table-striped table-hover">
            <thead class="table-dark">
                <tr>
                    <th>@Localizer["Column.Id"]</th>
                    <th>@Localizer["Column.Amount"]</th>
                    <th>@Localizer["Column.Status"]</th>
                    <th>@Localizer["Column.Actions"]</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var tx in _transactions)
                {
                    <EscrowTransactionRow Transaction="tx"
                                          OnRelease="HandleReleaseAsync"
                                          OnDispute="HandleDisputeAsync" />
                }
            </tbody>
        </table>
    }
</div>
```

### .razor.cs — Code-Behind

Must be a `partial` class matching the `.razor` filename. Owns all logic.

```csharp
namespace EscrowApp.Components.Pages.EscrowDashboard;

public sealed partial class EscrowDashboard : ComponentBase, IDisposable
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState> AuthState { get; set; } = default!;

    private IReadOnlyList<EscrowTransactionDto>? _transactions;
    private CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        var result = await Mediator.Send(
            new GetEscrowTransactionsQuery(), _cts.Token);
        _transactions = result.Transactions;
    }

    private async Task HandleReleaseAsync(Guid transactionId)
    {
        await Mediator.Send(
            new ReleaseFundsCommand(transactionId), _cts.Token);
        // Refresh data after action
        await OnInitializedAsync();
    }

    private async Task HandleDisputeAsync(Guid transactionId)
    {
        await Mediator.Send(
            new DisputeFundsCommand(transactionId), _cts.Token);
        await OnInitializedAsync();
    }

    public void Dispose() => _cts.Cancel();
}
```

### .razor.css — Scoped Styles

Component-scoped CSS. Use only for overrides beyond Bootstrap 5 defaults.

```css
/* EscrowDashboard.razor.css */
h1 {
    font-weight: 600;
    color: var(--bs-primary);
}

::deep .status-badge {
    font-size: 0.85rem;
    min-width: 5rem;
    text-align: center;
}
```

---

## Component Lifecycle

| Method | Use When |
|---|---|
| `OnInitializedAsync` | Loading data on first render — primary data-fetch location |
| `OnParametersSetAsync` | Reacting to parameter changes from parent (e.g., selected transaction ID) |
| `OnAfterRenderAsync(firstRender)` | JS interop setup, DOM measurements — guard with `if (firstRender)` |
| `ShouldRender()` | Skipping re-renders on high-frequency updates (e.g., real-time feeds) |
| `Dispose` / `DisposeAsync` | Cleaning up `CancellationTokenSource`, timers, event subscriptions |

**Never** use the constructor for async work. Always use `OnInitializedAsync`.

---

## Bootstrap 5 Class Conventions

Use these standard Bootstrap 5 classes consistently:

| Element | Classes |
|---|---|
| Primary actions | `btn btn-primary` |
| Danger/cancel | `btn btn-outline-danger` |
| Data tables | `table table-striped table-hover` |
| Table headers | `table-dark` on `<thead>` |
| Status badges | `badge bg-success`, `badge bg-warning text-dark`, `badge bg-danger` |
| Cards | `card`, `card-header`, `card-body` |
| Forms | `form-control`, `form-label`, `form-select`, `form-check` |
| Layout | `container-fluid`, `row`, `col-md-*` |
| Spacing | `mt-3`, `mb-4`, `p-3` — use Bootstrap spacing utilities |
| Alerts | `alert alert-info`, `alert alert-danger` |

**Do NOT** use inline `style` attributes. Apply Bootstrap utility classes or scoped CSS instead.

---

## Localization

Inject `IStringLocalizer<SharedResource>` in every component that renders user-facing text.

```csharp
// In .razor.cs
[Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = default!;
```

```razor
<!-- In .razor — reference as @Localizer["Key"] -->
<h1>@Localizer["EscrowDashboard.Heading"]</h1>
<button class="btn btn-primary">@Localizer["Button.HoldFunds"]</button>
```

- Supported locales: `en-US` (default), `es-MX`
- Resource keys: dot-separated, context-prefixed (e.g., `EscrowDashboard.Title`, `Button.HoldFunds`)
- Never hardcode user-visible strings — always use localizer keys

---

## Parent-Child Communication

### EventCallback&lt;T&gt; — Child notifies parent

```csharp
// Child component (.razor.cs)
[Parameter] public EventCallback<Guid> OnRelease { get; set; }

private async Task ReleaseClicked() =>
    await OnRelease.InvokeAsync(_transactionId);
```

```razor
<!-- Parent component (.razor) -->
<EscrowTransactionRow Transaction="tx" OnRelease="HandleReleaseAsync" />
```

### CascadingParameter — Reserved for auth state only

```csharp
// Only use CascadingParameter for authentication state
[CascadingParameter]
private Task<AuthenticationState> AuthState { get; set; } = default!;
```

Do **not** cascade custom state objects. Use `IMediator` or scoped DI services instead.

---

## StreamRendering for Progressive Loading

Apply `[StreamRendering]` on pages that fetch data in `OnInitializedAsync`. This renders the page shell immediately and streams content as data becomes available.

```razor
@attribute [StreamRendering]
```

Pair with a loading indicator:

```razor
@if (_data is null)
{
    <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">@Localizer["Loading"]</span>
    </div>
}
else
{
    <!-- Render data -->
}
```

---

## IDisposable Cleanup

Always implement `IDisposable` (or `IAsyncDisposable`) when the component owns:
- `CancellationTokenSource`
- `Timer` or `PeriodicTimer`
- Event handler subscriptions
- `DotNetObjectReference` or `IJSObjectReference`

```csharp
public sealed partial class MyComponent : ComponentBase, IDisposable
{
    private CancellationTokenSource _cts = new();

    public void Dispose() => _cts.Cancel();
}
```

---

## Hard Rules

| Rule | Rationale |
|---|---|
| ❌ No `@code { }` blocks in `.razor` files | Separation of concerns — logic lives in `.razor.cs` |
| ❌ No inline `style="..."` attributes | Use Bootstrap utilities or scoped `.razor.css` |
| ❌ No direct repository or DbContext injection | Go through `IMediator.Send()` only |
| ❌ No `IHttpContextAccessor` in components | Use `[CascadingParameter] Task<AuthenticationState>` |
| ✅ Always `partial class` in `.razor.cs` | Required for code-behind to work |
| ✅ Always scoped `.razor.css` per component | Prevents style leakage across components |
| ✅ Always localize user-facing strings | Required for en-US / es-MX support |
| ✅ Always cancel async work on Dispose | Prevents memory leaks and circuit issues |
