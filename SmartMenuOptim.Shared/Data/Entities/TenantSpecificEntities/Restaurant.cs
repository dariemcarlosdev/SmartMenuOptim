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


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a restaurant in the system. Each restaurant is owned by an AdminUser (owner),
    /// and can have its own dishes, categories, and reviews.
    /// </summary>
    /// <remarks>
    /// Multi-Tenant Support: This entity is the root tenant entity. Each Restaurant acts as a tenant.
    /// Indexes for common queries (e.g., by OwnerId, Name, Email) should be centralized in `AppDbContext.OnModelCreating`.
    ///
    /// NOTE: Design rationale for inheritance
    /// - Restaurant is the tenant root and therefore MUST NOT inherit from `TenantEntityBase`.
    /// - `TenantEntityBase` contains a `RestaurantId` foreign key and `Restaurant` navigation; inheriting it would create
    ///   a self-referencing tenant FK on the Restaurant table (conceptually incorrect and problematic for migrations).
    /// - Shared audit/concurrency properties live on `EntityBase`. Tenant-scoped entities should inherit `TenantEntityBase : EntityBase`.
    /// - Recommended model:
    ///     `EntityBase` (Id, CreatedAt, UpdatedAt, IsDeleted, xmin/RowVersion)
    ///     `TenantEntityBase : EntityBase` (+ RestaurantId, Restaurant nav)
    ///     `Restaurant : EntityBase` (tenant root)
    /// - This separation keeps the domain model clean and avoids redundant or circular FK mappings.
    /// </remarks>
    [Table("Restaurants")]
    public class Restaurant : EntityBase, IValidatableObject
    {
        // === Standalone Properties ===

        /// <summary>
        /// Name of the restaurant.
        /// </summary>
        [Required(ErrorMessage = "Restaurant name is required")]
        [MaxLength(200, ErrorMessage = "Restaurant name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        // === Relationship Properties (Foreign Keys) ===

        /// <summary>
        /// Foreign key to the owner (AdminUser). Each restaurant is owned by a single admin user.
        /// </summary>
        [Required]
        [ForeignKey(nameof(Owner))]
        public int OwnerId { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property to the owner (AdminUser).
        /// Each restaurant is owned by a single admin user.
        /// </summary>
        [InverseProperty(nameof(AdminUser.OwnedRestaurants))]
        public AdminUser? Owner { get; set; }

        /// <summary>
        /// Navigation property for all dishes in this restaurant.
        /// </summary>
        [InverseProperty(nameof(Dish.Restaurant))]
        public ICollection<Dish> Dishes { get; set; } = new List<Dish>();

        /// <summary>
        /// Navigation property for all categories in this restaurant.
        /// </summary>
        [InverseProperty(nameof(Category.Restaurant))]
        public ICollection<Category> Categories { get; set; } = new List<Category>();

        /// <summary>
        /// Navigation property for all reviews in this restaurant.
        /// </summary>
        [InverseProperty(nameof(Review.Restaurant))]
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        /// <summary>
        /// Navigation property for all menus in this restaurant.
        /// </summary>
        [InverseProperty(nameof(Menu.Restaurant))]
        public ICollection<Menu> Menus { get; set; } = new List<Menu>();

        /// <summary>
        /// Navigation property for all orders in this restaurant.
        /// </summary>
        [InverseProperty(nameof(Order.Restaurant))]
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        /// <summary>
        /// Navigation property for all tables in this restaurant.
        /// </summary>
        [InverseProperty(nameof(Table.Restaurant))]
        public ICollection<Table> Tables { get; set; } = new List<Table>();

        /// <summary>
        /// Navigation property for all staff schedules in this restaurant.
        /// </summary>
        [InverseProperty(nameof(StaffSchedule.Restaurant))]
        public ICollection<StaffSchedule> StaffSchedules { get; set; } = new List<StaffSchedule>();

        /// <summary>
        /// Navigation property for all customer loyalty records in this restaurant.
        /// </summary>
        [InverseProperty(nameof(CustomerLoyalty.Restaurant))]
        public ICollection<CustomerLoyalty> CustomerLoyalties { get; set; } = new List<CustomerLoyalty>();

        /// <summary>
        /// Navigation property for all promotions in this restaurant.
        /// </summary>
        [InverseProperty(nameof(Promotion.Restaurant))]
        public ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

        /// <summary>
        /// Navigation property for all sale records in this restaurant.
        /// </summary>
        [InverseProperty(nameof(SaleRecord.Restaurant))]
        public ICollection<SaleRecord> SaleRecords { get; set; } = new List<SaleRecord>();

        /// <summary>
        /// Gets or sets the collection of menu types associated with this restaurant.
        /// </summary>
        [InverseProperty(nameof(MenuType.Restaurant))]
        public ICollection<MenuType> MenuTypes { get; set; } = new List<MenuType>();

        /// <summary>
        /// Navigation property for all order statuses in this restaurant.
        /// </summary>
        [InverseProperty(nameof(OrderStatus.Restaurant))]
        public ICollection<OrderStatus> OrderStatuses { get; set; } = new List<OrderStatus>();


        // === Business Properties ===

        /// <summary>
        /// Contact email for the restaurant.
        /// </summary>
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Phone number for the restaurant.
        /// </summary>
        [Required]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [MaxLength(50, ErrorMessage = "Phone number cannot exceed 50 characters")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Physical address of the restaurant.
        /// </summary>
        [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Operating hours and additional information.
        /// </summary>
        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Restaurant's timezone identifier (e.g., "America/New_York").
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "TimeZoneId cannot exceed 100 characters")]
        public string TimeZoneId { get; set; } = "UTC";

          // === Validation ===
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Name is required", new[] { nameof(Name) });

            if (!string.IsNullOrWhiteSpace(TimeZoneId) && TimeZoneId.Length > 100)
                yield return new ValidationResult("TimeZoneId is too long", new[] { nameof(TimeZoneId) });

            if (!string.IsNullOrWhiteSpace(PhoneNumber) && PhoneNumber.Length > 50)
                yield return new ValidationResult("PhoneNumber is too long", new[] { nameof(PhoneNumber) });

            yield break;
        }
    }
}
