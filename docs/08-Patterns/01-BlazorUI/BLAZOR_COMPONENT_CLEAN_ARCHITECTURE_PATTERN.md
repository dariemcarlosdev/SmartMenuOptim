# Blazor Component Clean Architecture Pattern

> **Pattern Type:** Architectural / Comprehensive Guide  
> **Applied In:** SmartMenuOptim.Server

> **AI Prompt Ready Documentation**
> 
> This document provides patterns and guidelines for creating clean, maintainable Blazor components following Clean Architecture, SOLID principles, and Vertical Slice Architecture.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture Layers](#architecture-layers)
3. [Pattern Components](#pattern-components)
4. [Implementation Guide](#implementation-guide)
5. [Code Examples](#code-examples)
6. [AI Prompt Templates](#ai-prompt-templates)

---

## Overview

### Goals

- **Separation of Concerns**: UI markup separate from business logic
- **Testability**: Components and services easily unit testable
- **Reusability**: Shared UI components reduce code duplication
- **Maintainability**: Clear patterns make code easier to understand and modify
- **Predictable State**: State container pattern for consistent state management

### Design Patterns Used (please ./docs/08-Patterns/README.md for details)

| Pattern | Purpose |
|---------|---------|
| **Code-Behind** | Separates C# logic from Razor markup |
| **State Container** | Centralizes component state management |
| **Adapter** | Adapts HTTP API calls to service interfaces |
| **Result** | Encapsulates success/failure without exceptions |
| **Observer** | State change notifications to components |

---

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────────┐
│                     Blazor Server Project                        │
├─────────────────────────────────────────────────────────────────┤
│  Components/Pages/         │  Components/Shared/                 │
│  ├── EntityDetail.razor    │  ├── LoadingSpinner.razor          │
│  └── EntityDetail.razor.cs │  ├── ErrorAlert.razor              │
│                            │  ├── NotFoundAlert.razor           │
│                            │  ├── DetailCard.razor              │
│                            │  └── StatItem.razor                │
├─────────────────────────────────────────────────────────────────┤
│  State/                    │  Services/                          │
│  └── EntityDetailState.cs  │  ├── Interfaces/                   │
│                            │  │   └── IEntityClientService.cs   │
│                            │  └── EntityClientService.cs        │
├─────────────────────────────────────────────────────────────────┤
│  Extensions/                                                     │
│  └── ServiceCollectionExtensions.cs (DI Registration)           │
└─────────────────────────────────────────────────────────────────┘
```

---

## Pattern Components

### 1. Reusable UI Components

#### LoadingSpinner.razor
```razor
@if (IsLoading)
{
    <div class="d-flex justify-content-center @ContainerClass">
        <div class="spinner-border @SpinnerClass" role="status">
            <span class="visually-hidden">@Message</span>
        </div>
    </div>
}

@code {
    [Parameter] public bool IsLoading { get; set; } = true;
    [Parameter] public string Message { get; set; } = "Loading...";
    [Parameter] public string SpinnerClass { get; set; } = "text-primary";
    [Parameter] public string ContainerClass { get; set; } = "py-5";
}
```

#### ErrorAlert.razor
```razor
@if (!string.IsNullOrWhiteSpace(Message))
{
    <div class="alert @AlertClass d-flex align-items-center" role="alert">
        <i class="bi @IconClass me-2"></i>
        <div class="flex-grow-1">
            @if (!string.IsNullOrWhiteSpace(Title))
            {
                <strong>@Title:</strong>
            }
            @Message
            @if (!string.IsNullOrWhiteSpace(BackLinkUrl))
            {
                <a href="@BackLinkUrl" class="alert-link ms-2">@BackLinkText</a>
            }
        </div>
        @if (IsDismissible)
        {
            <button type="button" class="btn-close" @onclick="OnDismiss"></button>
        }
    </div>
}

@code {
    [Parameter] public string? Message { get; set; }
    [Parameter] public string? Title { get; set; } = "Error";
    [Parameter] public string AlertType { get; set; } = "danger";
    [Parameter] public bool IsDismissible { get; set; } = false;
    [Parameter] public string? BackLinkUrl { get; set; }
    [Parameter] public string BackLinkText { get; set; } = "Go back";
    [Parameter] public EventCallback OnDismiss { get; set; }
}
```

#### DetailCard.razor
```razor
<div class="card shadow-sm @CardClass">
    <div class="card-header">
        <div class="d-flex justify-content-between align-items-center">
            <span>
                @if (!string.IsNullOrWhiteSpace(HeaderIcon))
                {
                    <i class="bi @HeaderIcon me-2"></i>
                }
                @HeaderTitle
            </span>
            @HeaderActions
        </div>
    </div>
    <div class="card-body">
        @ChildContent
    </div>
</div>

@code {
    [Parameter] public string? HeaderTitle { get; set; }
    [Parameter] public string? HeaderIcon { get; set; }
    [Parameter] public RenderFragment? HeaderActions { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string CardClass { get; set; } = "mb-4";
}
```

#### StatItem.razor
```razor
<div class="d-flex justify-content-between align-items-center @(ShowBorder ? "mb-3 pb-3 border-bottom" : "")">
    <span class="text-muted">@Label</span>
    @if (UseBadge)
    {
        <span class="badge @BadgeClass fs-6">@Value</span>
    }
    else
    {
        <span class="fw-semibold">@Value</span>
    }
</div>

@code {
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string? Value { get; set; }
    [Parameter] public bool UseBadge { get; set; } = false;
    [Parameter] public string BadgeClass { get; set; } = "bg-primary";
    [Parameter] public bool ShowBorder { get; set; } = true;
}
```

---

### 2. State Container Pattern

#### ComponentStateBase.cs (Base Class)
```csharp
public abstract class ComponentStateBase<TData> where TData : class
{
    private TData? _data;
    private bool _isLoading;
    private string? _error;

    public event Action? OnStateChanged;

    public TData? Data
    {
        get => _data;
        protected set { _data = value; NotifyStateChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        protected set { _isLoading = value; NotifyStateChanged(); }
    }

    public string? Error
    {
        get => _error;
        protected set { _error = value; NotifyStateChanged(); }
    }

    public bool HasData => Data is not null;
    public bool HasError => !string.IsNullOrWhiteSpace(Error);

    public virtual void Reset()
    {
        _data = null;
        _isLoading = false;
        _error = null;
        NotifyStateChanged();
    }

    protected void SetLoading()
    {
        _isLoading = true;
        _error = null;
        NotifyStateChanged();
    }

    protected void SetData(TData data)
    {
        _data = data;
        _isLoading = false;
        _error = null;
        NotifyStateChanged();
    }

    protected void SetError(string error)
    {
        _error = error;
        _isLoading = false;
        NotifyStateChanged();
    }

    protected void NotifyStateChanged() => OnStateChanged?.Invoke();
}
```

#### Entity-Specific State Container
```csharp
public class EntityDetailState : ComponentStateBase<EntityDTO>
{
    private readonly IEntityClientService _service;
    private readonly ILogger<EntityDetailState> _logger;

    public EntityDetailState(
        IEntityClientService service,
        ILogger<EntityDetailState> logger)
    {
        _service = service;
        _logger = logger;
    }

    public EntityDTO? Entity => Data;

    public async Task LoadAsync(int id, CancellationToken ct = default)
    {
        SetLoading();

        try
        {
            var result = await _service.GetByIdAsync(id, ct);
            
            if (result.IsSuccess && result.Value is not null)
            {
                SetData(result.Value);
                _logger.LogInformation("Loaded entity {Id}", id);
            }
            else
            {
                SetError(result.Error ?? "Entity not found.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading entity {Id}", id);
            SetError("An unexpected error occurred.");
        }
    }
}
```

---

### 3. Client Service Pattern (HTTP Adapter)

#### Interface
```csharp
public interface IEntityClientService
{
    Task<Result<EntityDTO>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<EntityDTO>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<EntityDTO>> CreateAsync(EntityCreateDTO dto, CancellationToken ct = default);
    Task<Result<EntityDTO>> UpdateAsync(EntityUpdateDTO dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
```

#### Implementation
```csharp
public class EntityClientService : IEntityClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EntityClientService> _logger;
    private const string ApiBasePath = "api/v1/entities";

    public EntityClientService(
        IHttpClientFactory httpClientFactory,
        ILogger<EntityClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<EntityDTO>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.GetAsync($"{ApiBasePath}/{id}", ct);

            if (response.IsSuccessStatusCode)
            {
                var entity = await response.Content.ReadFromJsonAsync<EntityDTO>(ct);
                return entity is not null
                    ? Result.Success(entity)
                    : Result.Failure<EntityDTO>("Entity not found.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to load entity.");
            return Result.Failure<EntityDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading entity {Id}", id);
            return Result.Failure<EntityDTO>("Unable to connect to the server.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading entity {Id}", id);
            return Result.Failure<EntityDTO>("An unexpected error occurred.");
        }
    }
}
```

---

### 4. Component Implementation

#### Razor View (EntityDetail.razor)
```razor
@page "/entities/{Id:int}"
@using SmartMenuOptim.Server.Components.Shared

<PageTitle>Entity Details</PageTitle>

<div class="container mt-4">
    <LoadingSpinner IsLoading="_loading" Message="Loading entity..." />

    @if (!_loading && _error is not null)
    {
        <ErrorAlert Message="@_error" 
                    BackLinkUrl="/entities" 
                    IsDismissible="true"
                    OnDismiss="DismissError" />
    }

    @if (!_loading && _error is null && _entity is null)
    {
        <NotFoundAlert Message="Entity not found." BackLinkUrl="/entities" />
    }

    @if (!_loading && _entity is not null)
    {
        <DetailCard HeaderTitle="Entity Information" HeaderIcon="bi-info-circle">
            <StatItem Label="Name" Value="@_entity.Name" />
            <StatItem Label="Status" Value="@_entity.Status" UseBadge="true" />
            <StatItem Label="Created" Value="@_entity.CreatedAt.ToString("MMM dd, yyyy")" ShowBorder="false" />
        </DetailCard>
    }
</div>
```

#### Code-Behind (EntityDetail.razor.cs)
```csharp
public partial class EntityDetail : ComponentBase, IDisposable
{
    [Inject] private EntityDetailState State { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public int Id { get; set; }

    private EntityDTO? _entity => State.Entity;
    private bool _loading => State.IsLoading;
    private string? _error => State.Error;

    protected override async Task OnInitializedAsync()
    {
        State.OnStateChanged += HandleStateChanged;
        await State.LoadAsync(Id);
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);
    
    private void DismissError() => State.ClearError();

    public void Dispose()
    {
        State.OnStateChanged -= HandleStateChanged;
        GC.SuppressFinalize(this);
    }
}
```

---

### 5. Service Registration

```csharp
public static IServiceCollection AddAppServices(this IServiceCollection services)
{
    // Client Services (HTTP-based adapters)
    services.AddScoped<IEntityClientService, EntityClientService>();
    
    // State Containers (Scoped for per-circuit state)
    services.AddScoped<EntityDetailState>();
    
    services.AddLogging();
    return services;
}
```

---

## Implementation Guide

### Step-by-Step Process

1. **Create Shared UI Components** (if not existing)
   - `LoadingSpinner.razor`
   - `ErrorAlert.razor`
   - `NotFoundAlert.razor`
   - `DetailCard.razor`
   - `StatItem.razor`

2. **Create Client Service Interface and Implementation**
   - Define `IEntityClientService` interface
   - Implement `EntityClientService` with HTTP calls
   - Use Result pattern for return values

3. **Create State Container**
   - Inherit from `ComponentStateBase<TData>`
   - Inject client service
   - Implement domain-specific operations

4. **Create Component with Code-Behind**
   - `.razor` file: Only markup using shared components
   - `.razor.cs` file: Logic, state subscription, navigation

5. **Register Services**
   - Add client service as Scoped
   - Add state container as Scoped

### Checklist

- [ ] Shared UI components exist in `Components/Shared/`
- [ ] Client service interface defined in `Services/Interfaces/`
- [ ] Client service implementation in `Services/`
- [ ] State container in `State/`
- [ ] Component uses code-behind pattern
- [ ] Services registered in `ServiceCollectionExtensions`
- [ ] Component implements `IDisposable` for cleanup

---

## AI Prompt Templates

> **Prompt Engineering Best Practices Applied:**
> - Include **minimal code snippets** for critical patterns
> - Use **placeholders** (`[Entity]`, `[entity]`) for adaptability  
> - Reference existing code sections for full examples
> - Focus on **structure and intent** over complete implementations

---

### 🚀 Condensed Prompts (Token-Efficient)

> **Use these for token-limited contexts (~200-300 tokens each)**

#### Detail Page (Condensed)
```
Create [Entity]Detail Blazor component following SmartMenuOptim patterns:
- Files: I[Entity]ClientService, [Entity]ClientService, [Entity]DetailState, [Entity]Detail.razor/.cs
- Use: ComponentStateBase<T>, Result pattern, shared components (LoadingSpinner, ErrorAlert, DetailCard)
- Pattern: State subscription in OnInitializedAsync, IDisposable cleanup
- Reference: RestaurantDetail implementation
```

#### List Page (Condensed)
```
Create [Entity]List Blazor component:
- State: [Entity]ListState : ComponentStateBase<IReadOnlyList<[Entity]DTO>>
- Features: LoadAsync, DeleteAsync with refresh, HasItems property
- UI: Table with actions, empty state, ErrorAlert
- Reference: Pattern doc section "Creating a New List Page"
```

#### Form Page (Condensed)
```
Create [Entity]Form Blazor component (Create/Edit modes):
- Model: [Entity]FormModel with validation, FromDto/ToDto mappers
- State: [Entity]FormState with InitializeForCreate/Edit, SubmitAsync
- Routes: /[entities]/create and /[entities]/{Id:int}/edit
- UI: EditForm, DataAnnotationsValidator, submit loading state
```

---

### 📋 Full Prompts (Comprehensive)

> **Use these when context allows (~800-900 tokens each)**

---

### Creating a New Detail Page Component

```markdown
Create a Blazor component for viewing [Entity] details following the Clean Architecture pattern.

## Context
- Project: SmartMenuOptim.Server (Blazor Server, .NET 9)
- Existing patterns: See `RestaurantDetail` implementation as reference
- Shared components location: `Components/Shared/`

## Files to Create

### 1. Client Service Interface
**File:** `Services/Interfaces/I[Entity]ClientService.cs`

```csharp
public interface I[Entity]ClientService
{
    Task<Result<[Entity]DTO>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<[Entity]DTO>>> GetAllAsync(CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
```

### 2. Client Service Implementation  
**File:** `Services/[Entity]ClientService.cs`
- Inject `IHttpClientFactory` and `ILogger`
- Use `ApiErrorHelper.GetErrorMessageAsync()` for error handling
- API path: `api/v1/[entities]`
- Return `Result<T>` for all operations

### 3. State Container
**File:** `State/[Entity]DetailState.cs`

```csharp
public class [Entity]DetailState : ComponentStateBase<[Entity]DTO>
{
    private readonly I[Entity]ClientService _service;
    private readonly ILogger<[Entity]DetailState> _logger;

    public [Entity]DTO? [Entity] => Data;

    public async Task LoadAsync(int id, CancellationToken ct = default)
    {
        SetLoading();
        try
        {
            var result = await _service.GetByIdAsync(id, ct);
            if (result.IsSuccess && result.Value is not null)
                SetData(result.Value);
            else
                SetError(result.Error ?? "[Entity] not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading [entity] {Id}", id);
            SetError("An unexpected error occurred.");
        }
    }
}
```

### 4. Razor Component
**File:** `Components/Pages/[Entity]/[Entity]Detail.razor`

```razor
@page "/[entities]/{Id:int}"
@using SmartMenuOptim.Server.Components.Shared

<PageTitle>[Entity] Details - SmartMenuOptimizer</PageTitle>

<div class="container mt-4">
    <LoadingSpinner IsLoading="_loading" Message="Loading [entity]..." />

    @if (!_loading && _error is not null)
    {
        <ErrorAlert Message="@_error" 
                    BackLinkUrl="/[entities]" 
                    IsDismissible="true"
                    OnDismiss="DismissError" />
    }

    @if (!_loading && _error is null && _[entity] is null)
    {
        <NotFoundAlert Message="[Entity] not found." BackLinkUrl="/[entities]" />
    }

    @if (!_loading && _[entity] is not null)
    {
        @* Add DetailCard and StatItem components for [Entity] properties *@
    }
</div>
```

### 5. Code-Behind
**File:** `Components/Pages/[Entity]/[Entity]Detail.razor.cs`

```csharp
public partial class [Entity]Detail : ComponentBase, IDisposable
{
    [Inject] private [Entity]DetailState State { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public int Id { get; set; }

    private [Entity]DTO? _[entity] => State.[Entity];
    private bool _loading => State.IsLoading;
    private string? _error => State.Error;

    protected override async Task OnInitializedAsync()
    {
        State.OnStateChanged += HandleStateChanged;
        await State.LoadAsync(Id);
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);
    private void DismissError() => State.ClearError();

    public void Dispose()
    {
        State.OnStateChanged -= HandleStateChanged;
        GC.SuppressFinalize(this);
    }
}
```

### 6. Service Registration
**File:** `Extensions/ServiceCollectionExtensions.cs`

Add to `AddAppServices()`:
```csharp
services.AddScoped<I[Entity]ClientService, [Entity]ClientService>();
services.AddScoped<[Entity]DetailState>();
```

## Requirements
- Use existing shared components (LoadingSpinner, ErrorAlert, NotFoundAlert, DetailCard, StatItem)
- Follow Result pattern for all service returns
- Implement IDisposable for state cleanup
- Use XML documentation comments on public members
```

---

### Creating a New List Page Component

```markdown
Create a Blazor component for listing [Entity] items following the Clean Architecture pattern.

## Context
- Extend existing `I[Entity]ClientService` if it exists
- Use shared components from `Components/Shared/`

## Files to Create/Modify

### 1. State Container
**File:** `State/[Entity]ListState.cs`

```csharp
public class [Entity]ListState : ComponentStateBase<IReadOnlyList<[Entity]DTO>>
{
    private readonly I[Entity]ClientService _service;
    private readonly ILogger<[Entity]ListState> _logger;

    public IReadOnlyList<[Entity]DTO> [Entities] => Data ?? [];
    public bool HasItems => [Entities].Count > 0;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        SetLoading();
        try
        {
            var result = await _service.GetAllAsync(ct);
            if (result.IsSuccess)
                SetData(result.Value ?? []);
            else
                SetError(result.Error ?? "Failed to load [entities].");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading [entities]");
            SetError("An unexpected error occurred.");
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var result = await _service.DeleteAsync(id, ct);
            if (result.IsSuccess)
            {
                await LoadAsync(ct); // Refresh list
                return true;
            }
            SetError(result.Error ?? "Failed to delete [entity].");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting [entity] {Id}", id);
            SetError("An unexpected error occurred.");
            return false;
        }
    }
}
```

### 2. Razor Component Structure
**File:** `Components/Pages/[Entity]/[Entity]List.razor`

```razor
@page "/[entities]"

<PageTitle>[Entities] - SmartMenuOptimizer</PageTitle>

<div class="container mt-4">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h2>[Entities]</h2>
        <button class="btn btn-primary" @onclick="NavigateToCreate">
            <i class="bi bi-plus-lg me-1"></i> Add [Entity]
        </button>
    </div>

    <LoadingSpinner IsLoading="_loading" />

    <ErrorAlert Message="@_error" IsDismissible="true" OnDismiss="DismissError" />

    @if (!_loading && !State.HasItems)
    {
        <div class="alert alert-info">
            No [entities] found. <a href="/[entities]/create">Create one</a>
        </div>
    }

    @if (!_loading && State.HasItems)
    {
        <div class="table-responsive">
            <table class="table table-hover">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Status</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var item in _[entities])
                    {
                        <tr>
                            <td>@item.Name</td>
                            <td><span class="badge bg-success">@item.Status</span></td>
                            <td>
                                <button class="btn btn-sm btn-outline-primary" 
                                        @onclick="() => NavigateToDetail(item.Id)">
                                    View
                                </button>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
</div>
```

## Requirements
- Implement delete confirmation modal
- Support pagination if list can be large
- Use code-behind pattern with IDisposable
```

---

### Creating a Form Component (Create/Edit)

```markdown
Create a Blazor form component for creating/editing [Entity] following Clean Architecture.

## Context
- Support both Create (no Id) and Edit (with Id) modes
- Use EditForm with DataAnnotationsValidator
- Handle optimistic UI updates

## Files to Create

### 1. Form Model (if not using DTO directly)
**File:** `Components/Pages/[Entity]/Models/[Entity]FormModel.cs`

```csharp
public class [Entity]FormModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    // Map from DTO
    public static [Entity]FormModel FromDto([Entity]DTO dto) => new()
    {
        Name = dto.Name,
        Description = dto.Description
    };

    // Map to Create DTO
    public [Entity]CreateDTO ToCreateDto() => new()
    {
        Name = Name,
        Description = Description
    };

    // Map to Update DTO
    public [Entity]UpdateDTO ToUpdateDto(int id) => new()
    {
        Id = id,
        Name = Name,
        Description = Description
    };
}
```

### 2. Form State Container
**File:** `State/[Entity]FormState.cs`

```csharp
public class [Entity]FormState : ComponentStateBase<[Entity]FormModel>
{
    private readonly I[Entity]ClientService _service;
    private readonly ILogger<[Entity]FormState> _logger;
    private bool _isSubmitting;
    private int? _editId;

    public [Entity]FormModel Model => Data ?? new();
    public bool IsSubmitting => _isSubmitting;
    public bool IsEditMode => _editId.HasValue;

    public void InitializeForCreate()
    {
        _editId = null;
        SetData(new [Entity]FormModel());
    }

    public async Task InitializeForEditAsync(int id, CancellationToken ct = default)
    {
        _editId = id;
        SetLoading();

        var result = await _service.GetByIdAsync(id, ct);
        if (result.IsSuccess && result.Value is not null)
            SetData([Entity]FormModel.FromDto(result.Value));
        else
            SetError(result.Error ?? "[Entity] not found.");
    }

    public async Task<bool> SubmitAsync(CancellationToken ct = default)
    {
        _isSubmitting = true;
        NotifyStateChanged();

        try
        {
            var result = IsEditMode
                ? await _service.UpdateAsync(Model.ToUpdateDto(_editId!.Value), ct)
                : await _service.CreateAsync(Model.ToCreateDto(), ct);

            _isSubmitting = false;

            if (result.IsSuccess)
                return true;

            SetError(result.Error ?? "Failed to save [entity].");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving [entity]");
            _isSubmitting = false;
            SetError("An unexpected error occurred.");
            return false;
        }
    }
}
```

### 3. Form Razor Component
**File:** `Components/Pages/[Entity]/[Entity]Form.razor`

```razor
@page "/[entities]/create"
@page "/[entities]/{Id:int}/edit"

<PageTitle>@(_isEditMode ? "Edit" : "Create") [Entity]</PageTitle>

<div class="container mt-4">
    <h2>@(_isEditMode ? "Edit" : "Create") [Entity]</h2>

    <LoadingSpinner IsLoading="_loading" />
    <ErrorAlert Message="@_error" IsDismissible="true" OnDismiss="DismissError" />

    @if (!_loading && _model is not null)
    {
        <EditForm Model="_model" OnValidSubmit="HandleSubmit">
            <DataAnnotationsValidator />
            <ValidationSummary class="text-danger" />

            <div class="mb-3">
                <label class="form-label">Name</label>
                <InputText class="form-control" @bind-Value="_model.Name" />
                <ValidationMessage For="() => _model.Name" class="text-danger" />
            </div>

            <div class="mb-3">
                <label class="form-label">Description</label>
                <InputTextArea class="form-control" @bind-Value="_model.Description" rows="3" />
            </div>

            <div class="d-flex gap-2">
                <button type="submit" class="btn btn-primary" disabled="@_isSubmitting">
                    @if (_isSubmitting)
                    {
                        <span class="spinner-border spinner-border-sm me-1"></span>
                    }
                    @(_isEditMode ? "Update" : "Create")
                </button>
                <button type="button" class="btn btn-outline-secondary" @onclick="Cancel">
                    Cancel
                </button>
            </div>
        </EditForm>
    }
</div>
```

## Requirements
- Validate on both client and server
- Show loading state during submit
- Navigate to detail page on success
- Confirm navigation if form is dirty (optional)
```

---

## File Structure Reference

```
SmartMenuOptim.Server/
├── Components/
│   ├── Pages/
│   │   └── [Entity]/
│   │       ├── [Entity]Detail.razor
│   │       ├── [Entity]Detail.razor.cs
│   │       ├── [Entity]List.razor
│   │       ├── [Entity]List.razor.cs
│   │       ├── [Entity]Form.razor
│   │       └── [Entity]Form.razor.cs
│   └── Shared/
│       ├── LoadingSpinner.razor
│       ├── ErrorAlert.razor
│       ├── NotFoundAlert.razor
│       ├── DetailCard.razor
│       └── StatItem.razor
├── Services/
│   ├── Interfaces/
│   │   └── I[Entity]ClientService.cs
│   └── [Entity]ClientService.cs
├── State/
│   ├── ComponentStateBase.cs
│   └── [Entity]DetailState.cs
├── Helpers/
│   └── ApiErrorHelper.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

---

## Benefits Summary

| Benefit | Description |
|---------|-------------|
| **Testability** | State containers and services can be unit tested independently |
| **Reusability** | Shared components reduce duplication across pages |
| **Maintainability** | Clear separation makes code easier to modify |
| **Consistency** | Same patterns across all components |
| **Type Safety** | Strong typing with Result pattern and DTOs |
| **Error Handling** | Centralized, consistent error handling |

---

## Related Documentation

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Blazor Component Lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle)
- [State Management in Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management)
- [Result Pattern](https://www.milanjovanovic.tech/blog/functional-error-handling-in-dotnet-with-the-result-pattern)

---

## Quick Reference Card

> **Copy-paste ready snippets for common operations**

### State Subscription Pattern
```csharp
// In OnInitializedAsync
State.OnStateChanged += HandleStateChanged;
await State.LoadAsync(Id);

// Handler
private void HandleStateChanged() => InvokeAsync(StateHasChanged);

// In Dispose
State.OnStateChanged -= HandleStateChanged;
```

### Result Pattern Usage
```csharp
// Success with value
return Result.Success(entity);

// Success without value
return Result.Success();

// Failure
return Result.Failure<EntityDTO>("Error message");

// Checking result
if (result.IsSuccess && result.Value is not null)
    // handle success
else
    // handle failure with result.Error
```

### Service HTTP Call Pattern
```csharp
var client = _httpClientFactory.CreateClient("BackendAPI");
var response = await client.GetAsync($"{ApiBasePath}/{id}", ct);

if (response.IsSuccessStatusCode)
{
    var entity = await response.Content.ReadFromJsonAsync<EntityDTO>(ct);
    return Result.Success(entity!);
}

var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Default error");
return Result.Failure<EntityDTO>(error);
```

### Shared Component Quick Usage
```razor
@* Loading *@
<LoadingSpinner IsLoading="_loading" />

@* Error with dismiss *@
<ErrorAlert Message="@_error" IsDismissible="true" OnDismiss="DismissError" />

@* Not found *@
<NotFoundAlert Message="Item not found." BackLinkUrl="/items" />

@* Card with header action *@
<DetailCard HeaderTitle="Info" HeaderIcon="bi-info">
    <HeaderActions>
        <button class="btn btn-sm btn-primary">Edit</button>
    </HeaderActions>
    <ChildContent>
        <StatItem Label="Name" Value="@item.Name" />
        <StatItem Label="Count" Value="@item.Count" UseBadge="true" ShowBorder="false" />
    </ChildContent>
</DetailCard>
```

---

*Document Version: 1.1*
*Last Updated: 2025-03-01*
*Target Framework: .NET 9 / Blazor Server*
