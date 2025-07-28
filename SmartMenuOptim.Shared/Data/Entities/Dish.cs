using System.Collections.Generic;

namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Represents a dish offered by the restaurant.
    
    /// Each Dish belongs to a single Category (CategoryId is the foreign key). The relationship with Category is many-to-one, meaning a Dish can belong to one Category, but a Category can have many Dishes.
    /// The relationship is many-to-one, meaning a Dish can have many SaleRecords and Reviews, but each SaleRecord and Review is associated with one Dish.
    
    /// Navigation properties:
    /// - Category: the category this dish belongs to.
    /// - Reviews: all reviews associated with this dish.
    /// - SaleRecords: all sales records for this dish.
    /// </summary>
    public class Dish
    {
        /// <summary>
        /// Primary key for the Dish entity.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the dish.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the Category entity.
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// Navigation property to the Category this dish belongs to.
        /// </summary>
        public Category? Category { get; set; }

        /// <summary>
        /// Navigation property for all reviews associated with this dish.
        /// </summary>
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        /// <summary>
        /// Navigation property for all sales records associated with this dish.
        /// </summary>
        public ICollection<SaleRecord> SaleRecords { get; set; } = new List<SaleRecord>();
    }
}
