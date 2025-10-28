namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a sales record for a dish.
    /// </summary>
    public class SaleRecord : TenantEntityBase
    {
        // === Standalone Properties ===

        /// <summary>
        /// Primary key for the SaleRecord entity.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the restaurant. For multi-tenant support, each sale record is associated with a specific restaurant (tenant).
        /// </summary>
        public int RestaurantId { get; set; }

        /// <summary>
        /// Quantity of the dish sold in this record.
        /// </summary>
        public int QuantitySold { get; set; }

        /// <summary>
        /// Date of the sale (UTC).
        /// </summary>
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;

        // === Relationship Properties (Foreign Keys) ===

        /// <summary>
        /// Foreign key to the Dish entity. Each sale record is for a single dish.
        /// </summary>
        public int DishId { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property to the Dish this sales record is for.
        /// </summary>
        public Dish? Dish { get; set; }
    }
}
