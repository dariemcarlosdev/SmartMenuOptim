# MenuCompositionValidatorService - Usage Examples

## Quick Start

```csharp
using SmartMenuOptim.Domain.Services;
using SmartMenuOptim.Domain.Aggregates.MenuAggregate;

// Create the validator (stateless, can be reused)
var validator = new MenuCompositionValidatorService();

// Validate a menu
var result = validator.ValidateMenuComposition(myMenu);

if (result.IsValid)
{
    Console.WriteLine("✅ Menu is valid!");
}
else
{
    Console.WriteLine($"❌ {result.Summary}");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

## Scenario 1: Publishing a Menu

```csharp
public class MenuManagementService
{
    private readonly IMenuRepository _menuRepository;
    private readonly MenuCompositionValidatorService _validator;
    
    public async Task<Result<bool>> PublishMenuAsync(int menuId)
    {
        // 1. Load menu with all dishes
        var menu = await _menuRepository.GetByIdWithDishesAsync(menuId);
        
        if (menu == null)
            return Result<bool>.Failure("Menu not found");
        
        // 2. Validate composition
        var validationResult = _validator.ValidateMenuComposition(menu);
        
        if (!validationResult.IsValid)
        {
            return Result<bool>.Failure(
                "Cannot publish menu - validation failed",
                validationResult.Errors.ToArray()
            );
        }
        
        // 3. Log warnings (menu is valid but could be improved)
        if (validationResult.Warnings.Any())
        {
            _logger.LogWarning("Menu {MenuId} published with warnings: {Warnings}",
                menuId, string.Join("; ", validationResult.Warnings));
        }
        
        // 4. Publish menu
        menu.MakeAvailable();
        await _menuRepository.UpdateAsync(menu);
        
        return Result<bool>.Success(true);
    }
}
```

## Scenario 2: Real-Time Validation (Blazor Component)

```razor
@page "/menus/{menuId:int}/edit"
@inject MenuManagementService MenuService
@inject MenuCompositionValidatorService Validator

<h3>Edit Menu</h3>

@if (validationResult != null && !validationResult.IsValid)
{
    <div class="alert alert-danger">
        <h4>❌ Menu Validation Errors:</h4>
        <ul>
            @foreach (var error in validationResult.Errors)
            {
                <li>@error</li>
            }
        </ul>
    </div>
}

@if (validationResult?.Warnings.Any() == true)
{
    <div class="alert alert-warning">
        <h4>⚠️ Menu Warnings:</h4>
        <ul>
            @foreach (var warning in validationResult.Warnings)
            {
                <li>@warning</li>
            }
        </ul>
    </div>
}

<button @onclick="ValidateMenu" class="btn btn-primary">Validate Menu</button>
<button @onclick="PublishMenu" class="btn btn-success" disabled="@(!IsValidForPublish)">
    Publish Menu
</button>

@code {
    [Parameter] public int MenuId { get; set; }
    
    private Menu? menu;
    private MenuValidationResult? validationResult;
    
    private bool IsValidForPublish => validationResult?.IsValid == true;
    
    protected override async Task OnInitializedAsync()
    {
        menu = await MenuService.GetMenuByIdAsync(MenuId);
        
        // Auto-validate on load
        if (menu != null)
        {
            validationResult = Validator.ValidateMenuComposition(menu);
        }
    }
    
    private void ValidateMenu()
    {
        if (menu != null)
        {
            validationResult = Validator.ValidateMenuComposition(menu);
            StateHasChanged();
        }
    }
    
    private async Task PublishMenu()
    {
        if (!IsValidForPublish)
        {
            return;
        }
        
        var result = await MenuService.PublishMenuAsync(MenuId);
        
        if (result.IsSuccess)
        {
            // Navigate to menu list or show success message
            NavigationManager.NavigateTo("/menus");
        }
    }
}
```

## Scenario 3: API Endpoint Validation

```csharp
[ApiController]
[Route("api/[controller]")]
public class MenusController : ControllerBase
{
    private readonly IMenuRepository _menuRepository;
    private readonly MenuCompositionValidatorService _validator;
    
    [HttpPost("{id}/validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuValidationResponse>> ValidateMenu(int id)
    {
        var menu = await _menuRepository.GetByIdWithDishesAsync(id);
        
        if (menu == null)
            return NotFound();
        
        var result = _validator.ValidateMenuComposition(menu);
        
        return Ok(new MenuValidationResponse
        {
            IsValid = result.IsValid,
            Summary = result.Summary,
            Errors = result.Errors.ToArray(),
            Warnings = result.Warnings.ToArray()
        });
    }
    
    [HttpPost("{id}/publish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishMenu(int id)
    {
        var menu = await _menuRepository.GetByIdWithDishesAsync(id);
        
        if (menu == null)
            return NotFound();
        
        // Validate before publishing
        var validationResult = _validator.ValidateMenuComposition(menu);
        
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                message = "Menu validation failed",
                errors = validationResult.Errors,
                warnings = validationResult.Warnings
            });
        }
        
        menu.MakeAvailable();
        await _menuRepository.UpdateAsync(menu);
        
        return Ok(new { message = "Menu published successfully" });
    }
}
```

## Scenario 4: Quick Validation Checks

```csharp
// Quick variety check before adding dishes
public async Task<bool> CanRemoveDish(Menu menu, int dishId)
{
    // Temporarily remove dish to check
    var dishToRemove = menu.MenuDishes.FirstOrDefault(md => md.DishId == dishId);
    if (dishToRemove == null)
        return false;
    
    // Create a test menu state
    var testMenu = menu.Clone(); // Assume cloning exists
    testMenu.RemoveDish(dishId);
    
    // Check if menu still has adequate variety
    return _validator.HasAdequateVariety(testMenu);
}

// Quick price check when updating prices
public bool IsAddingPriceDiversity(Menu menu, Dish newDish)
{
    var currentPriceBalance = _validator.HasBalancedPricePoints(menu);
    
    if (currentPriceBalance)
        return false; // Already balanced
    
    // Add dish temporarily
    var testMenu = menu.Clone();
    testMenu.AddDish(newDish);
    
    var newPriceBalance = _validator.HasBalancedPricePoints(testMenu);
    
    return !currentPriceBalance && newPriceBalance; // Improved balance
}
```

## Scenario 5: Background Job Validation

```csharp
public class MenuQualityReportJob : BackgroundService
{
    private readonly IMenuRepository _menuRepository;
    private readonly MenuCompositionValidatorService _validator;
    private readonly IEmailService _emailService;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await GenerateDailyQualityReport();
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
    
    private async Task GenerateDailyQualityReport()
    {
        var allActiveMenus = await _menuRepository.GetActiveMenusAsync();
        
        var report = new StringBuilder();
        report.AppendLine("Daily Menu Quality Report");
        report.AppendLine("========================");
        report.AppendLine();
        
        foreach (var menu in allActiveMenus)
        {
            var result = _validator.ValidateMenuComposition(menu);
            
            if (!result.IsValid)
            {
                report.AppendLine($"❌ {menu.Name} (ID: {menu.Id})");
                foreach (var error in result.Errors)
                {
                    report.AppendLine($"   - {error}");
                }
            }
            else if (result.Warnings.Any())
            {
                report.AppendLine($"⚠️  {menu.Name} (ID: {menu.Id})");
                foreach (var warning in result.Warnings)
                {
                    report.AppendLine($"   - {warning}");
                }
            }
            else
            {
                report.AppendLine($"✅ {menu.Name} (ID: {menu.Id})");
            }
            
            report.AppendLine();
        }
        
        await _emailService.SendToAdminAsync(
            "Daily Menu Quality Report",
            report.ToString()
        );
    }
}
```

## Scenario 6: Validation During Menu Creation Wizard

```csharp
public class MenuCreationWizard
{
    private readonly MenuCompositionValidatorService _validator;
    private Menu _draftMenu;
    
    public void InitializeNewMenu(string name, int menuTypeId)
    {
        _draftMenu = new Menu(
            restaurantId: CurrentRestaurantId,
            name: name,
            menuTypeId: menuTypeId
        );
    }
    
    public MenuValidationResult AddDishAndValidate(Dish dish)
    {
        _draftMenu.AddDish(dish);
        
        // Validate after each addition
        return _validator.ValidateMenuComposition(_draftMenu);
    }
    
    public bool CanProceedToNextStep()
    {
        var result = _validator.ValidateMenuComposition(_draftMenu);
        
        // Must be valid to proceed
        return result.IsValid;
    }
    
    public string GetNextStepGuidance()
    {
        var result = _validator.ValidateMenuComposition(_draftMenu);
        
        if (!result.IsValid)
        {
            return $"Please fix these issues: {string.Join(", ", result.Errors)}";
        }
        
        if (result.Warnings.Any())
        {
            return $"Suggestions: {string.Join(", ", result.Warnings)}";
        }
        
        return "Menu looks good! You can proceed to publish.";
    }
}
```

## Scenario 7: Validation with Custom Business Rules

```csharp
// Extend for restaurant-specific rules
public class RestaurantMenuValidator
{
    private readonly MenuCompositionValidatorService _baseValidator;
    private readonly RestaurantConfiguration _config;
    
    public MenuValidationResult ValidateForRestaurant(Menu menu)
    {
        // Start with base validation
        var result = _baseValidator.ValidateMenuComposition(menu);
        
        // Add restaurant-specific rules
        var errors = result.Errors.ToList();
        var warnings = result.Warnings.ToList();
        
        // Restaurant requires gluten-free options
        if (_config.RequiresGlutenFreeOptions)
        {
            var hasGlutenFree = menu.MenuDishes
                .Any(md => md.Dish.IsGlutenFree);
            
            if (!hasGlutenFree)
            {
                warnings.Add("Restaurant policy requires at least one gluten-free option");
            }
        }
        
        // Restaurant has minimum revenue target per menu
        if (_config.MinimumAveragePrice.HasValue)
        {
            var avgPrice = menu.MenuDishes
                .Average(md => md.SpecialPrice ?? md.Dish.DishPrice);
            
            if (avgPrice < _config.MinimumAveragePrice.Value)
            {
                warnings.Add($"Average menu price (${avgPrice:F2}) is below target (${_config.MinimumAveragePrice.Value:F2})");
            }
        }
        
        return errors.Any()
            ? MenuValidationResult.Failure(errors, warnings)
            : MenuValidationResult.Success(warnings);
    }
}
```

## Dependency Injection Setup

```csharp
// In ServiceCollectionExtensions.cs or Startup.cs
public static IServiceCollection AddDomainServices(this IServiceCollection services)
{
    // Register as singleton (stateless service)
    services.AddSingleton<MenuCompositionValidatorService>();
    
    // Or register as scoped if you add dependencies later
    // services.AddScoped<MenuCompositionValidatorService>();
    
    return services;
}
```

## Testing Example

```csharp
[Fact]
public void ValidateMenuComposition_RealWorldScenario_ValidatesDinnerMenu()
{
    // Arrange
    var validator = new MenuCompositionValidatorService();
    var dinnerMenu = CreateDinnerMenu();
    
    // Act
    var result = validator.ValidateMenuComposition(dinnerMenu);
    
    // Assert
    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
    Assert.Contains(result.Summary, "valid");
}

private Menu CreateDinnerMenu()
{
    var menu = new Menu(1, "Dinner Menu", 1, "Evening dining");
    
    // Appetizers
    menu.AddDish(CreateDish("Caesar Salad", 8.99m, "Appetizers"));
    menu.AddDish(CreateDish("Bruschetta", 7.99m, "Appetizers"));
    
    // Main Course
    menu.AddDish(CreateDish("Grilled Salmon", 24.99m, "Main Course"));
    menu.AddDish(CreateDish("Ribeye Steak", 34.99m, "Main Course"));
    menu.AddDish(CreateDish("Pasta Primavera", 16.99m, "Main Course"));
    
    // Desserts
    menu.AddDish(CreateDish("Tiramisu", 9.99m, "Desserts"));
    menu.AddDish(CreateDish("Chocolate Lava Cake", 8.99m, "Desserts"));
    
    return menu;
}
```

## Error Handling

```csharp
public async Task<IActionResult> ValidateAndPublishMenu(int menuId)
{
    try
    {
        var menu = await _menuRepository.GetByIdAsync(menuId);
        
        if (menu == null)
            return NotFound($"Menu {menuId} not found");
        
        var result = _validator.ValidateMenuComposition(menu);
        
        if (!result.IsValid)
        {
            _logger.LogWarning(
                "Menu {MenuId} validation failed: {Errors}",
                menuId,
                string.Join("; ", result.Errors)
            );
            
            return BadRequest(new
            {
                success = false,
                message = result.Summary,
                errors = result.Errors,
                warnings = result.Warnings
            });
        }
        
        menu.MakeAvailable();
        await _menuRepository.UpdateAsync(menu);
        
        _logger.LogInformation("Menu {MenuId} published successfully", menuId);
        
        return Ok(new
        {
            success = true,
            message = "Menu published successfully",
            warnings = result.Warnings
        });
    }
    catch (ArgumentNullException ex)
    {
        _logger.LogError(ex, "Null argument in menu validation");
        return BadRequest("Invalid menu data");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error validating menu {MenuId}", menuId);
        return StatusCode(500, "An error occurred while validating the menu");
    }
}
```

## Best Practices

1. **Always validate before publishing**
   ```csharp
   var result = _validator.ValidateMenuComposition(menu);
   if (result.IsValid)
   {
       menu.MakeAvailable();
   }
   ```

2. **Show warnings to users**
   ```csharp
   if (result.Warnings.Any())
   {
       ShowWarningNotification(result.Warnings);
   }
   ```

3. **Log validation failures**
   ```csharp
   if (!result.IsValid)
   {
       _logger.LogWarning("Menu validation failed: {Errors}", result.Errors);
   }
   ```

4. **Use in background jobs for quality monitoring**
   ```csharp
   var invalidMenus = allMenus
       .Where(m => !_validator.ValidateMenuComposition(m).IsValid)
       .ToList();
   ```

5. **Provide real-time feedback in UI**
   ```csharp
   @if (!validationResult.IsValid)
   {
       <span class="text-danger">@validationResult.Summary</span>
   }
   ```
