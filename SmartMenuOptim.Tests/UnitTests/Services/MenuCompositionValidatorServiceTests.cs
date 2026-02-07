using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.MenuAggregate;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Services;
using SmartMenuOptim.Domain.ValueObjects;
using Xunit;

namespace SmartMenuOptim.Tests.UnitTests.Services;

/// <summary>
/// Unit tests for MenuCompositionValidatorService domain service.
/// Tests all business rules and validation logic.
/// </summary>
public class MenuCompositionValidatorServiceTests
{
    private readonly MenuCompositionValidatorService _validator;

    public MenuCompositionValidatorServiceTests()
    {
        _validator = new MenuCompositionValidatorService();
    }

    #region Constructor and Null Validation Tests

    [Fact]
    public void ValidateMenuComposition_NullMenu_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _validator.ValidateMenuComposition(null!));
    }

    [Fact]
    public void HasAdequateVariety_NullMenu_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _validator.HasAdequateVariety(null!));
    }

    [Fact]
    public void HasBalancedPricePoints_NullMenu_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _validator.HasBalancedPricePoints(null!));
    }

    #endregion

    #region ValidateMenuComposition Tests

    [Fact]
    public void ValidateMenuComposition_ValidMenu_ReturnsSuccess()
    {
        // Arrange
        var menu = CreateValidMenu();

        // Act
        var result = _validator.ValidateMenuComposition(menu);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateMenuComposition_DeletedMenu_ReturnsFailure()
    {
        // Arrange
        var menu = CreateValidMenu();
        // Use reflection to set IsDeleted
        typeof(Menu).BaseType?.GetProperty("IsDeleted")?.SetValue(menu, true);

        // Act
        var result = _validator.ValidateMenuComposition(menu);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("deleted menu"));
    }

    [Fact]
    public void ValidateMenuComposition_NoActiveDishes_ReturnsFailure()
    {
        // Arrange
        var menu = CreateMenuWithoutDishes();

        // Act
        var result = _validator.ValidateMenuComposition(menu);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("at least one dish"));
    }

    [Fact]
    public void ValidateMenuComposition_InsufficientVariety_ReturnsFailure()
    {
        // Arrange
        var menu = CreateMenuWithLimitedDishes(2); // Only 2 dishes

        // Act
        var result = _validator.ValidateMenuComposition(menu);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("at least 3 active dishes"));
    }

    [Fact]
    public void ValidateMenuComposition_DuplicateDishes_ReturnsFailure()
    {
        // Arrange
        var menu = CreateMenuWithDuplicateDishes();

        // Act
        var result = _validator.ValidateMenuComposition(menu);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("appears") && e.Contains("times"));
    }

    [Fact]
    public void ValidateMenuComposition_SinglePriceLevel_ReturnsFailure()
    {
        // Arrange
        var menu = CreateMenuWithSinglePriceLevel();

        // Act
        var result = _validator.ValidateMenuComposition(menu);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("price levels"));
    }

    [Fact]
    public void ValidateMenuComposition_CategoryDominance_ReturnsFailure()
    {
        // Arrange
        var menu = CreateMenuWithDominantCategory();

        // Act
        var result = _validator.ValidateMenuComposition(menu);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("dominates the menu"));
    }

    [Fact]
    public void ValidateMenuComposition_LimitedVariety_ReturnsWarning()
    {
        // Arrange
        var menu = CreateMenuWithLimitedDishes(4); // 4 dishes - valid but limited

        // Act
        var result = _validator.ValidateMenuComposition(menu);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("limited variety"));
    }

    #endregion

    #region HasAdequateVariety Tests

    [Fact]
    public void HasAdequateVariety_SufficientDishes_ReturnsTrue()
    {
        // Arrange
        var menu = CreateMenuWithLimitedDishes(5);

        // Act
        var result = _validator.HasAdequateVariety(menu);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasAdequateVariety_InsufficientDishes_ReturnsFalse()
    {
        // Arrange
        var menu = CreateMenuWithLimitedDishes(2);

        // Act
        var result = _validator.HasAdequateVariety(menu);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasAdequateVariety_ExactlyMinimum_ReturnsTrue()
    {
        // Arrange
        var menu = CreateMenuWithLimitedDishes(3);

        // Act
        var result = _validator.HasAdequateVariety(menu);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region HasBalancedPricePoints Tests

    [Fact]
    public void HasBalancedPricePoints_DiversePrices_ReturnsTrue()
    {
        // Arrange
        var menu = CreateMenuWithDiversePrices();

        // Act
        var result = _validator.HasBalancedPricePoints(menu);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasBalancedPricePoints_SinglePriceLevel_ReturnsFalse()
    {
        // Arrange
        var menu = CreateMenuWithSinglePriceLevel();

        // Act
        var result = _validator.HasBalancedPricePoints(menu);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasBalancedPricePoints_InsufficientDishes_ReturnsFalse()
    {
        // Arrange
        var menu = CreateMenuWithLimitedDishes(2);

        // Act
        var result = _validator.HasBalancedPricePoints(menu);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Helper Methods

    private Menu CreateValidMenu()
    {
        var menu = new Menu(
            restaurantId: 1,
            name: "Dinner Menu",
            menuTypeId: 1,
            description: "Evening dining options"
        );

        var category1 = CreateCategory(1, "Appetizers", "Starters", 1);
        var category2 = CreateCategory(2, "Main Course", "Entrees", 2);
        var category3 = CreateCategory(3, "Desserts", "Sweet endings", 3);

        // Add diverse dishes with different categories and prices
        AddDishToMenu(menu, 1, "Caesar Salad", 8.99m, category1);
        AddDishToMenu(menu, 2, "Grilled Salmon", 24.99m, category2);
        AddDishToMenu(menu, 3, "Ribeye Steak", 34.99m, category2);
        AddDishToMenu(menu, 4, "Pasta Primavera", 16.99m, category2);
        AddDishToMenu(menu, 5, "Chocolate Cake", 7.99m, category3);

        return menu;
    }

    private Menu CreateMenuWithoutDishes()
    {
        return new Menu(
            restaurantId: 1,
            name: "Empty Menu",
            menuTypeId: 1,
            description: "No dishes"
        );
    }

    private Menu CreateMenuWithLimitedDishes(int count)
    {
        var menu = new Menu(
            restaurantId: 1,
            name: "Limited Menu",
            menuTypeId: 1,
            description: "Few dishes"
        );

        var category = CreateCategory(1, "Main Course", "Entrees", 1);

        for (int i = 1; i <= count; i++)
        {
            AddDishToMenu(menu, i, $"Dish {i}", 10.00m + (i * 5), category);
        }

        return menu;
    }

    private Menu CreateMenuWithDuplicateDishes()
    {
        var menu = new Menu(
            restaurantId: 1,
            name: "Menu with Duplicates",
            menuTypeId: 1,
            description: "Has duplicate dishes"
        );

        var category = CreateCategory(1, "Main Course", "Entrees", 1);
        var dish1 = CreateDish(1, "Salmon", 20.00m, category);
        var dish2 = CreateDish(2, "Steak", 30.00m, category);

        // Add dish1 twice (duplicate)
        menu.AddDish(dish1, 1, null, null);
        menu.AddDish(dish1, 2, null, null);
        menu.AddDish(dish2, 3, null, null);

        // Manually set the dish references since we're not using repository
        foreach (var md in menu.MenuDishes)
        {
            md.Dish = md.DishId == 1 ? dish1 : dish2;
        }

        return menu;
    }

    private Menu CreateMenuWithSinglePriceLevel()
    {
        var menu = new Menu(
            restaurantId: 1,
            name: "Uniform Pricing Menu",
            menuTypeId: 1,
            description: "All same price"
        );

        var category = CreateCategory(1, "Main Course", "Entrees", 1);

        // All dishes within 10% price range (same level)
        AddDishToMenu(menu, 1, "Dish 1", 20.00m, category);
        AddDishToMenu(menu, 2, "Dish 2", 20.50m, category);
        AddDishToMenu(menu, 3, "Dish 3", 21.00m, category);
        AddDishToMenu(menu, 4, "Dish 4", 21.50m, category);

        return menu;
    }

    private Menu CreateMenuWithDiversePrices()
    {
        var menu = new Menu(
            restaurantId: 1,
            name: "Diverse Pricing Menu",
            menuTypeId: 1,
            description: "Multiple price levels"
        );

        var category = CreateCategory(1, "Main Course", "Entrees", 1);

        // Distinct price levels
        AddDishToMenu(menu, 1, "Budget Dish", 10.00m, category);
        AddDishToMenu(menu, 2, "Mid-range Dish", 20.00m, category);
        AddDishToMenu(menu, 3, "Premium Dish", 40.00m, category);

        return menu;
    }

    private Menu CreateMenuWithDominantCategory()
    {
        var menu = new Menu(
            restaurantId: 1,
            name: "Unbalanced Menu",
            menuTypeId: 1,
            description: "One category dominates"
        );

        var mainCourse = CreateCategory(1, "Main Course", "Entrees", 1);
        var desserts = CreateCategory(2, "Desserts", "Sweets", 2);

        // 8 main courses (80% dominance)
        for (int i = 1; i <= 8; i++)
        {
            AddDishToMenu(menu, i, $"Main {i}", 10.00m + i, mainCourse);
        }

        // Only 2 desserts (20%)
        AddDishToMenu(menu, 9, "Dessert 1", 8.00m, desserts);
        AddDishToMenu(menu, 10, "Dessert 2", 9.00m, desserts);

        return menu;
    }

    private void AddDishToMenu(Menu menu, int dishId, string name, decimal price, DishCategory category)
    {
        var dish = CreateDish(dishId, name, price, category);
        menu.AddDish(dish, menu.MenuDishes.Count + 1, null, null);

        // Manually set the dish reference and restaurant ID
        var menuDish = menu.MenuDishes.Last();
        menuDish.RestaurantId = menu.RestaurantId;
    }

    private Dish CreateDish(int id, string name, decimal price, DishCategory category)
    {
        var dish = new Dish
        {
            Name = new DishName(name),
            Description = $"{name} description",
            DishPrice = price,
            CategoryId = category.Id,
            RestaurantId = 1,
            IsActive = true,
            PreparationTime = 15,
            Calories = 500
        };

        // Use reflection to set the Id since it's likely private set
        typeof(Dish).GetProperty("Id")?.SetValue(dish, id);
        
        // Set the category reference
        dish.Category = category;

        return dish;
    }

    private DishCategory CreateCategory(int id, string name, string description, int displayOrder)
    {
        var category = new DishCategory(1, name, description, displayOrder);
        
        // Use reflection to set the Id
        typeof(DishCategory).BaseType?.BaseType?.GetProperty("Id")?.SetValue(category, id);
        
        return category;
    }

    #endregion
}
