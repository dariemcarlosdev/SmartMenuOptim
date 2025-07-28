namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Represents a sales record for a dish.
    /// Each SaleRecord is associated with a single Dish (DishId is the foreign key). The relashionship is many-to-one, A SaleRecord belongs to one Dish, but a Dish can have many SaleRecords.
    /// Navigation properties:
    /// - Dish: the dish this sales record is for.
    /// </summary>
    public class SaleRecord
    {
        /// <summary>
        /// Primary key for the SaleRecord entity.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the Dish entity.
        /// </summary>
        public int DishId { get; set; }

        /// <summary>
        /// Navigation property to the Dish this sales record is for.
        /// </summary>
        public Dish Dish { get; set; } = new();

        /// <summary>
        /// Quantity of the dish sold in this record.
        /// </summary>
        public int QuantitySold { get; set; }

        /// <summary>
        /// Date of the sale.
        /// </summary>
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    }
}
