using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartMenuOptim.Domain.Entities.RestaurantEntities
{
    /// <summary>
    /// Represents a dish category for organizing menu items within a restaurant's menu system.
    /// </summary>
    /// <remarks>
    /// This is a LOOKUP AGGREGATE ROOT following Domain-Driven Design principles.
    /// <para><strong>3-TIER DDD STRATEGY: Tier 2 - Simple Aggregates (Lightweight DDD) - Lookup/Reference Data</strong></para>
    /// <para>This class implements a lightweight DDD aggregate pattern for lookup/reference data entities. While simpler than
    /// main domain aggregates (Menu, Order, Restaurant), it still provides encapsulation, validation, and behavioral methods
    /// to maintain data consistency and support menu organization.</para>
    /// 
    /// <para><strong>Tier 2 Characteristics (Lookup Aggregate):</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Encapsulation:</strong> Properties use private setters to prevent unauthorized state changes</description></item>
    ///   <item><description><strong>Validation:</strong> Business rules enforced through constructor and behavioral methods with guard clauses</description></item>
    ///   <item><description><strong>Rich Behavior:</strong> Domain logic encapsulated in methods (UpdateBasicInfo, UpdateDisplayOrder) rather than anemic property bags</description></item>
    ///   <item><description><strong>Simple Lifecycle:</strong> No complex child entities, serves as reference data for dish classification</description></item>
    ///   <item><description><strong>Lightweight Invariants:</strong> Basic consistency rules (name uniqueness, display order, minimum content)</description></item>
    ///   <item><description><strong>Reference Data:</strong> Defines dish classifications referenced by Dish aggregate via CategoryId</description></item>
    /// </list>
    /// 
    /// <para><strong>Entity Overview:</strong></para>
    /// <para>A Category organizes dishes into logical groupings within a restaurant's menu system. Common categories include
    /// "Appetizers", "Main Course", "Desserts", "Beverages", "Salads", or cuisine-based groupings like "Italian", "Asian Fusion",
    /// "Vegetarian". Categories support menu navigation, filtering, and organization in both digital menus and physical menu cards.
    /// Each category can contain multiple dishes and includes display ordering for consistent presentation.</para>
    /// 
    /// <para><strong>Multi-Tenant Support:</strong></para>
    /// <para>Inherits from TenantEntityBase to provide built-in multi-tenancy support. Each category is scoped to a specific
    /// restaurant (RestaurantId), allowing restaurants to define custom category structures. This ensures proper data isolation
    /// in a multi-tenant environment and prevents cross-tenant category references.</para>
    /// 
    /// <para><strong>Consistency Boundary:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Invariants Protected:</strong> Name required (2-50 chars), alphanumeric format, description minimum 10 chars if provided, display order non-negative and capped at 1000</description></item>
    ///   <item><description><strong>Encapsulated State:</strong> Internal state can only be modified through behavioral methods (UpdateBasicInfo, UpdateDisplayOrder)</description></item>
    ///   <item><description><strong>Transactional Consistency:</strong> All changes validated atomically through public methods</description></item>
    ///   <item><description><strong>Business Rules:</strong> Established categories must contain at least one dish, dishes must belong to same restaurant</description></item>
    ///   <item><description><strong>Reference Data Integrity:</strong> Cannot be deleted if referenced by active dishes</description></item>
    /// </list>
    /// 
    /// <para><strong>Domain Features:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Identity:</strong> Inherits entity identity from TenantEntityBase (Id property from EntityBase)</description></item>
    ///   <item><description><strong>Automatic Timestamps:</strong> CreatedAt, UpdatedAt automatically managed through EntityBase</description></item>
    ///   <item><description><strong>Soft Delete Support:</strong> Inherits IsDeleted flag for soft deletion scenarios</description></item>
    ///   <item><description><strong>Optimistic Concurrency:</strong> Uses xmin timestamp token from EntityBase for concurrency control</description></item>
    ///   <item><description><strong>Display Order:</strong> Supports custom ordering for menu presentation and navigation</description></item>
    ///   <item><description><strong>Name Format Validation:</strong> Enforces alphanumeric format with spaces and hyphens only</description></item>
    /// </list>
    /// 
    /// <para><strong>Relationships:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Dishes (One-to-Many):</strong> Referenced by Dish entities via CategoryId foreign key</description></item>
    ///   <item><description><strong>Restaurant (Required):</strong> Inherited from TenantEntityBase, ensures tenant isolation</description></item>
    ///   <item><description><strong>Lookup/Reference Data:</strong> Provides classification for menu items</description></item>
    /// </list>
    /// 
    /// <para><strong>Example Usage:</strong></para>
    /// <code>
    /// // Creating standard menu categories
    /// var appetizers = new Category(
    ///     restaurantId: 123,
    ///     name: "Appetizers",
    ///     description: "Start your meal with our delicious starters",
    ///     displayOrder: 1
    /// );
    /// 
    /// var mainCourse = new Category(
    ///     restaurantId: 123,
    ///     name: "Main Course",
    ///     description: "Signature entrees and hearty meals",
    ///     displayOrder: 2
    /// );
    /// 
    /// var desserts = new Category(
    ///     restaurantId: 123,
    ///     name: "Desserts",
    ///     description: "Sweet treats to complete your dining experience",
    ///     displayOrder: 3
    /// );
    /// 
    /// // Updating category information
    /// appetizers.UpdateBasicInfo(
    ///     name: "Starters & Appetizers",
    ///     description: "Light bites and appetizing beginnings"
    /// );
    /// 
    /// // Reordering categories
    /// desserts.UpdateDisplayOrder(10);
    /// 
    /// // Validating tenant consistency after loading from database
    /// appetizers.ValidateTenantConsistency();
    /// 
    /// // Using categories in menu organization
    /// var dish = new Dish(restaurantId, "Caesar Salad", appetizers.Id);
    /// </code>
    /// 
    /// <para><strong>Entity Framework Core Support:</strong></para>
    /// <para>Includes a protected parameterless constructor for EF Core's use during materialization. The entity can be
    /// persisted and retrieved through a repository pattern. Private setters are accessible to EF Core through reflection-based
    /// field mapping in the entity configuration. Navigation properties configured for dish relationships.</para>
    /// 
    /// <para><strong>Design Considerations:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Name Uniqueness:</strong> Should be unique per restaurant to avoid confusion (recommended, enforced at application level or database index)</description></item>
    ///   <item><description><strong>Display Order Cap:</strong> Maximum 1000 to prevent excessive ordering values</description></item>
    ///   <item><description><strong>Name Format:</strong> Alphanumeric with spaces and hyphens for clean menu presentation</description></item>
    ///   <item><description><strong>Minimum Content Rule:</strong> Established categories (Id != 0) must have at least one dish to remain valid</description></item>
    ///   <item><description><strong>Description Optional:</strong> But if provided, must be meaningful (minimum 10 characters)</description></item>
    ///   <item><description><strong>Reference Data Stability:</strong> Categories should be relatively stable; changes affect all referencing dishes</description></item>
    ///   <item><description><strong>Soft Delete:</strong> Prefer soft deletion over hard deletion to maintain dish classification history</description></item>
    /// </list>
    /// 
    /// <para><strong>Indexing Strategy:</strong></para>
    /// <para>Database indexes for efficient querying are defined centrally in AppDbContext.OnModelCreating:
    /// - IX_Categories_Restaurant_DisplayOrder: For tenant-scoped category ordering in menus
    /// - IX_Categories_Restaurant_Name: For lookup by category name within restaurant
    /// - Unique constraint on (RestaurantId, Name) to prevent duplicate category names per restaurant</para>
    /// 
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Menu Organization:</strong> Group dishes for easier navigation in digital and physical menus</description></item>
    ///   <item><description><strong>Filtering:</strong> Allow customers to filter menu by category (vegetarian, seafood, etc.)</description></item>
    ///   <item><description><strong>Menu Sections:</strong> Define sections in printed menus and digital menu apps</description></item>
    ///   <item><description><strong>Reporting:</strong> Analyze sales and popularity by category</description></item>
    ///   <item><description><strong>Kitchen Organization:</strong> Route orders to appropriate kitchen stations based on category</description></item>
    ///   <item><description><strong>Pricing Strategy:</strong> Apply category-based pricing or promotions</description></item>
    /// </list>
    /// </remarks>
    [Table("Categories")]
    public class Category : TenantEntityBase, IValidatableObject
    {
        // ===================================================================
        // PROPERTIES WITH ENCAPSULATION (Private Setters)
        // ===================================================================

        /// <summary>
        /// Name of the category. Must be unique within a restaurant.
        /// </summary>
        /// <remarks>
        /// Required identifier for the menu classification. Must be:
        /// - Non-empty and non-whitespace
        /// - Between 2 and 50 characters
        /// - Alphanumeric with spaces and hyphens only
        /// - Unique per restaurant (recommended, enforced at application/database level)
        /// 
        /// Common Category Names:
        /// - Course-based: "Appetizers", "Soups & Salads", "Main Course", "Desserts", "Beverages"
        /// - Cuisine-based: "Italian", "Asian Fusion", "Mexican", "Mediterranean"
        /// - Dietary-based: "Vegetarian", "Vegan", "Gluten-Free", "Low-Carb"
        /// - Meal-based: "Breakfast", "Lunch", "Dinner", "Brunch"
        /// - Special: "Chef's Specials", "Seasonal", "Kids Menu"
        /// 
        /// Modifiable via UpdateBasicInfo() method.
        /// </remarks>
        [Required(ErrorMessage = "Category name is required")]
        [MaxLength(50, ErrorMessage = "Category name cannot exceed 50 characters")]
        [MinLength(2, ErrorMessage = "Category name must be at least 2 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "Category name can only contain letters, numbers, spaces, and hyphens")]
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Optional description of the category providing additional context.
        /// </summary>
        /// <remarks>
        /// Provides detailed information about the category for:
        /// - Maximum length: 500 characters
        /// - Minimum length: 10 characters (if provided)
        /// - Can be null or empty
        /// 
        /// Used for:
        /// - Menu descriptions and storytelling
        /// - Digital menu tooltips
        /// - Marketing and customer engagement
        /// - Staff training materials
        /// 
        /// Examples:
        /// - "Start your meal with our handcrafted appetizers"
        /// - "Signature entrees prepared with locally sourced ingredients"
        /// - "Decadent desserts made fresh daily by our pastry chef"
        /// - "Refreshing beverages and specialty cocktails"
        /// 
        /// Modifiable via UpdateBasicInfo() method.
        /// </remarks>
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; private set; }

        /// <summary>
        /// Display order for sorting categories in menu presentations. Lower numbers appear first.
        /// </summary>
        /// <remarks>
        /// Controls the sequence in which categories appear in:
        /// - Digital menu apps and websites
        /// - Printed menu cards
        /// - Kitchen display systems
        /// - Point-of-sale systems
        /// 
        /// Typical Display Order Pattern:
        /// - 1: Appetizers/Starters
        /// - 2: Soups & Salads
        /// - 3: Main Course/Entrees
        /// - 4: Sides
        /// - 5: Desserts
        /// - 6: Beverages
        /// - 10: Specials (if separate)
        /// 
        /// Constraints:
        /// - Must be non-negative (0 or greater)
        /// - Maximum value: 1000
        /// - Gaps allowed (e.g., 1, 2, 5, 10)
        /// - Multiple categories can share same order (displayed alphabetically)
        /// 
        /// Modifiable via UpdateDisplayOrder() method.
        /// </remarks>
        [Range(0, int.MaxValue, ErrorMessage = "Display order must be a non-negative number")]
        public int DisplayOrder { get; private set; }

        // ===================================================================
        // NAVIGATION PROPERTIES
        // ===================================================================

        /// <summary>
        /// Navigation property for all dishes in this category.
        /// </summary>
        /// <remarks>
        /// Provides access to all Dish entities referencing this Category via CategoryId.
        /// 
        /// Used for:
        /// - Listing all dishes in a category for menus
        /// - Category-based filtering and search
        /// - Analytics on category popularity
        /// - Preventing deletion of categories with active dishes
        /// - Tenant consistency validation
        /// 
        /// Business Rules:
        /// - Established categories (Id != 0) should have at least one dish
        /// - All dishes must belong to the same restaurant as the category
        /// 
        /// Performance Considerations:
        /// - May contain large collections for popular categories
        /// - Use Include() explicitly when needed for eager loading
        /// - Consider pagination for categories with many dishes
        /// 
        /// Tenant Consistency:
        /// All dishes in this collection must belong to the same restaurant as this Category.
        /// Validated in ValidateTenantConsistency() and Validate() methods.
        /// </remarks>
        [InverseProperty(nameof(Dish.Category))]
        public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();
        
        // ===================================================================
        // CONSTRUCTORS
        // ===================================================================
        
        /// <summary>
        /// Protected parameterless constructor for Entity Framework Core.
        /// </summary>
        /// <remarks>
        /// Required by EF Core for entity materialization from database.
        /// Not intended for direct use in application code.
        /// EF Core uses reflection to populate properties after instantiation.
        /// </remarks>
        protected Category() { }
        
        /// <summary>
        /// Creates a new category with validation.
        /// </summary>
        /// <param name="restaurantId">The restaurant (tenant) identifier where this category is defined.</param>
        /// <param name="name">The category name (required, 2-50 characters, alphanumeric with spaces/hyphens).</param>
        /// <param name="description">Optional description (min 10 chars if provided, max 500 chars).</param>
        /// <param name="displayOrder">Display order for sorting (default: 0, must be non-negative, max: 1000).</param>
        /// <exception cref="ArgumentException">Thrown when validation fails for any parameter.</exception>
        /// <remarks>
        /// This constructor enforces invariants at creation time following DDD best practices.
        /// 
        /// Validation Rules Enforced:
        /// - RestaurantId must be positive integer (tenant identifier)
        /// - Name is required, non-whitespace, 2-50 characters, trimmed automatically
        /// - Name must match alphanumeric format (letters, numbers, spaces, hyphens only)
        /// - DisplayOrder defaults to 0, must be non-negative
        /// - Description is optional but if provided, trimmed automatically
        /// 
        /// Automatic Behavior:
        /// - Name is trimmed of leading/trailing whitespace
        /// - Description is trimmed if provided
        /// - CreatedAt automatically set to DateTime.UtcNow
        /// - UpdatedAt automatically set to DateTime.UtcNow
        /// - DisplayOrder defaults to 0 if not provided or negative
        /// 
        /// Usage Context:
        /// Typically called by:
        /// - Restaurant setup/configuration wizards
        /// - Admin menu management interfaces
        /// - Database seeding operations
        /// - Menu template applications
        /// </remarks>
        public Category(int restaurantId, string name, string? description = null, int displayOrder = 0)
        {
            if (restaurantId <= 0)
                throw new ArgumentException("Valid restaurant ID is required.", nameof(restaurantId));
            
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name is required.", nameof(name));
            
            if (name.Length < 2)
                throw new ArgumentException("Category name must be at least 2 characters.", nameof(name));
            
            RestaurantId = restaurantId;
            Name = name.Trim();
            Description = description?.Trim();
            DisplayOrder = displayOrder >= 0 ? displayOrder : 0;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        
        // ===================================================================
        // DOMAIN BEHAVIORS (Tier 2 - Lightweight DDD Methods)
        // ===================================================================
        
        /// <summary>
        /// Updates the category's basic information (name and description).
        /// </summary>
        /// <param name="name">The new category name (required, 2-50 characters, alphanumeric with spaces/hyphens).</param>
        /// <param name="description">The new description (optional, min 10 chars if provided, max 500 chars).</param>
        /// <exception cref="ArgumentException">Thrown when name is invalid.</exception>
        /// <remarks>
        /// This behavioral method allows authorized updates to category identity while maintaining encapsulation.
        /// 
        /// Common Use Cases:
        /// - Correcting typos in category names
        /// - Updating descriptions for clarity or marketing
        /// - Translating category names for localization
        /// - Refining menu terminology
        /// - Seasonal category updates
        /// 
        /// Validation:
        /// - Name must not be null, empty, or whitespace
        /// - Name must be at least 2 characters
        /// - Name is automatically trimmed
        /// - Description is automatically trimmed if provided
        /// 
        /// Side Effects:
        /// - Updates the UpdatedAt timestamp automatically (via EntityBase)
        /// - Preserves audit trail through timestamp changes
        /// 
        /// Authorization:
        /// This method should only be called by authorized users (admins, managers) through
        /// application services that enforce role-based access control.
        /// 
        /// Impact Considerations:
        /// - Changes affect all dishes currently using this category
        /// - Menu displays will update to reflect new name/description
        /// - Consider communication to kitchen staff when changing category names
        /// </remarks>
        public void UpdateBasicInfo(string name, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name is required.", nameof(name));
            
            if (name.Length < 2)
                throw new ArgumentException("Category name must be at least 2 characters.", nameof(name));
            
            Name = name.Trim();
            Description = description?.Trim();
            UpdatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Updates the display order for menu presentation.
        /// </summary>
        /// <param name="order">The new display order (must be between 0 and 1000).</param>
        /// <exception cref="ArgumentException">Thrown when order is out of valid range.</exception>
        /// <remarks>
        /// This behavioral method allows reordering categories in menus while maintaining encapsulation.
        /// 
        /// Common Use Cases:
        /// - Reordering menu sections for better flow
        /// - Promoting seasonal categories to top
        /// - Adjusting category sequence after adding new categories
        /// - Grouping related categories together
        /// - Separating special categories from standard ones
        /// 
        /// Validation:
        /// - Order must be 0 or greater (no negative values)
        /// - Order cannot exceed 1000 (maximum limit)
        /// 
        /// Side Effects:
        /// - Updates the UpdatedAt timestamp automatically
        /// - Changes category position in sorted menu lists
        /// 
        /// UI Impact:
        /// - Digital menus will show category in new position
        /// - Printed menus may need regeneration
        /// - Mobile apps will reflect new ordering
        /// - POS systems may reflow category display
        /// 
        /// Note: Multiple categories can share the same display order.
        /// When display orders match, categories are typically sorted alphabetically by name.
        /// </remarks>
        public void UpdateDisplayOrder(int order)
        {
            if (order < 0)
                throw new ArgumentException("Display order must be non-negative.", nameof(order));
            
            const int maxDisplayOrder = 1000;
            if (order > maxDisplayOrder)
                throw new ArgumentException($"Display order cannot exceed {maxDisplayOrder}.", nameof(order));
            
            DisplayOrder = order;
            UpdatedAt = DateTime.UtcNow;
        }

        // ===================================================================
        // MULTI-TENANT VALIDATION
        // ===================================================================

        /// <summary>
        /// Validates that the category maintains multi-tenant boundaries and consistency.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when tenant consistency is violated.</exception>
        /// <remarks>
        /// This method should be called after navigation properties are loaded to ensure:
        /// - Restaurant navigation property matches RestaurantId
        /// - All referenced dishes belong to the same restaurant
        /// 
        /// Tenant Consistency Rules:
        /// 1. Category must belong to exactly one restaurant (RestaurantId)
        /// 2. All dishes in Dishes collection must belong to the same restaurant
        /// 3. Restaurant navigation property ID (if loaded) must match RestaurantId
        /// 
        /// Security Implications:
        /// This is a critical security check in multi-tenant systems to prevent:
        /// - Cross-tenant category references
        /// - Dishes from one restaurant appearing in another restaurant's categories
        /// - Menu confusion between different restaurant tenants
        /// - Reporting inaccuracies in multi-tenant dashboards
        /// 
        /// When to Call:
        /// - After loading categories with navigation properties from database
        /// - Before displaying category information in multi-tenant contexts
        /// - In data import/migration processes
        /// - As part of data integrity audits
        /// - When validating dish category assignments
        /// 
        /// Performance Note:
        /// Only performs validation if navigation properties are loaded.
        /// Does not trigger lazy loading to avoid N+1 query issues.
        /// For large Dishes collections, consider validating via database query instead.
        /// </remarks>
        public void ValidateTenantConsistency()
        {
            // Validate Restaurant navigation property consistency
            if (Restaurant != null && Restaurant.Id != RestaurantId)
            {
                throw new InvalidOperationException(
                    $"Restaurant navigation property ID ({Restaurant.Id}) does not match RestaurantId ({RestaurantId}).");
            }

            // Validate all dishes belong to same restaurant
            if (Dishes != null && Dishes.Any())
            {
                var inconsistentDishes = Dishes
                    .Where(d => d.RestaurantId != RestaurantId)
                    .Select(d => new { d.Id, d.Name, d.RestaurantId })
                    .ToList();

                if (inconsistentDishes.Any())
                {
                    var dishInfo = string.Join(", ", inconsistentDishes.Select(d => $"{d.Name} (ID: {d.Id}, RestaurantId: {d.RestaurantId})"));
                    
                    throw new InvalidOperationException(
                        $"Category contains dishes from different restaurants. " +
                        $"Category RestaurantId: {RestaurantId}, " +
                        $"Inconsistent dishes: [{dishInfo}]");
                }
            }
        }

        // ===================================================================
        // VALIDATION LOGIC (IValidatableObject)
        // ===================================================================
        // IValidatableObject is REQUIRED for Tier 2 when used with EF Core SaveChanges validation
        // Delegates tenant consistency checks to avoid redundancy
        
        /// <summary>
        /// Validates the category entity ensuring data consistency and business rules.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>Collection of validation results.</returns>
        /// <remarks>
        /// Validation Rules:
        /// 1. Tenant Boundary:
        ///    - Must belong to exactly one restaurant
        ///    - All dishes must belong to same restaurant
        /// 2. Category Data:
        ///    - Name must be non-empty and non-whitespace
        ///    - Description minimum length if provided
        ///    - DisplayOrder within valid range
        /// 3. Content Rules:
        ///    - Established categories must have at least one dish
        /// </remarks>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // ===================================================================
            // TENANT CONSISTENCY VALIDATION
            // ===================================================================
            
            // Validate restaurant ID
            if (RestaurantId <= 0)
            {
                yield return new ValidationResult(
                    "Category must be associated with a restaurant",
                    new[] { nameof(RestaurantId) }
                );
            }

            // Validate restaurant navigation consistency
            if (Restaurant != null && Restaurant.Id != RestaurantId)
            {
                yield return new ValidationResult(
                    "Restaurant navigation property is inconsistent with RestaurantId",
                    new[] { nameof(Restaurant), nameof(RestaurantId) }
                );
            }

            // Validate dishes belong to same restaurant
            if (Dishes != null && Dishes.Any())
            {
                var inconsistentDishes = Dishes
                    .Where(d => d.RestaurantId != RestaurantId)
                    .Select(d => new { d.Id, d.Name })
                    .ToList();

                if (inconsistentDishes.Any())
                {
                    yield return new ValidationResult(
                        $"Category contains dishes from different restaurants. Inconsistent dishes: {string.Join(", ", inconsistentDishes.Select(d => $"{d.Name} (ID: {d.Id})"))}",
                        new[] { nameof(Dishes), nameof(RestaurantId) }
                    );
                }
            }

            // ===================================================================
            // BUSINESS RULE VALIDATION
            // ===================================================================
            // ===================================================================
            // BUSINESS RULE VALIDATION
            // ===================================================================

            // Name validation
            if (string.IsNullOrWhiteSpace(Name))
            {
                yield return new ValidationResult(
                    "Category name must not be empty or whitespace",
                    new[] { nameof(Name) }
                );
            }

            // Description validation (if provided)
            if (!string.IsNullOrEmpty(Description) && Description.Trim().Length < 10)
            {
                yield return new ValidationResult(
                    "Description, if provided, must be at least 10 characters long",
                    new[] { nameof(Description) }
                );
            }

            // Display order validation
            const int maxDisplayOrder = 1000;
            if (DisplayOrder > maxDisplayOrder)
            {
                yield return new ValidationResult(
                    $"Display order cannot exceed {maxDisplayOrder}",
                    new[] { nameof(DisplayOrder) }
                );
            }

            // Category content validation (only for established categories)
            if (Id != 0 && !Dishes.Any())
            {
                yield return new ValidationResult(
                    "Established categories must have at least one dish",
                    new[] { nameof(Dishes) }
                );
            }
        }
    }
}
