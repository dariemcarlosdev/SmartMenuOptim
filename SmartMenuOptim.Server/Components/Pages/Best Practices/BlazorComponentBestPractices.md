# Blazor Component Best Practices

This document provides guidelines for structuring Blazor components to improve readability and maintainability for all contributors.

---

## 1. Component Decomposition
- Break large components into smaller, focused child components.
- Each component should have a single responsibility (e.g., `RestaurantCard`, `CategoryTable`, `SummaryCard`).

## 2. Consistent Naming
- Use clear, descriptive names for components, parameters, and methods.
- Use PascalCase for component names and camelCase for parameters.

---

## Best Practices for Naming Components and Parameters in Blazor

### Component Naming
- Use **PascalCase** for component names (e.g., `OrderSummary`, `RestaurantCard`).
- Name components based on their responsibility or UI role.
- Avoid generic names like `MyComponent` or `TestComponent`.
- Use suffixes like `Card`, `Table`, `List`, or `Dialog` to clarify the component's purpose (e.g., `CategoryTable`, `ReviewDialog`).

### Parameter Naming
- Use **camelCase** for parameter names (e.g., `restaurantName`, `totalSales`).
- Name parameters descriptively to indicate their purpose.
- Prefix boolean parameters with `is` or `has` (e.g., `isExpanded`, `hasError`).
- For event callbacks, use the `On` prefix (e.g., `OnClick`, `OnToggle`, `OnDishSelected`).
- Avoid abbreviations unless they are widely understood.

### General Tips
- Keep names concise but meaningful.
- Use plural names for collections (e.g., `dishes`, `categories`).
- For parameters representing UI fragments, use the `RenderFragment` type and name them with a `Fragment` suffix (e.g., `iconFragment`).

---

## 3. Separation of Concerns
- Keep UI markup and business logic separate.
- Use code-behind files (`.razor.cs`) or partial classes for complex logic.

## 4. Parameter Usage
- Use `[Parameter]` for data passed from parent to child components.
- Prefer `EventCallback` for event handling between components.

## 5. Reusable Styles
- Use CSS isolation (`.razor.css`) for component-specific styles.
- Avoid inline styles when possible.

## 6. Avoid Inline Logic in Markup
- Move complex expressions or logic to properties or methods in the `@code` block.

## 7. Use Services for Data Access
- Inject services for data retrieval and business logic.
- Avoid direct data access in the UI layer.

## 8. Handle State and Loading Gracefully
- Use loading indicators and error messages for async operations.
- Keep state management simple; use state containers for shared state if needed.

## 9. Accessibility
- Use semantic HTML and ARIA attributes for better accessibility.
- Ensure interactive elements are keyboard accessible.

## 10. Documentation and Comments
- Add XML comments to public properties and methods.
- Use inline comments sparingly to clarify non-obvious logic.

---

## Example Components Based on These Best Practices

Below are examples of components that could be created to improve structure and maintainability in your Blazor project:

### 1. `SummaryCard.razor`
Displays a summary metric (e.g., total sales, total dishes sold).

**Parameters:**
- `title` (string)
- `value` (string or number)
- `iconFragment` (RenderFragment)
- `description` (string)
- `colorClass` (string)

### 2. `RestaurantDashboardCard.razor`
Represents a restaurant group in the dashboard, with expand/collapse functionality.

**Parameters:**
- `restaurantName` (string)
- `totalSales` (decimal)
- `avgRating` (double)
- `isExpanded` (bool)
- `onToggle` (EventCallback)
- `categories` (IEnumerable<CategoryGroupDTO>)

### 3. `CategoryTable.razor`
Displays a table of dishes for a specific category.

**Parameters:**
- `categoryName` (string)
- `dishes` (IEnumerable<DishDTO>)
- `totalSales` (decimal)
- `avgRating` (double)

### 4. `DishRow.razor`
Represents a single dish row in a category table.

**Parameters:**
- `dish` (DishDTO)
- `onDishClicked` (EventCallback<string>)

### 5. `LoadingIndicator.razor`
Reusable loading spinner for async operations.

**Parameters:**
- `message` (string)

### 6. `AlertMessage.razor`
Reusable alert for info, warning, or error messages.

**Parameters:**
- `type` (string: "info", "warning", "error")
- `message` (string)

---



