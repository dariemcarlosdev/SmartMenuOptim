# Blazor Server Reference

> **Load when:** Building components, managing state, implementing code-behind, CSS isolation, or JS interop.

## Component Architecture

### Code-Behind Pattern (Required)

Every Blazor component uses the code-behind pattern: `.razor` for markup, `.razor.cs` for logic.

```razor
@* EscrowDashboard.razor — markup only, no @code block *@
@page "/escrows"
@attribute [Authorize(Policy = "EscrowOperator")]

<PageTitle>Escrow Dashboard</PageTitle>

<h1>My Escrows</h1>

@if (_loading)
{
    <div class="spinner-border" role="status">
        <span class="visually-hidden">Loading...</span>
    </div>
}
else if (_escrows.Count == 0)
{
    <div class="alert alert-info">No escrow transactions found.</div>
}
else
{
    <div class="table-responsive">
        <table class="table table-striped">
            <thead>
                <tr>
                    <th>ID</th>
                    <th>Amount</th>
                    <th>Status</th>
                    <th>Created</th>
                    <th>Actions</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var escrow in _escrows)
                {
                    <EscrowRow Escrow="escrow" OnRelease="HandleRelease" />
                }
            </tbody>
        </table>
    </div>
}
```

```csharp
// EscrowDashboard.razor.cs — all logic here
namespace EscrowApp.Presentation.Components.Pages;

public sealed partial class EscrowDashboard : ComponentBase, IAsyncDisposable
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState> AuthState { get; set; } = default!;

    private IReadOnlyList<EscrowSummaryDto> _escrows = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        var userId = state.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        _escrows = await Mediator.Send(new GetUserEscrowsQuery(userId));
        _loading = false;
    }

    private async Task HandleRelease(Guid escrowId)
    {
        await Mediator.Send(new ReleaseEscrowCommand(escrowId));
        _escrows = await Mediator.Send(new GetUserEscrowsQuery(
            (await AuthState).User.FindFirstValue(ClaimTypes.NameIdentifier)!));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

## Scoped CSS (Required)

Every component gets its own `.razor.css` file.

```css
/* EscrowDashboard.razor.css — scoped to this component only */
h1 {
    color: var(--bs-primary);
    margin-bottom: 1.5rem;
}

.table th {
    background-color: var(--bs-light);
    font-weight: 600;
}

.spinner-border {
    display: block;
    margin: 2rem auto;
}
```

**CSS isolation rules:**
- File must be named exactly `ComponentName.razor.css`
- Styles are automatically scoped — no leaking to parent or child components
- Use `::deep` only when styling child component elements is unavoidable
- Prefer Bootstrap utility classes over custom CSS

## Parameters and Events

```csharp
// Child component with parameters
public sealed partial class EscrowRow : ComponentBase
{
    [Parameter, EditorRequired]
    public EscrowSummaryDto Escrow { get; set; } = default!;

    [Parameter]
    public EventCallback<Guid> OnRelease { get; set; }

    private bool _releasing;

    private async Task Release()
    {
        _releasing = true;
        await OnRelease.InvokeAsync(Escrow.Id);
        _releasing = false;
    }
}
```

## State Management

### Scoped State Service

```csharp
// Scoped service — one instance per SignalR circuit (per user)
public sealed class EscrowStateService
{
    public event Action? OnChange;

    private EscrowDetailDto? _currentEscrow;
    public EscrowDetailDto? CurrentEscrow
    {
        get => _currentEscrow;
        set
        {
            _currentEscrow = value;
            OnChange?.Invoke();
        }
    }
}

// Registration
builder.Services.AddScoped<EscrowStateService>();

// Usage in component
[Inject] private EscrowStateService State { get; set; } = default!;

protected override void OnInitialized() => State.OnChange += StateHasChanged;

public void Dispose() => State.OnChange -= StateHasChanged;
```

### Cascading Values

```razor
@* App.razor — cascading the current tenant *@
<CascadingValue Value="@_currentTenant">
    <Router AppAssembly="typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="routeData" />
        </Found>
    </Router>
</CascadingValue>
```

## Forms and Validation

```razor
@* CreateEscrowForm.razor *@
<EditForm Model="_model" OnValidSubmit="HandleSubmit" FormName="create-escrow">
    <FluentValidationValidator />
    <ValidationSummary />

    <div class="mb-3">
        <label class="form-label">Buyer ID</label>
        <InputText @bind-Value="_model.BuyerId" class="form-control" />
        <ValidationMessage For="() => _model.BuyerId" />
    </div>

    <div class="mb-3">
        <label class="form-label">Amount</label>
        <InputNumber @bind-Value="_model.Amount" class="form-control" />
        <ValidationMessage For="() => _model.Amount" />
    </div>

    <button type="submit" class="btn btn-primary" disabled="@_submitting">
        @(_submitting ? "Creating..." : "Create Escrow")
    </button>
</EditForm>
```

## Lifecycle Methods

| Method | Use For | Async Version |
|---|---|---|
| `OnInitialized` | One-time setup, sync data loading | `OnInitializedAsync` |
| `OnParametersSet` | React to parameter changes | `OnParametersSetAsync` |
| `OnAfterRender` | JS interop, DOM access | `OnAfterRenderAsync` |
| `ShouldRender` | Skip unnecessary re-renders | N/A (sync only) |
| `Dispose` | Clean up subscriptions, timers | `DisposeAsync` |

## JS Interop

```csharp
// Collocated JS module: EscrowDashboard.razor.js
export function showToast(message, type) {
    // Bootstrap toast logic
}

// Code-behind
[Inject] private IJSRuntime JS { get; set; } = default!;
private IJSObjectReference? _module;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
        _module = await JS.InvokeAsync<IJSObjectReference>(
            "import", "./Components/Pages/EscrowDashboard.razor.js");
}

private async Task ShowSuccess(string message) =>
    await _module!.InvokeVoidAsync("showToast", message, "success");

public async ValueTask DisposeAsync()
{
    if (_module is not null)
        await _module.DisposeAsync();
}
```

## Rendering Optimization

```csharp
// Use @key for list items
@foreach (var escrow in _escrows)
{
    <EscrowRow @key="escrow.Id" Escrow="escrow" />
}

// Skip re-render for high-frequency updates
protected override bool ShouldRender() => _dataChanged;

// StateHasChanged from non-UI thread (timer, SignalR callback)
private void OnSignalRUpdate(EscrowUpdate update)
{
    InvokeAsync(() =>
    {
        _escrows = _escrows.Select(e => 
            e.Id == update.EscrowId ? e with { Status = update.NewStatus } : e).ToList();
        StateHasChanged();
    });
}
```
