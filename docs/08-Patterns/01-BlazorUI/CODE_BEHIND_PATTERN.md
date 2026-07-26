# Code-Behind Pattern

> **Pattern Type:** Structural / Separation of Concerns  
> **Applied In:** SmartMenuOptim.Server

---

## Overview

The Code-Behind Pattern separates C# logic from Razor markup by using partial classes. The `.razor` file contains only UI markup while the `.razor.cs` file contains all C# logic.

---

## Problem It Solves

| Problem | Solution |
|---------|----------|
| Large `@code` blocks mixed with markup | Logic moved to separate file |
| Difficult to navigate component files | Clear separation by file |
| Hard to unit test component logic | Code-behind can be tested independently |
| IDE struggles with large mixed files | Better tooling support |

---

## Implementation

### File Structure

```
Components/Pages/Restaurant/
├── RestaurantDetail.razor      # Markup only
└── RestaurantDetail.razor.cs   # Logic only (partial class)
```

### Razor File (Markup Only)

**File:** `RestaurantDetail.razor`

```razor
@page "/restaurants/{Id:int}"
@using SmartMenuOptim.Server.Components.Shared

<PageTitle>Restaurant Details - SmartMenuOptimizer</PageTitle>

<div class="container mt-4">
    <LoadingSpinner IsLoading="_loading" Message="Loading restaurant..." />

    @if (!_loading && _error is not null)
    {
        <ErrorAlert Message="@_error" 
                    BackLinkUrl="/restaurants" 
                    IsDismissible="true"
                    OnDismiss="DismissError" />
    }

    @if (!_loading && _restaurant is not null)
    {
        <DetailCard HeaderTitle="Information" HeaderIcon="bi-info">
            <StatItem Label="Name" Value="@_restaurant.Name" />
        </DetailCard>
    }
</div>
```

### Code-Behind File (Logic Only)

**File:** `RestaurantDetail.razor.cs`

```csharp
using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Dtos.Restaurant;
using SmartMenuOptim.Server.State;

namespace SmartMenuOptim.Server.Components.Pages.Restaurant;

/// <summary>
/// Code-behind for RestaurantDetail component.
/// </summary>
public partial class RestaurantDetail : ComponentBase, IDisposable
{
    // Dependency Injection
    [Inject] private RestaurantDetailState State { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    // Route Parameters
    [Parameter] public int Id { get; set; }

    // State Properties (exposed to view)
    private RestaurantDTO? _restaurant => State.Restaurant;
    private bool _loading => State.IsLoading;
    private string? _error => State.Error;

    // Lifecycle Methods
    protected override async Task OnInitializedAsync()
    {
        State.OnStateChanged += HandleStateChanged;
        await State.LoadAsync(Id);
    }

    // Event Handlers
    private void HandleStateChanged() => InvokeAsync(StateHasChanged);
    private void DismissError() => State.ClearError();

    // Navigation Methods
    private void NavigateToEdit() => Navigation.NavigateTo($"/restaurants/{Id}/edit");
    private void NavigateToList() => Navigation.NavigateTo("/restaurants");

    // Cleanup
    public void Dispose()
    {
        State.OnStateChanged -= HandleStateChanged;
        GC.SuppressFinalize(this);
    }
}
```

---

## Key Conventions

### 1. Partial Class Declaration

```csharp
// The class MUST be partial to merge with Razor-generated code
public partial class RestaurantDetail : ComponentBase
```

### 2. Namespace Must Match

```csharp
// Namespace must match the folder structure
namespace SmartMenuOptim.Server.Components.Pages.Restaurant;
```

### 3. Inject Attributes for DI

```csharp
// Use [Inject] attribute instead of @inject directive
[Inject] private NavigationManager Navigation { get; set; } = default!;
```

### 4. Parameter Attributes

```csharp
// Use [Parameter] attribute for route/component parameters
[Parameter] public int Id { get; set; }
```

---

## Organization Guidelines

### Code-Behind Structure

```csharp
public partial class EntityDetail : ComponentBase, IDisposable
{
    // ═══════════════════════════════════════════════════════════
    // DEPENDENCY INJECTION
    // ═══════════════════════════════════════════════════════════
    [Inject] private EntityDetailState State { get; set; } = default!;

    // ═══════════════════════════════════════════════════════════
    // PARAMETERS
    // ═══════════════════════════════════════════════════════════
    [Parameter] public int Id { get; set; }

    // ═══════════════════════════════════════════════════════════
    // STATE PROPERTIES
    // ═══════════════════════════════════════════════════════════
    private EntityDTO? _entity => State.Entity;
    private bool _loading => State.IsLoading;

    // ═══════════════════════════════════════════════════════════
    // LIFECYCLE METHODS
    // ═══════════════════════════════════════════════════════════
    protected override async Task OnInitializedAsync() { }
    protected override void OnParametersSet() { }

    // ═══════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════
    private void HandleClick() { }
    private async Task HandleSubmitAsync() { }

    // ═══════════════════════════════════════════════════════════
    // NAVIGATION
    // ═══════════════════════════════════════════════════════════
    private void NavigateToEdit() { }

    // ═══════════════════════════════════════════════════════════
    // CLEANUP
    // ═══════════════════════════════════════════════════════════
    public void Dispose() { }
}
```

---

## Razor File Guidelines

### What Goes in .razor

- `@page` directive
- `@using` statements
- `<PageTitle>` component
- HTML markup
- Razor syntax (`@if`, `@foreach`, etc.)
- Component usage with parameters
- Event bindings (`@onclick`, `@onchange`)

### What Goes in .razor.cs

- Dependency injection
- Parameters and cascading parameters
- Private fields and properties
- Lifecycle methods
- Event handlers
- Navigation methods
- Helper methods
- IDisposable implementation

---

## Benefits

| Benefit | Description |
|---------|-------------|
| **Separation of Concerns** | UI and logic in separate files |
| **Better IDE Support** | Full C# IntelliSense in .cs file |
| **Easier Navigation** | Find logic quickly in dedicated file |
| **Cleaner Markup** | Razor file is readable HTML |
| **Testability** | Logic can be unit tested |
| **Code Organization** | Natural grouping of related code |

---

## Common Mistakes

### ❌ Wrong: Mixing @inject and [Inject]

```csharp
// Don't mix directive-style with attribute-style
// .razor file
@inject NavigationManager Navigation  // ❌ Don't use both

// .razor.cs file
[Inject] private NavigationManager Navigation { get; set; }  // ✅ Use this only
```

### ❌ Wrong: Missing Partial Keyword

```csharp
// This will cause compilation errors
public class RestaurantDetail : ComponentBase  // ❌ Missing partial
{
}

// Correct
public partial class RestaurantDetail : ComponentBase  // ✅
{
}
```

### ❌ Wrong: Namespace Mismatch

```csharp
// .razor is in Components/Pages/Restaurant/
// .razor.cs must use matching namespace
namespace SmartMenuOptim.Server.Components.Pages;  // ❌ Wrong
namespace SmartMenuOptim.Server.Components.Pages.Restaurant;  // ✅ Correct
```

---

## When to Use

✅ **Use When:**
- Component has significant C# logic
- Component has multiple event handlers
- Component implements IDisposable
- Component has complex state management
- Team prefers separation of markup and logic

❌ **Consider Inline When:**
- Very simple components (< 20 lines of code)
- Components with only a few parameters
- Pure display components with no logic

---

## Related Patterns

- [State Container Pattern](./STATE_CONTAINER_PATTERN.md) - Often used with code-behind
- [Reusable Components Pattern](./REUSABLE_COMPONENTS_PATTERN.md) - Simple components may not need code-behind

---

## References

- [Blazor Partial Class Support](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/#partial-class-support)
- [ASP.NET Core Blazor Components](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/)

---

*Document Version: 1.0*  
*Last Updated: 2025-03-01*
