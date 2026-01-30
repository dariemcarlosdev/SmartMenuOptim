using SmartMenuOptim.Domain.Aggregates.MenuAggregate;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartMenuOptim.Domain.Aggregates.DishAggregate    
{
    /// <summary>
    /// Represents a dish aggregate root managing menu item information, pricing, nutritional data, and menu associations for a restaurant tenant.
    /// </summary>
    /// <remarks>
    /// <para><strong>3-TIER DDD STRATEGY: Tier 1 - Full Aggregate Roots (Rich DDD)</strong></para>
    /// <para>This class implements a full DDD aggregate root pattern managing many-to-many relationships through MenuDish join entities.
    /// It serves as the consistency boundary for dish information and its associations with multiple menus.</para>
    /// 
    /// <para><strong>Tier 1 Characteristics:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Full Encapsulation:</strong> Public setters allow direct property access (pragmatic approach for menu item management)</description></item>
    ///   <item><description><strong>Many-to-Many Management:</strong> Manages associations with Menu through MenuDish join entities</description></item>
    ///   <item><description><strong>Aggregate Boundary:</strong> Defines consistency boundary for dish data and menu relationships</description></item>
    ///   <item><description><strong>Rich Domain Behavior:</strong> Implements IValidatableObject for complex business rule validation</description></item>
    ///   <item><description><strong>Invariant Protection:</strong> Validates pricing, category relationships, menu associations, and tenant consistency</description></item>
    ///   <item><description><strong>Collection Management:</strong> Manages MenuDishes, Reviews, SaleRecords, and OrderItems collections</description></item>
    ///   <item><description><strong>Cross-Aggregate References:</strong> Referenced by multiple aggregates (Menu, Order) while maintaining tenant isolation</description></item>
    /// </list>
    /// 
    /// <para><strong>Entity Overview:</strong></para>
    /// <para>A Dish represents a menu item offered by a restaurant with complete nutritional information, pricing, categorization,
    /// and multi-menu associations. Each dish can appear on multiple menus with optional special pricing, custom ordering, and
    /// menu-specific notes. Dishes track customer reviews, sales history, and order frequency for analytics and menu optimization.</para>
    /// 
    /// <para><strong>Multi-Tenant Support:</strong></para>
    /// <para>Inherits from TenantEntityBase to provide built-in multi-tenancy support. Each dish is scoped to a specific
    /// restaurant (RestaurantId). All menu associations, reviews, and sales must belong to the same restaurant to maintain
    /// tenant isolation and data security.</para>
    /// 
    /// <para><strong>Aggregate Composition:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Root Entity:</strong> Dish (this class)</description></item>
    ///   <item><description><strong>Join Entities:</strong> MenuDish collection - many-to-many with Menu (special pricing, display order)</description></item>
    ///   <item><description><strong>Related Collections:</strong> Reviews, SaleRecords, OrderItems (tracked for analytics)</description></item>
    ///   <item><description><strong>Referenced Entities:</strong> Category (required), Menus (many-to-many through MenuDish)</description></item>
    /// </list>
    /// 
    /// <para><strong>Consistency Boundary:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Invariants Protected:</strong> Positive price, valid category, menu associations within tenant, at least one active menu, reasonable special pricing</description></item>
    ///   <item><description><strong>Validation Logic:</strong> Cross-entity validation via IValidatableObject (menu tenant consistency, price reasonableness)</description></item>
    ///   <item><description><strong>Business Rules:</strong> Must belong to active category, special prices limited to 5x base price, active dishes require menu assignment</description></item>
    ///   <item><description><strong>Tenant Isolation:</strong> All menus, reviews, sales, and orders must belong to same restaurant</description></item>
    /// </list>
    /// 
    /// <para><strong>Domain Features:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Identity:</strong> Inherits entity identity from TenantEntityBase (Id property from EntityBase)</description></item>
    ///   <item><description><strong>Automatic Timestamps:</strong> CreatedAt, UpdatedAt automatically managed through EntityBase</description></item>
    ///   <item><description><strong>Soft Delete Support:</strong> Inherits IsDeleted flag for logical deletion (archived dishes)</description></item>
    ///   <item><description><strong>Optimistic Concurrency:</strong> Uses xmin timestamp token from EntityBase for concurrency control</description></item>
    ///   <item><description><strong>Nutritional Tracking:</strong> Calories, vegetarian/spicy flags, ingredients, allergen information</description></item>
    ///   <item><description><strong>Pricing Flexibility:</strong> Base price with menu-specific overrides via MenuDish</description></item>
    ///   <item><description><strong>Category Classification:</strong> Required category assignment for menu organization</description></item>
    ///   <item><description><strong>Multi-Menu Support:</strong> Can appear on multiple menus with different pricing/ordering</description></item>
    /// </list>
    /// 
    /// <para><strong>Relationships:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Category (Required):</strong> Every dish must belong to a category for menu organization</description></item>
    ///   <item><description><strong>MenuDish (Many-to-Many Join):</strong> Links to multiple menus with additional metadata</description></item>
    ///   <item><description><strong>Menus (Many-to-Many):</strong> Convenient navigation through MenuDish join entity</description></item>
    ///   <item><description><strong>Reviews (One-to-Many):</strong> Customer feedback collection for quality tracking</description></item>
    ///   <item><description><strong>SaleRecords (One-to-Many):</strong> Historical sales data for analytics</description></item>
    ///   <item><description><strong>OrderItems (One-to-Many):</strong> Line items in customer orders</description></item>
    ///   <item><description><strong>Restaurant (Required):</strong> Inherited from TenantEntityBase, ensures tenant isolation</description></item>
    /// </list>
    /// 
    /// <para><strong>Lifecycle States:</strong></para>
    /// <code>
    /// Draft → Published → Active ⇄ Inactive → Archived
    ///   ↓
    /// Seasonal ⇄ Regular (Optional classification)
    /// 
    /// 1. Draft: Newly created, not yet on any menu (IsActive = false, no MenuDish associations)
    /// 2. Published: Added to at least one menu (IsActive = false, has MenuDish associations)
    /// 3. Active: Available for ordering (IsActive = true, on active menus)
    /// 4. Inactive: Temporarily unavailable (IsActive = false, kept on menus)
    /// 5. Archived: Permanently removed (IsDeleted = true, soft-deleted)
    /// 
    /// Special States:
    /// - Seasonal: Active only during specific periods (controlled by menu availability)
    /// - Regular: Available year-round on standard menus
    /// 
    /// Note: Lifecycle managed through IsActive flag and menu associations.
    /// Dish must be on at least one active menu to accept orders.
    /// </code>
    /// 
    /// <para><strong>Example Usage:</strong></para>
    /// <code>
    /// // Creating a new dish
    /// var dish = new Dish
    /// {
    ///     RestaurantId = 123,
    ///     Name = "Margherita Pizza",
    ///     Description = "Classic Italian pizza with fresh mozzarella and basil",
    ///     DishPrice = 12.99m,
    ///     CategoryId = appetizersCategory.Id,
    ///     PreparationTime = 15, // minutes
    ///     Calories = 850,
    ///     IsVegetarian = true,
    ///     IsSpicy = false,
    ///     Ingredients = "Pizza dough, tomato sauce, mozzarella, basil, olive oil",
    ///     Allergens = "Gluten, Dairy",
    ///     IsActive = true
    /// };
    /// 
    /// // Validate dish (IValidatableObject)
    /// var validationResults = new List&lt;ValidationResult&gt;();
    /// var isValid = Validator.TryValidateObject(dish, new ValidationContext(dish), validationResults, true);
    /// 
    /// // Add to menus via Menu aggregate
    /// dinnerMenu.AddDish(dish, displayOrder: 1);
    /// lunchMenu.AddDish(dish, displayOrder: 5, specialPrice: 10.99m); // Lunch special
    /// 
    /// // Query menu associations with pricing
    /// var menuAssignments = dish.MenuDishes
    ///     .Where(md => md.IsActive)
    ///     .Select(md => new {
    ///         Menu = md.Menu,
    ///         EffectivePrice = md.SpecialPrice ?? dish.DishPrice,
    ///         DisplayOrder = md.DisplayOrder,
    ///         Notes = md.Notes
    ///     });
    /// 
    /// // Check reviews and ratings
    /// var averageRating = dish.Reviews
    ///     .Where(r => !r.IsDeleted)
    ///     .Average(r => r.Rating);
    /// 
    /// // Analyze sales performance
    /// var totalSales = dish.SaleRecords
    ///     .Where(sr => sr.SaleDate >= DateTime.UtcNow.AddMonths(-1))
    ///     .Sum(sr => sr.QuantitySold);
    /// 
    /// // Validate tenant consistency
    /// dish.ValidateTenantConsistency();
    /// </code>
    /// 
    /// <para><strong>Entity Framework Core Support:</strong></para>
    /// <para>Uses public parameterless constructor (implicit in C# for entities). The many-to-many relationship with Menu
    /// is managed through MenuDish join entity. Collections are initialized to empty lists to prevent null reference issues.
    /// Navigation properties use virtual keyword for lazy loading support.</para>
    /// 
    /// <para><strong>Design Considerations:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Public Setters:</strong> Pragmatic approach for dish management, relies on validation framework</description></item>
    ///   <item><description><strong>Price Validation:</strong> Base price must be positive, special prices limited to 5x markup</description></item>
    ///   <item><description><strong>Menu Requirement:</strong> Active dishes should be on at least one active menu</description></item>
    ///   <item><description><strong>Category Required:</strong> Must belong to valid category for organization</description></item>
    ///   <item><description><strong>Nutritional Data:</strong> Optional but recommended for customer information</description></item>
    ///   <item><description><strong>Allergen Tracking:</strong> Critical for customer safety and dietary restrictions</description></item>
    ///   <item><description><strong>Multi-Menu Pricing:</strong> Same dish can have different prices on different menus</description></item>
    ///   <item><description><strong>Tenant Isolation:</strong> Strict validation prevents cross-tenant data leakage</description></item>
    /// </list>
    /// 
    /// <para><strong>Indexing Strategy:</strong></para>
    /// <para>Database indexes for efficient querying are defined in AppDbContext.OnModelCreating:</para>
    /// <list type="bullet">
    ///   <item><description>IX_Dishes_Restaurant_Category: Composite index for filtering dishes by category per restaurant</description></item>
    ///   <item><description>IX_Dishes_Restaurant_IsActive: For showing only active dishes in customer interfaces</description></item>
    ///   <item><description>IX_Dishes_Name: For dish name searches and autocomplete</description></item>
    ///   <item><description>IX_Dishes_Price: For price-based filtering and sorting</description></item>
    ///   <item><description>IX_MenuDishes_Dish_Menu: For efficient menu-dish relationship queries</description></item>
    /// </list>
    /// 
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Menu Management:</strong> Create and manage menu items with detailed information</description></item>
    ///   <item><description><strong>Multi-Menu Assignment:</strong> Add same dish to multiple menus with different pricing</description></item>
    ///   <item><description><strong>Customer Browsing:</strong> Display dishes with nutritional info and allergen warnings</description></item>
    ///   <item><description><strong>Order Processing:</strong> Link dishes to order line items with point-in-time pricing</description></item>
    ///   <item><description><strong>Sales Analytics:</strong> Track dish popularity and revenue contribution</description></item>
    ///   <item><description><strong>Review Management:</strong> Collect and display customer feedback per dish</description></item>
    ///   <item><description><strong>Dietary Filtering:</strong> Filter menus by vegetarian, spicy, allergen-free options</description></item>
    ///   <item><description><strong>Inventory Planning:</strong> Track ingredient usage through sales data</description></item>
    /// </list>
    /// </remarks>
    [Table("Dishes")]
    public class Dish : TenantEntityBase
    {
        // === Standalone Properties ===

        /// <summary>
        /// Name of the dish.
        /// </summary>
        [Required(ErrorMessage = "Dish name is required")]
        [MaxLength(100, ErrorMessage = "Dish name cannot exceed 100 characters")]
        [MinLength(3, ErrorMessage = "Dish name must be at least 3 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the dish
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Price of the dish
        /// </summary>
        [Required(ErrorMessage = "Dish price is required")]
        [Range(0.01, 10000.00, ErrorMessage = "Price must be between 0.01 and 10,000.00")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal DishPrice { get; set; }

        /// <summary>
        /// Foreign key to the dish's category
        /// </summary>
        [Required(ErrorMessage = "CategoryId is required")]
        [ForeignKey(nameof(Category))]
        public int CategoryId { get; set; }

        /// <summary>
        /// Preparation time in minutes
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Preparation time must be positive")]
        public int? PreparationTime { get; set; }

        /// <summary>
        /// Calories per serving
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Calories must be non-negative")]
        public int? Calories { get; set; }

        /// <summary>
        /// Whether the dish is vegetarian
        /// </summary>
        public bool IsVegetarian { get; set; }

        /// <summary>
        /// Whether the dish is spicy
        /// </summary>
        public bool IsSpicy { get; set; }

        /// <summary>
        /// Ingredients list
        /// </summary>
        [MaxLength(1000)]
        public string? Ingredients { get; set; }

        /// <summary>
        /// Allergen information
        /// </summary>
        [MaxLength(500)]
        public string? Allergens { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property to the Category this dish belongs to.
        /// </summary>
        public virtual Category Category { get; set; } = null!;

        /// <summary>
        /// Collection of MenuDish entries associated with this dish.
        /// This represents the many-to-many relationship between Menu and Dish with additional properties.
        /// Use this collection for operations requiring access to menu-specific properties.
        /// </summary>
        [InverseProperty(nameof(MenuDish.Dish))]
        public virtual ICollection<MenuDish> MenuDishes { get; set; } = new List<MenuDish>();

        /// <summary>
        /// Collection of menus this dish appears on through the MenuDish join entity.
        /// Provides convenient access to related menus without needing to traverse the join entity.
        /// </summary>
        [InverseProperty(nameof(Menu.Dishes))]
        public virtual ICollection<Menu> Menus { get; set; } = new List<Menu>();

        /// <summary>
        /// Collection of reviews for this dish
        /// </summary>
        [InverseProperty(nameof(Review.Dish))]
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        /// <summary>
        /// Collection of sale records for this dish
        /// </summary>
        [InverseProperty(nameof(SaleRecord.Dish))]
        public virtual ICollection<SaleRecord> SaleRecords { get; set; } = new List<SaleRecord>();

        /// <summary>
        /// Collection of order items for this dish
        /// </summary>
        [InverseProperty(nameof(OrderItem.Dish))]
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // ===================================================================
        // MULTI-TENANT VALIDATION
        // ===================================================================

        /// <summary>
        /// Validates that the dish maintains multi-tenant boundaries and consistency across all relationships.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when tenant consistency is violated.</exception>
        /// <remarks>
        /// This method should be called after navigation properties are loaded to ensure:
        /// - Restaurant navigation property matches RestaurantId
        /// - Category belongs to the same restaurant
        /// - All menu assignments belong to the same restaurant
        /// - All reviews belong to the same restaurant
        /// - All sale records belong to the same restaurant
        /// - All order items belong to the same restaurant
        /// 
        /// Tenant Consistency Rules:
        /// 1. Dish must belong to exactly one restaurant (RestaurantId)
        /// 2. Category must belong to the same restaurant as the dish
        /// 3. All menus in MenuDishes collection must belong to the same restaurant
        /// 4. All reviews must belong to the same restaurant
        /// 5. All sale records must belong to the same restaurant
        /// 6. All order items must belong to the same restaurant (via order)
        /// 7. Restaurant navigation property ID (if loaded) must match RestaurantId
        /// 
        /// Security Implications:
        /// This is a critical security check in multi-tenant systems to prevent:
        /// - Cross-tenant data leakage through menu assignments
        /// - Dishes from one restaurant appearing in another restaurant's menus
        /// - Reviews from one restaurant's dishes appearing elsewhere
        /// - Sales data mixing across restaurant tenants
        /// - Order data crossing tenant boundaries
        /// - Reporting inaccuracies in multi-tenant dashboards
        /// 
        /// When to Call:
        /// - After loading dishes with navigation properties from database
        /// - Before displaying dish information in multi-tenant contexts
        /// - In data import/migration processes
        /// - As part of data integrity audits
        /// - When validating menu assignments and relationships
        /// 
        /// Performance Note:
        /// Only performs validation if navigation properties are loaded.
        /// Does not trigger lazy loading to avoid N+1 query issues.
        /// For large collections (MenuDishes, Reviews, SaleRecords), consider validating via database queries instead.
        /// </remarks>
        public void ValidateTenantConsistency()
        {
            // Validate Restaurant navigation property consistency
            if (Restaurant != null && Restaurant.Id != RestaurantId)
            {
                throw new InvalidOperationException(
                    $"Restaurant navigation property ID ({Restaurant.Id}) does not match RestaurantId ({RestaurantId}).");
            }

            // Validate Category belongs to same restaurant
            if (Category != null && Category.RestaurantId != RestaurantId)
            {
                throw new InvalidOperationException(
                    $"Dish category must belong to the same restaurant. " +
                    $"Dish RestaurantId: {RestaurantId}, Category RestaurantId: {Category.RestaurantId}, " +
                    $"Category: {Category.Name} (ID: {Category.Id})");
            }

            // Validate all menu assignments belong to same restaurant
            if (MenuDishes != null && MenuDishes.Any())
            {
                var inconsistentMenus = MenuDishes
                    .Where(md => md.Menu != null && md.Menu.RestaurantId != RestaurantId)
                    .Select(md => new { md.MenuId, md.Menu?.Name, md.Menu?.RestaurantId })
                    .ToList();

                if (inconsistentMenus.Any())
                {
                    var menuInfo = string.Join(", ", inconsistentMenus.Select(m => 
                        $"{m.Name ?? "Unknown"} (ID: {m.MenuId}, RestaurantId: {m.RestaurantId})"));
                    
                    throw new InvalidOperationException(
                        $"Dish contains menu assignments from different restaurants. " +
                        $"Dish RestaurantId: {RestaurantId}, " +
                        $"Inconsistent menus: [{menuInfo}]");
                }
            }

            // Validate all reviews belong to same restaurant
            if (Reviews != null && Reviews.Any())
            {
                var inconsistentReviews = Reviews
                    .Where(r => r.RestaurantId != RestaurantId)
                    .Select(r => new { r.Id, r.RestaurantId })
                    .ToList();

                if (inconsistentReviews.Any())
                {
                    var reviewIds = string.Join(", ", inconsistentReviews.Select(r => r.Id));
                    var restaurantIds = string.Join(", ", inconsistentReviews.Select(r => r.RestaurantId).Distinct());
                    
                    throw new InvalidOperationException(
                        $"Dish contains reviews from different restaurants. " +
                        $"Dish RestaurantId: {RestaurantId}, " +
                        $"Inconsistent Review IDs: [{reviewIds}], " +
                        $"Inconsistent Restaurant IDs: [{restaurantIds}]");
                }
            }

            // Validate all sale records belong to same restaurant
            if (SaleRecords != null && SaleRecords.Any())
            {
                var inconsistentSales = SaleRecords
                    .Where(sr => sr.RestaurantId != RestaurantId)
                    .Select(sr => new { sr.Id, sr.RestaurantId, sr.SaleDate })
                    .ToList();

                if (inconsistentSales.Any())
                {
                    var saleInfo = string.Join(", ", inconsistentSales.Select(s => 
                        $"ID: {s.Id} (RestaurantId: {s.RestaurantId}, Date: {s.SaleDate:yyyy-MM-dd})"));
                    
                    throw new InvalidOperationException(
                        $"Dish contains sale records from different restaurants. " +
                        $"Dish RestaurantId: {RestaurantId}, " +
                        $"Inconsistent sales: [{saleInfo}]");
                }
            }

            // Validate all order items belong to same restaurant (via their orders)
            if (OrderItems != null && OrderItems.Any())
            {
                var inconsistentOrderItems = OrderItems
                    .Where(oi => oi.Order != null && oi.Order.RestaurantId != RestaurantId)
                    .Select(oi => new { oi.Id, oi.OrderId, oi.Order?.RestaurantId })
                    .ToList();

                if (inconsistentOrderItems.Any())
                {
                    var orderItemInfo = string.Join(", ", inconsistentOrderItems.Select(oi => 
                        $"OrderItem ID: {oi.Id}, Order ID: {oi.OrderId}, RestaurantId: {oi.RestaurantId}"));
                    
                    throw new InvalidOperationException(
                        $"Dish contains order items from different restaurants. " +
                        $"Dish RestaurantId: {RestaurantId}, " +
                        $"Inconsistent order items: [{orderItemInfo}]");
                }
            }
        }

        /// <summary>
        /// Validates the dish entity.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>A collection of validation results.</returns>
        /// <remarks>
        /// Validation Overview:
        /// 
        /// 1. Restaurant/Tenant Boundary Validation:
        ///    - Ensures all menu assignments belong to same restaurant as dish
        ///    - Maintains multi-tenant data isolation
        ///    - Prevents cross-tenant data leakage
        /// 
        /// 2. Price Validation:
        ///    - Validates special prices in menu assignments
        ///    - Ensures prices are positive
        ///    - Limits markup to 5x base price
        ///    - Prevents unreasonable price overrides
        /// 
        /// 3. Menu Assignment Validation:
        ///    - Requires at least one active menu for established dishes
        ///    - New dishes (Id = 0) exempt from this requirement
        ///    - Ensures dishes are available to customers
        /// 
        /// 4. Data Consistency:
        ///    - Validates relationships across entities
        ///    - Ensures proper tenant boundaries
        ///    - Maintains data integrity
        /// 
        /// Additional Validations via Attributes:
        /// - Name: Required, 3-100 chars
        /// - Description: Max 500 chars
        /// - Price: Required, 0.01-10,000.00
        /// - CategoryId: Required, FK relationship
        /// - PreparationTime: Positive integer
        /// - Calories: Non-negative integer
        /// - Ingredients: Max 1000 chars
        /// - Allergens: Max 500 chars
        /// 
        /// Change History:
        /// - Improved price validation logic
        /// - Enhanced tenant boundary checks
        /// - Added menu assignment requirements
        /// - Improved error messages
        /// </remarks>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Restaurant/Tenant consistency validation
            var differentRestaurantMenus = MenuDishes
                .Where(md => md.Menu != null && md.Menu.RestaurantId != RestaurantId)
                .Select(md => md.MenuId)
                .ToList();

            if (differentRestaurantMenus.Any())
            {
                yield return new ValidationResult(
                    $"Dish is assigned to menus from different restaurants. Menu IDs: {string.Join(", ", differentRestaurantMenus)}",
                    new[] { nameof(MenuDishes) }
                );
            }

            // Price validation for menu assignments
            var invalidPrices = MenuDishes
                .Where(md => md.SpecialPrice.HasValue && 
                          (md.SpecialPrice.Value <= 0 || md.SpecialPrice.Value > DishPrice * 5))
                .ToList();

            if (invalidPrices.Any())
            {
                yield return new ValidationResult(
                    "Some menu assignments have invalid special prices (must be positive and not exceed 5x base price)",
                    new[] { nameof(MenuDishes) }
                );
            }

            // Menu assignment validation for established dishes
            if (Id != 0 && !MenuDishes.Any(md => md.IsActive))
            {
                yield return new ValidationResult(
                    "Dish must be assigned to at least one active menu",
                    new[] { nameof(MenuDishes) }
                );
            }

            // Base price validation
            if (DishPrice <= 0)
            {
                yield return new ValidationResult(
                    "Dish price must be greater than zero",
                    new[] { nameof(DishPrice) }
                );
            }

            // Category validation
            if (CategoryId <= 0)
            {
                yield return new ValidationResult(
                    "Category must be specified",
                    new[] { nameof(CategoryId) }
                );
            }

            // Name validation
            if (string.IsNullOrWhiteSpace(Name))
            {
                yield return new ValidationResult(
                    "Dish name must not be empty or whitespace",
                    new[] { nameof(Name) }
                );
            }

            // Optional field validations
            if (PreparationTime.HasValue && PreparationTime.Value <= 0)
            {
                yield return new ValidationResult(
                    "Preparation time must be positive when specified",
                    new[] { nameof(PreparationTime) }
                );
            }

            if (Calories.HasValue && Calories.Value < 0)
            {
                yield return new ValidationResult(
                    "Calories must be non-negative when specified",
                    new[] { nameof(Calories) }
                );
            }
        }
    }
}
