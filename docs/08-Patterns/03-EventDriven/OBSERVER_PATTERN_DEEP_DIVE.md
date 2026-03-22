# Observer Pattern (State Change Notification)

> **Pattern Type:** Behavioral  
> **Applied In:** SmartMenuOptim.Server

---

## Overview

The Observer Pattern establishes a one-to-many dependency between objects. When the state container changes, all subscribed components are automatically notified and can re-render.

---

## Problem It Solves

| Problem | Solution |
|---------|----------|
| Components don't know when state changes | Subscribe to OnStateChanged event |
| Manual UI refresh needed | Automatic notification triggers StateHasChanged |
| Tight coupling between state and UI | Loose coupling via event subscription |
| Multiple components need same state updates | All subscribers notified simultaneously |

---

## When to Use This Pattern

Use the Observer pattern when **one object's state change must automatically notify and update multiple dependent objects** — without tight coupling between them.

✅ **Use when:**
- Multiple UI components need to react to the same state change
- Asynchronous operations (API calls, background tasks) must refresh the UI on completion
- You want clean separation between state management and rendering logic
- Components at different places in the component tree share the same data source

❌ **Avoid when:**
- State is local to a single component — use `@bind` or local fields instead
- Parent→child data flow only — use `[Parameter]` and `EventCallback`
- You need complex state transitions with undo/redo — consider a state machine or Redux-style store

### Real-World Scenarios

| # | Scenario | How Observer Applies |
|---|----------|---------------------|
| 1 | **Restaurant dashboard with live order count** | The `OrderListState` container fetches orders from the API. Three independent components observe it: a counter badge in the navbar, the order table on the main page, and a summary card in the sidebar. When a new order arrives and `SetData()` is called, all three re-render automatically via `OnStateChanged`. No component knows the others exist. |
| 2 | **Menu editing with unsaved-changes indicator** | A `MenuEditState` container tracks whether the user has modified any dish. The "Save" button component, the browser-tab title component, and a "discard changes" banner all subscribe to `OnStateChanged`. When the user types a new price, the state container updates and all three UI elements reflect the change instantly — the button enables, the tab shows "●", and the banner appears. |
| 3 | **Multi-step order form with validation summary** | An `OrderFormState` holds the current order being built (customer, dishes, quantities). A step-indicator component, the running total component, and a validation-error list component each subscribe. As the user adds dishes or changes quantities, every observer updates in sync — the total recalculates, the step indicator advances, and validation errors clear — all from a single `NotifyStateChanged()` call. |

---

## Implementation

### State Container (Subject/Observable)

```csharp
public abstract class ComponentStateBase<TData> where TData : class
{
    // The event that observers subscribe to
    public event Action? OnStateChanged;

    private TData? _data;

    public TData? Data
    {
        get => _data;
        protected set
        {
            _data = value;
            NotifyStateChanged();  // Notify all observers
        }
    }

    // Notify all subscribed components
    protected void NotifyStateChanged() => OnStateChanged?.Invoke();
}
```

### Component (Observer)

```csharp
public partial class RestaurantDetail : ComponentBase, IDisposable
{
    [Inject] private RestaurantDetailState State { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to state changes
        State.OnStateChanged += HandleStateChanged;
        
        await State.LoadAsync(Id);
    }

    // Handler - re-render when state changes
    private void HandleStateChanged()
    {
        // InvokeAsync ensures we're on the correct thread
        InvokeAsync(StateHasChanged);
    }

    // Unsubscribe when component is disposed
    public void Dispose()
    {
        State.OnStateChanged -= HandleStateChanged;
        GC.SuppressFinalize(this);
    }
}
```

---

## Sequence Diagram

```
┌──────────────┐     ┌─────────────────────┐     ┌──────────────┐
│  Component   │     │   State Container   │     │   Service    │
│  (Observer)  │     │    (Subject)        │     │              │
└──────┬───────┘     └──────────┬──────────┘     └──────┬───────┘
       │                        │                       │
       │ Subscribe              │                       │
       │ (OnStateChanged +=)    │                       │
       │───────────────────────▶│                       │
       │                        │                       │
       │ LoadAsync(id)          │                       │
       │───────────────────────▶│                       │
       │                        │                       │
       │                        │ SetLoading()          │
       │                        │ NotifyStateChanged()  │
       │◀───────────────────────│                       │
       │ HandleStateChanged()   │                       │
       │ StateHasChanged()      │                       │
       │                        │                       │
       │                        │ GetByIdAsync()        │
       │                        │──────────────────────▶│
       │                        │                       │
       │                        │◀──────────────────────│
       │                        │ Result<T>             │
       │                        │                       │
       │                        │ SetData()/SetError()  │
       │                        │ NotifyStateChanged()  │
       │◀───────────────────────│                       │
       │ HandleStateChanged()   │                       │
       │ StateHasChanged()      │                       │
       │                        │                       │
       │ Dispose()              │                       │
       │ (OnStateChanged -=)    │                       │
       │───────────────────────▶│                       │
       │                        │                       │
```

---

## Key Implementation Details

### 1. Using InvokeAsync

```csharp
// Always use InvokeAsync when calling StateHasChanged from event handlers
private void HandleStateChanged()
{
    InvokeAsync(StateHasChanged);  // ✅ Thread-safe
}

// Don't call StateHasChanged directly
private void HandleStateChanged()
{
    StateHasChanged();  // ❌ May not be on UI thread
}
```

### 2. Proper Cleanup

```csharp
public void Dispose()
{
    // Always unsubscribe to prevent memory leaks
    State.OnStateChanged -= HandleStateChanged;
    
    // Suppress finalizer (IDisposable best practice)
    GC.SuppressFinalize(this);
}
```

### 3. Multiple State Containers

```csharp
public partial class Dashboard : ComponentBase, IDisposable
{
    [Inject] private RestaurantListState RestaurantState { get; set; } = default!;
    [Inject] private OrderListState OrderState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to multiple state containers
        RestaurantState.OnStateChanged += HandleStateChanged;
        OrderState.OnStateChanged += HandleStateChanged;
        
        await Task.WhenAll(
            RestaurantState.LoadAsync(),
            OrderState.LoadAsync()
        );
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        // Unsubscribe from all
        RestaurantState.OnStateChanged -= HandleStateChanged;
        OrderState.OnStateChanged -= HandleStateChanged;
        GC.SuppressFinalize(this);
    }
}
```

---

## Alternative: Using Action<T> with Data

```csharp
// State container with typed event
public abstract class ComponentStateBase<TData> where TData : class
{
    public event Action<TData?>? OnDataChanged;
    public event Action<string?>? OnErrorChanged;
    public event Action<bool>? OnLoadingChanged;

    protected void SetData(TData data)
    {
        _data = data;
        OnDataChanged?.Invoke(data);
    }

    protected void SetError(string error)
    {
        _error = error;
        OnErrorChanged?.Invoke(error);
    }
}

// Component can handle specific changes
protected override void OnInitialized()
{
    State.OnDataChanged += HandleDataChanged;
    State.OnErrorChanged += HandleErrorChanged;
}

private void HandleDataChanged(RestaurantDTO? data)
{
    // React specifically to data changes
    InvokeAsync(StateHasChanged);
}
```

---

## Benefits

| Benefit | Description |
|---------|-------------|
| **Loose Coupling** | Components don't depend on state internals |
| **Automatic Updates** | UI refreshes when state changes |
| **Multiple Observers** | Many components can observe same state |
| **Single Notification** | One event triggers all updates |
| **Clean Separation** | State logic separate from UI |

---

## Common Mistakes

### ❌ Forgetting to Unsubscribe

```csharp
// Memory leak - handler keeps component alive
protected override void OnInitialized()
{
    State.OnStateChanged += HandleStateChanged;
    // Missing Dispose() implementation!
}
```

### ❌ Not Implementing IDisposable

```csharp
// Component won't be properly cleaned up
public partial class MyComponent : ComponentBase  // Missing IDisposable
{
    // ...
}
```

### ❌ Calling StateHasChanged Directly

```csharp
// May cause threading issues
private void HandleStateChanged()
{
    StateHasChanged();  // ❌ Not thread-safe
}
```

---

## When to Use

✅ **Use When:**
- State changes need to trigger UI updates
- Multiple components share state
- Asynchronous operations affect UI
- Clean separation between state and UI needed

❌ **Consider Alternatives When:**
- Simple local state (use @bind instead)
- One-time data fetch (no updates needed)
- Parent-child only (use [Parameter] and EventCallback)

---

## Related Patterns

- [State Container Pattern](./STATE_CONTAINER_PATTERN.md) - The subject being observed
- [Code-Behind Pattern](./CODE_BEHIND_PATTERN.md) - Where subscription happens

---

## References

- [Observer Pattern](https://refactoring.guru/design-patterns/observer)
- [C# Events Tutorial](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/events/)
- [Blazor Component Lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle)

---

*Document Version: 1.0*  
*Last Updated: 2025-03-01*
