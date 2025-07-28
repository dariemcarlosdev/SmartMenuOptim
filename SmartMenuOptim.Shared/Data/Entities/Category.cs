using System.Collections.Generic;

namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Represents a category of dishes (e.g., Italian, Salad).
    /// Each Category can have multiple Dishes.
    /// Navigation properties:
    /// - Dishes: all dishes that belong to this category.
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Primary key for the Category entity.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the category.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for all dishes in this category.
        /// </summary>
        public ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    }
}
