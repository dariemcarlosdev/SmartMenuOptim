# State Container Pattern — Prompt-Ready Implementation Guide

> **Pattern Type:** Behavioral / State Management  
> **Applied In:** SmartMenuOptim.Server (Blazor Server)  
> **Last Updated:** 2026-03-11

---

## Purpose

This document is a **prompt-ready reference** for applying the State Container Pattern to any Blazor Server component in SmartMenuOptimizer. Copy the relevant sections into your prompt when scaffolding a new feature.

---

## Architecture Overview

```
┌──────────────────────┐
│   Razor Component    │  UI only — subscribes to state changes
│  (.razor + .razor.cs)│  delegates all logic to state container
└──────────┬───────────┘
           │ [Inject]
           ▼
┌──────────────────────┐
│   State Container    │  Owns data, loading, error, mutation logic
│ (ComponentStateBase) │  raises OnStateChanged via base class
└──────────┬───────────┘
           │ [Inject]
           ▼
┌──────────────────────┐
│   Client Service     │  HTTP adapter — translates API responses
│ (I*ClientService)    │  into Result<T> (never raw HTTP)
└──────────────────────┘
```

### Data Flow

```
User Action → Component → State.MethodAsync() → ClientService → HTTP → API
     ↑                         │
     └── StateHasChanged ◄─── OnStateChanged (NotifyStateChanged)
```

---

## File Placement

```
SmartMenuOptim.Server/
├── Features/
│   └── {Feature}/
│       ├── Services/
│       │   ├── I{Entity}ClientService.cs    ← Interface
│       │   └── {Entity}ClientService.cs     ← HTTP adapter
│       ├── State/
│       │   ├── {Entity}ListState.cs         ← List page state
│       │   ├── {Entity}EditorState.cs       ← Create/Edit page state
│       │   └── {Entity}DetailState.cs       ← Detail page state
│       └── Components/
│           ├── {Entity}List.razor            ← Markup only
│           ├── {Entity}List.razor.cs         ← Code-behind
│           ├── {Entity}Form.razor
│           └── {Entity}Form.razor.cs
└── State/
    └── ComponentStateBase.cs                ← Base class (shared)
```

---

## Naming Conventions

| Layer | Pattern | Example |
|-------|---------|---------|
| Client interface | `I{Entity}ClientService` | `IMenuClientService` |
| Client implementation | `{Entity}ClientService` | `MenuClientService` |
| List state | `{Entity}ListState` | `MenuListState` |
| Editor/Form state | `{Entity}EditorState` | `MenuEditorState` |
| Detail state | `{Entity}DetailState` | `RestaurantDetailState` |

---

## Layer 1: Client Service Interface

**Location:** `Server/Features/{Feature}/Services/I{Entity}ClientService.cs`

### Rules

- Mirror the Application layer service but adapted for HTTP consumption
- Every method returns `Result<T>` or `Result` — never raw `HttpResponseMessage`
- Accept `CancellationToken` on all async methods
- Never inject into components directly — only into state containers

### Template

```csharp
using SmartMenuOptim.Application.Common;

namespace SmartMenuOptim.Server.Features.{Feature}.Services;

public interface I{Entity}ClientService
{
    Task<Result<{Entity}DTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<{Entity}DTO>>> GetAllAsync(int parentId, CancellationToken cancellationToken = default);
    Task<Result<{Entity}DTO>> CreateAsync({Entity}CreateDTO dto, CancellationToken cancellationToken = default);
    Task<Result<{Entity}DTO>> UpdateAsync({Entity}UpdateDTO dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
```

---

## Layer 2: Client Service Implementation

**Location:** `Server/Features/{Feature}/Services/{Entity}ClientService.cs`

### Rules

- Inject `IHttpClientFactory`, create client via `CreateClient("BackendAPI")`
- Wrap every HTTP call in try/catch → map to `Result.Success()` / `Result.Failure()`
- Use `ApiErrorHelper.GetErrorMessageAsync()` for error extraction from API responses
- Log errors with structured logging (`ILogger<{Entity}ClientService>`)

### Template

```csharp
using System.Net.Http.Json;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Server.Helpers;

namespace SmartMenuOptim.Server.Features.{Feature}.Services;

public class {Entity}ClientService : I{Entity}ClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<{Entity}ClientService> _logger;
    private const string ApiBasePath = "api/v1/{entities}";

    public {Entity}ClientService(IHttpClientFactory httpClientFactory, ILogger<{Entity}ClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<{Entity}DTO>>> GetAllAsync(int parentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var items = await client.GetFromJsonAsync<List<{Entity}DTO>>(
                $"api/v1/parents/{parentId}/{entities}", cancellationToken);
            return Result.Success<IReadOnlyList<{Entity}DTO>>(items ?? []);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading {entities}");
            return Result.Failure<IReadOnlyList<{Entity}DTO>>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading {entities}");
            return Result.Failure<IReadOnlyList<{Entity}DTO>>("An unexpected error occurred.");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.DeleteAsync($"{ApiBasePath}/{id}", cancellationToken);

            if (response.IsSuccessStatusCode) return Result.Success();

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to delete.");
            return Result.Failure(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting {entity} {Id}", id);
            return Result.Failure("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting {entity} {Id}", id);
            return Result.Failure("An unexpected error occurred.");
        }
    }
}
```

---

## Layer 3: State Container

**Location:** `Server/Features/{Feature}/State/{Entity}ListState.cs`

### Rules

- Inherit from `ComponentStateBase<TData>`
- Inject the client service interface — never `IHttpClientFactory`
- Expose read-only properties for component binding (e.g., `Items => Data`)
- Use `SetLoading()`, `SetData()`, `SetError()` from base class
- Add operation-specific state (e.g., `IsDeleting`, `IsSaving`, `ShowDeleteModal`)
- Include a `ClearError()` method
- Register as **Scoped** — one instance per Blazor circuit

### List State Template

```csharp
using SmartMenuOptim.Server.State;

namespace SmartMenuOptim.Server.Features.{Feature}.State;

public class {Entity}ListState : ComponentStateBase<IReadOnlyList<{Entity}DTO>>
{
    private readonly I{Entity}ClientService _{entity}Service;
    private readonly ILogger<{Entity}ListState> _logger;

    private bool _deleting;
    private {Entity}DTO? _itemToDelete;
    private bool _showDeleteModal;

    public {Entity}ListState(I{Entity}ClientService service, ILogger<{Entity}ListState> logger)
    {
        _{entity}Service = service;
        _logger = logger;
    }

    // Read-only projections
    public IReadOnlyList<{Entity}DTO>? Items => Data;
    public bool IsDeleting => _deleting;
    public {Entity}DTO? ItemToDelete => _itemToDelete;
    public bool ShowDeleteModal => _showDeleteModal;

    // Load
    public async Task LoadAsync(int parentId, CancellationToken ct = default)
    {
        SetLoading();
        var result = await _{entity}Service.GetAllAsync(parentId, ct);

        if (result.IsSuccess)
            SetData(result.Value ?? []);
        else
            SetError(result.Error ?? "Failed to load.");
    }

    // Delete flow
    public void ConfirmDelete({Entity}DTO item)
    {
        _itemToDelete = item;
        _showDeleteModal = true;
        NotifyStateChanged();
    }

    public void CancelDelete()
    {
        _itemToDelete = null;
        _showDeleteModal = false;
        NotifyStateChanged();
    }

    public async Task DeleteAsync(CancellationToken ct = default)
    {
        if (_itemToDelete is null) return;
        _deleting = true;
        NotifyStateChanged();

        var result = await _{entity}Service.DeleteAsync(_itemToDelete.Id, ct);

        if (result.IsSuccess)
        {
            if (Data is not null)
                SetData(Data.Where(x => x.Id != _itemToDelete.Id).ToList());
            CancelDelete();
        }
        else
        {
            SetError(result.Error ?? "Failed to delete.");
        }

        _deleting = false;
        NotifyStateChanged();
    }

    public void ClearError() { if (HasError) SetError(null!); }
}
```

### Editor State Template

```csharp
public class {Entity}EditorState : ComponentStateBase<{Entity}DTO>
{
    private readonly I{Entity}ClientService _{entity}Service;
    private readonly ILogger<{Entity}EditorState> _logger;
    private bool _saving;

    public {Entity}EditorState(I{Entity}ClientService service, ILogger<{Entity}EditorState> logger)
    {
        _{entity}Service = service;
        _logger = logger;
    }

    public {Entity}DTO? Item => Data;
    public bool IsSaving { get => _saving; private set { _saving = value; NotifyStateChanged(); } }

    public async Task LoadAsync(int id, CancellationToken ct = default)
    {
        SetLoading();
        var result = await _{entity}Service.GetByIdAsync(id, ct);

        if (result.IsSuccess && result.Value is not null)
            SetData(result.Value);
        else
            SetError(result.Error ?? "Not found.");
    }

    public async Task<bool> CreateAsync({Entity}CreateDTO dto, CancellationToken ct = default)
    {
        IsSaving = true;
        var result = await _{entity}Service.CreateAsync(dto, ct);
        IsSaving = false;

        if (result.IsSuccess) return true;

        SetError(result.Error ?? "Failed to create.");
        return false;
    }

    public async Task<bool> UpdateAsync({Entity}UpdateDTO dto, CancellationToken ct = default)
    {
        IsSaving = true;
        var result = await _{entity}Service.UpdateAsync(dto, ct);
        IsSaving = false;

        if (result.IsSuccess) return true;

        SetError(result.Error ?? "Failed to update.");
        return false;
    }

    public void ClearError() { if (HasError) SetError(null!); }
}
```

---

## Layer 4: Component Code-Behind

**Location:** `Components/Pages/{Feature}/{Entity}List.razor.cs`

### Rules

- Inject the state container and `NavigationManager` — nothing else for data
- Expose state as private read-only properties: `private bool _loading => State.IsLoading;`
- Subscribe to `State.OnStateChanged` in `OnInitializedAsync`
- Implement `IDisposable` to unsubscribe
- All user actions delegate to state
- Keep navigation logic in the component (not the state)
- Keep UI-only helpers (e.g., `TruncateText`) in the component

### List Component Template

```csharp
public partial class {Entity}List : ComponentBase, IDisposable
{
    [Inject] private {Entity}ListState State { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public int ParentId { get; set; }

    // Read-only projections from state
    private IReadOnlyList<{Entity}DTO>? _items => State.Items;
    private bool _loading => State.IsLoading;
    private string? _error => State.Error;
    private bool _showDeleteModal => State.ShowDeleteModal;
    private {Entity}DTO? _itemToDelete => State.ItemToDelete;
    private bool _deleting => State.IsDeleting;

    protected override async Task OnInitializedAsync()
    {
        State.OnStateChanged += HandleStateChanged;
        await State.LoadAsync(ParentId);
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);

    // Delegate to state
    private void ConfirmDelete({Entity}DTO item) => State.ConfirmDelete(item);
    private void CancelDelete() => State.CancelDelete();
    private async Task DeleteAsync() => await State.DeleteAsync();

    // Navigation stays in component
    private void CreateNew() => Navigation.NavigateTo($"/{entities}/new");
    private void Edit(int id) => Navigation.NavigateTo($"/{entities}/{id}/edit");

    public void Dispose()
    {
        State.OnStateChanged -= HandleStateChanged;
        GC.SuppressFinalize(this);
    }
}
```

### Editor Component Template

```csharp
public partial class {Entity}Editor : ComponentBase, IDisposable
{
    [Inject] private {Entity}EditorState State { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public int ParentId { get; set; }
    [Parameter] public int? Id { get; set; }

    private bool _isEdit => Id.HasValue;
    private bool _loading => State.IsLoading;
    private bool _saving => State.IsSaving;
    private string? _error => State.Error;

    protected override async Task OnInitializedAsync()
    {
        State.OnStateChanged += HandleStateChanged;
        if (_isEdit) await State.LoadAsync(Id!.Value);
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);

    private async Task HandleSubmitAsync()
    {
        bool success;
        if (_isEdit)
            success = await State.UpdateAsync(BuildUpdateDto());
        else
            success = await State.CreateAsync(BuildCreateDto());

        if (success) Navigation.NavigateTo($"/parent/{ParentId}/{entities}");
    }

    private void Cancel() => Navigation.NavigateTo($"/parent/{ParentId}/{entities}");

    public void Dispose()
    {
        State.OnStateChanged -= HandleStateChanged;
        GC.SuppressFinalize(this);
    }
}
```

---

## DI Registration

**Location:** `Server/Extensions/ServiceCollectionExtensions.cs` → `AddAppServices()`

```csharp
public static IServiceCollection AddAppServices(this IServiceCollection services)
{
    // Client Services (HTTP-based adapters for API communication)
    services.AddScoped<I{Entity}ClientService, {Entity}ClientService>();

    // State Containers (Scoped for per-circuit state in Blazor Server)
    services.AddScoped<{Entity}ListState>();
    services.AddScoped<{Entity}EditorState>();

    return services;
}
```

---

## Design Patterns Combined

| Pattern | Where | Purpose |
|---------|-------|---------|
| **State Container** | `*State.cs` | Single source of truth for component state |
| **Observer** | `OnStateChanged` event | Component re-renders when state mutates |
| **Result** | `Result<T>` returns | Explicit success/failure without exceptions |
| **Adapter** | `*ClientService.cs` | Translates HTTP → domain-friendly interface |
| **DI Scoping** | `AddScoped<>` | One state instance per Blazor circuit (user session) |

---

## Anti-Patterns to Avoid

| ❌ Don't | ✅ Do Instead |
|----------|--------------|
| Inject `IHttpClientFactory` in components | Inject State Container only |
| Put try/catch in component code-behind | Handle errors in Client Service → Result |
| Store state in component fields | State lives in the State Container |
| Call `StateHasChanged()` manually after data ops | Subscribe to `OnStateChanged` event |
| Put navigation logic in state containers | Keep `Navigation.NavigateTo()` in component |
| Forget to unsubscribe from `OnStateChanged` | Implement `IDisposable` on every component |

---

## API Controller Note

All `v1` controllers must use a base `[Route("api/v1")]` attribute on the class. Action routes are then **relative** to the base (e.g., `[HttpGet("categories/{id:int}")]`). This enables `CreatedAtAction(nameof(GetByIdAsync), new { id }, value)` to resolve the `Location` header correctly. Without the base route, `CreatedAtAction` fails with `No route matches the supplied values`.

---

## When to Use

✅ **Use When:**
- Component has complex state logic (load + CRUD + modals)
- Multiple operations affect the same state
- State needs to be shared across components
- You need to unit test state logic independently

❌ **Avoid When:**
- Simple components with minimal state (one read-only list, no mutations)
- State is truly local to one component with no async operations

---

## Existing Implementations

| State Container | Component | Entity |
|----------------|-----------|--------|
| `RestaurantListState` | `RestaurantList` | Restaurant |
| `RestaurantDetailState` | `RestaurantDetail` | Restaurant |
| `MenuListState` | `MenuList` | Menu |
| `MenuEditorState` | `MenuEditor` | Menu |

---

## Related Patterns

- [Observer Pattern](./OBSERVER_PATTERN.md) — `OnStateChanged` event mechanism
- [Response/Result Pattern](./RESPONSE_RESULT_PATTERN.md) — `Result<T>` operation outcomes
- [Client Service Adapter Pattern](./CLIENT_SERVICE_ADAPTER_PATTERN.md) — HTTP-to-service translation
- [Code-Behind Pattern](./CODE_BEHIND_PATTERN.md) — `.razor` / `.razor.cs` separation

---

## References

- [Blazor State Management — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management)
- [State Container Pattern in Blazor — Chris Sainty](https://chrissainty.com/3-ways-to-communicate-between-components-in-blazor/)
- Base class: `SmartMenuOptim.Server/State/ComponentStateBase.cs`

---

*Document Version: 1.0*  
*Last Updated: 2026-03-11*
