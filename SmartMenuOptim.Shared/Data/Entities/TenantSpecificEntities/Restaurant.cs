/*
================================================================================
Multi-Tenant Implementation Design Note
================================================================================
Current Implementation uses RestaurantId as the tenant identifier, which:
1. Avoids Redundancy:
   - RestaurantId serves dual purpose as both foreign key and tenant identifier
   - No need for separate TenantId since Restaurant IS the tenant
   - Simpler data model and clearer business relationships
   - Better performance with integer keys vs GUIDs

2. Natural Business Relationship:
   - Restaurant entity naturally represents the tenant
   - Child entities (Dish, Review, Category) inherently isolated by RestaurantId
   - Maintains clear domain-driven design principles

3. Future Extensibility:
   - TenantId (currently commented out) should only be implemented if:
     * Adding restaurant groups/chains that need tenant isolation
     * Implementing hierarchical multi-tenancy
     * Requiring GUID-based tenant identification for specific scaling needs

The current design provides effective tenant isolation while maintaining
simplicity and performance. Adding a separate TenantId would be redundant
unless the multi-tenancy model needs to evolve beyond single restaurants.

================================================================================
Multi-Tenant Expansion Reference
================================================================================
Other entities that could be tenant-specific in your multi-tenant restaurant application include:
• Menu: Each restaurant (tenant) can have its own set of menus (e.g., breakfast, lunch, dinner, seasonal).
• Ingredient: If ingredients are managed per restaurant (e.g., inventory, supplier), they should be tenant-specific.
• Order: Orders placed by customers are specific to a restaurant.
• OrderItem: Items within an order, linked to dishes of a specific restaurant.
• Reservation: Table reservations are specific to a restaurant.
• Table: Physical tables in a restaurant, if you manage seating/floor plans.
• Promotion/Discount: Special offers or discounts that apply only to a specific restaurant.
• Staff/User: Employees or users (e.g., waiters, managers) assigned to a specific restaurant.
• Notification: System or user notifications scoped to a restaurant.
• Payment/Transaction: Payments processed for orders in a specific restaurant.
• Customer Loyalty Program: If loyalty points or rewards are tracked per restaurant.

Any entity that represents data or business logic unique to a single restaurant (tenant) should be considered tenant-specific to ensure proper data isolation and multi-tenancy support.
================================================================================
*/


using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a restaurant in the system. Each restaurant is owned by an AdminUser (owner),
    /// and can have its own dishes, categories, and reviews.
    /// </summary>
    /// <remarks>
    /// Multi-Tenant Support: This entity is the root tenant entity. Each Restaurant acts as a tenant, allowing the application to support multiple restaurants per owner (AdminUser), each with their own menus, dishes, and reviews. This structure is a solid foundation for a multi-tenant application.
    /// </remarks>
    public class Restaurant
    {
        // === Standalone Properties ===

        /// <summary>
        /// Primary key for the Restaurant entity.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the restaurant.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        // === Multi-Tenancy Expansion Properties ===
        // The following properties are required for advanced multi-tenancy support (e.g., tenant isolation, auditing, soft deletion, concurrency control).
        // Uncomment these when expanding the application to support full multi-tenant scenarios.

        /// <summary>
        /// Unique tenant identifier for the restaurant (for multi-tenant isolation).
        /// </summary>
        //public Guid TenantId { get; set; } = Guid.NewGuid();


        // === Relationship Properties (Foreign Keys) ===

        /// <summary>
        /// Foreign key to the owner (AdminUser). Each restaurant is owned by a single admin user.
        /// </summary>
        public int OwnerId { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property to the owner (AdminUser).
        /// Each restaurant is owned by a single admin user.
        /// </summary>
        public AdminUser? Owner { get; set; }

        /// <summary>
        /// Navigation property for all dishes in this restaurant.
        /// </summary>
        public ICollection<Dish> Dishes { get; set; } = [];

        /// <summary>
        /// Navigation property for all categories in this restaurant.
        /// </summary>
        public ICollection<Category> Categories { get; set; } = [];

        /// <summary>
        /// Navigation property for all reviews in this restaurant.
        /// </summary>
        public ICollection<Review> Reviews { get; set; } = [];

        // === Business Properties ===
        
        /// <summary>
        /// Contact email for the restaurant.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Phone number for the restaurant.
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Physical address of the restaurant.
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Operating hours and additional information.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Restaurant's timezone identifier (e.g., "America/New_York").
        /// </summary>
        public string TimeZoneId { get; set; } = "UTC";

        /// <summary>
        /// Whether the restaurant is currently active and operating.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
