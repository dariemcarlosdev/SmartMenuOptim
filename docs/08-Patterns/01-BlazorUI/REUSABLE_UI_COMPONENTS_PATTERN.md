# Reusable UI Components Pattern

> **Pattern Type:** Structural / Composition  
> **Applied In:** SmartMenuOptim.Server

---

## Overview

The Reusable UI Components Pattern extracts common UI elements into standalone Blazor components with configurable parameters. This reduces code duplication and ensures visual consistency across the application.

---

## Problem It Solves

| Problem | Solution |
|---------|----------|
| Duplicate HTML/CSS patterns | Extract to shared components |
| Inconsistent UI across pages | Single source of truth |
| Hard to update common elements | Change once, update everywhere |
| Verbose page markup | Clean component-based markup |
| Difficult to test UI patterns | Isolated testable components |

---

## Implementation

### Component Location

```
SmartMenuOptim.Server/
└── Components/
    └── Shared/
        ├── LoadingSpinner.razor
        ├── ErrorAlert.razor
        ├── NotFoundAlert.razor
        ├── DetailCard.razor
        └── StatItem.razor
```

---

## Components

### LoadingSpinner

**Purpose:** Consistent loading state display with accessibility support.

**File:** `Components/Shared/LoadingSpinner.razor`

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

**Usage:**
```razor
<LoadingSpinner IsLoading="_loading" Message="Loading restaurant..." />
<LoadingSpinner IsLoading="_saving" SpinnerClass="text-success" ContainerClass="py-2" />
```

---

### ErrorAlert

**Purpose:** Standardized error display with optional dismiss and navigation.

**File:** `Components/Shared/ErrorAlert.razor`

```razor
@if (!string.IsNullOrWhiteSpace(Message))
{
    <div class="alert @AlertClass d-flex align-items-center @(IsDismissible ? "alert-dismissible fade show" : "")" 
         role="alert">
        <i class="bi @IconClass me-2"></i>
        <div class="flex-grow-1">
            @if (!string.IsNullOrWhiteSpace(Title))
            {
                <strong>@Title:</strong>
                @(" ")
            }
            @Message
            @if (!string.IsNullOrWhiteSpace(BackLinkUrl))
            {
                <a href="@BackLinkUrl" class="alert-link ms-2">@BackLinkText</a>
            }
        </div>
        @if (IsDismissible)
        {
            <button type="button" class="btn-close" @onclick="OnDismiss" aria-label="Close"></button>
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

    private string AlertClass => $"alert-{AlertType}";
    
    private string IconClass => AlertType switch
    {
        "danger" => "bi-exclamation-triangle-fill",
        "warning" => "bi-exclamation-circle-fill",
        "info" => "bi-info-circle-fill",
        "success" => "bi-check-circle-fill",
        _ => "bi-exclamation-triangle-fill"
    };
}
```

**Usage:**
```razor
@* Basic error *@
<ErrorAlert Message="@_error" />

@* Dismissible error *@
<ErrorAlert Message="@_error" IsDismissible="true" OnDismiss="ClearError" />

@* Warning with back link *@
<ErrorAlert Message="@_error" 
            AlertType="warning" 
            BackLinkUrl="/restaurants" 
            BackLinkText="Back to list" />
```

---

### NotFoundAlert

**Purpose:** Consistent "not found" display with navigation option.

**File:** `Components/Shared/NotFoundAlert.razor`

```razor
<div class="alert alert-warning" role="alert">
    <i class="bi bi-question-circle me-2"></i>
    @Message
    @if (!string.IsNullOrWhiteSpace(BackLinkUrl))
    {
        <a href="@BackLinkUrl" class="alert-link">@BackLinkText</a>
    }
</div>

@code {
    [Parameter] public string Message { get; set; } = "Resource not found.";
    [Parameter] public string? BackLinkUrl { get; set; }
    [Parameter] public string BackLinkText { get; set; } = "Go back";
}
```

**Usage:**
```razor
<NotFoundAlert Message="Restaurant not found." 
               BackLinkUrl="/restaurants" 
               BackLinkText="Back to restaurants" />
```

---

### DetailCard

**Purpose:** Card layout with header, optional icon, and action buttons.

**File:** `Components/Shared/DetailCard.razor`

```razor
<div class="card shadow-sm @CardClass">
    <div class="card-header @HeaderClass">
        @if (HeaderTemplate is not null)
        {
            @HeaderTemplate
        }
        else
        {
            <div class="d-flex justify-content-between align-items-center">
                <span>
                    @if (!string.IsNullOrWhiteSpace(HeaderIcon))
                    {
                        <i class="bi @HeaderIcon me-2"></i>
                    }
                    @HeaderTitle
                </span>
                @if (HeaderActions is not null)
                {
                    @HeaderActions
                }
            </div>
        }
    </div>
    <div class="card-body @BodyClass">
        @ChildContent
    </div>
    @if (FooterContent is not null)
    {
        <div class="card-footer @FooterClass">
            @FooterContent
        </div>
    }
</div>

@code {
    [Parameter] public string? HeaderTitle { get; set; }
    [Parameter] public string? HeaderIcon { get; set; }
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }
    [Parameter] public RenderFragment? HeaderActions { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? FooterContent { get; set; }
    [Parameter] public string CardClass { get; set; } = "mb-4";
    [Parameter] public string HeaderClass { get; set; } = "";
    [Parameter] public string BodyClass { get; set; } = "";
    [Parameter] public string FooterClass { get; set; } = "";
}
```

**Usage:**
```razor
@* Simple card *@
<DetailCard HeaderTitle="Contact Information" HeaderIcon="bi-telephone">
    <p>Email: test@example.com</p>
</DetailCard>

@* Card with header actions *@
<DetailCard HeaderTitle="Business Hours" HeaderIcon="bi-clock">
    <HeaderActions>
        <button class="btn btn-sm btn-outline-primary" @onclick="EditHours">
            <i class="bi bi-pencil me-1"></i> Edit
        </button>
    </HeaderActions>
    <ChildContent>
        <p>Mon-Fri: 9 AM - 5 PM</p>
    </ChildContent>
</DetailCard>

@* Card with footer *@
<DetailCard HeaderTitle="Summary">
    <ChildContent>
        <p>Content here</p>
    </ChildContent>
    <FooterContent>
        <button class="btn btn-primary">Save</button>
    </FooterContent>
</DetailCard>
```

---

### StatItem

**Purpose:** Key-value display with optional badge styling.

**File:** `Components/Shared/StatItem.razor`

```razor
<div class="d-flex justify-content-between align-items-center @(ShowBorder ? "mb-3 pb-3 border-bottom" : "")">
    <span class="text-muted">@Label</span>
    @if (UseBadge)
    {
        <span class="badge @BadgeClass fs-6">@Value</span>
    }
    else
    {
        <span class="fw-semibold @ValueClass">@Value</span>
    }
</div>

@code {
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string? Value { get; set; }
    [Parameter] public bool UseBadge { get; set; } = false;
    [Parameter] public string BadgeClass { get; set; } = "bg-primary";
    [Parameter] public string ValueClass { get; set; } = "";
    [Parameter] public bool ShowBorder { get; set; } = true;
}
```

**Usage:**
```razor
@* Basic stat *@
<StatItem Label="Owner" Value="John Doe" />

@* Badge stat *@
<StatItem Label="Status" Value="Active" UseBadge="true" BadgeClass="bg-success" />

@* Last item (no border) *@
<StatItem Label="Created" Value="Jan 1, 2025" ShowBorder="false" />
```

---

## Component Design Principles

### 1. Default Values

```csharp
// Provide sensible defaults for all optional parameters
[Parameter] public string Message { get; set; } = "Loading...";
[Parameter] public bool ShowBorder { get; set; } = true;
```

### 2. EditorRequired for Mandatory Parameters

```csharp
// Compiler warning if not provided
[Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
```

### 3. RenderFragment for Custom Content

```csharp
// Allow custom content injection
[Parameter] public RenderFragment? ChildContent { get; set; }
[Parameter] public RenderFragment? HeaderActions { get; set; }
```

### 4. CSS Class Parameters for Flexibility

```csharp
// Allow styling overrides
[Parameter] public string CardClass { get; set; } = "mb-4";
[Parameter] public string SpinnerClass { get; set; } = "text-primary";
```

### 5. EventCallback for Interactions

```csharp
// Enable parent component to handle events
[Parameter] public EventCallback OnDismiss { get; set; }
```

---

## Complete Page Example

```razor
@page "/restaurants/{Id:int}"
@using SmartMenuOptim.Server.Components.Shared

<PageTitle>Restaurant Details</PageTitle>

<div class="container mt-4">
    @* Loading State *@
    <LoadingSpinner IsLoading="_loading" Message="Loading restaurant..." />

    @* Error State *@
    @if (!_loading && _error is not null)
    {
        <ErrorAlert Message="@_error" 
                    BackLinkUrl="/restaurants" 
                    IsDismissible="true"
                    OnDismiss="DismissError" />
    }

    @* Not Found State *@
    @if (!_loading && _error is null && _restaurant is null)
    {
        <NotFoundAlert Message="Restaurant not found." 
                       BackLinkUrl="/restaurants" />
    }

    @* Success State *@
    @if (!_loading && _restaurant is not null)
    {
        <DetailCard HeaderTitle="Contact Information" HeaderIcon="bi-telephone">
            <StatItem Label="Email" Value="@_restaurant.Email" />
            <StatItem Label="Phone" Value="@_restaurant.PhoneNumber" ShowBorder="false" />
        </DetailCard>

        <DetailCard HeaderTitle="Quick Stats" HeaderIcon="bi-speedometer2">
            <StatItem Label="Max Orders" Value="@_restaurant.MaxOrders.ToString()" UseBadge="true" />
            <StatItem Label="Status" Value="@(_restaurant.IsOpen ? "Open" : "Closed")" 
                      UseBadge="true" 
                      BadgeClass="@(_restaurant.IsOpen ? "bg-success" : "bg-secondary")" 
                      ShowBorder="false" />
        </DetailCard>
    }
</div>
```

---

## Benefits

| Benefit | Description |
|---------|-------------|
| **DRY** | Don't Repeat Yourself - single implementation |
| **Consistency** | Same look and feel across app |
| **Maintainability** | Update component, update everywhere |
| **Readability** | Clean, semantic markup in pages |
| **Testability** | Components can be tested in isolation |
| **Flexibility** | Parameters allow customization |

---

## When to Extract a Component

✅ **Extract When:**
- Pattern appears 3+ times
- Pattern has consistent structure
- Pattern needs consistent styling
- Pattern has complex HTML/CSS

❌ **Don't Extract When:**
- Pattern is truly unique
- Extraction adds more complexity
- Pattern varies significantly each use

---

## Related Patterns

- [Code-Behind Pattern](./CODE_BEHIND_PATTERN.md) - Complex components use code-behind
- [State Container Pattern](./STATE_CONTAINER_PATTERN.md) - Page components using shared components

---

## References

- [ASP.NET Core Blazor Components](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/)
- [Blazor Component Parameters](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/#component-parameters)

---

*Document Version: 1.0*  
*Last Updated: 2025-03-01*
