/*
 * File: RestaurantDTO.cs
 * Data Transfer Object for Restaurant entity
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Purpose: Represents a restaurant in the system for data transfer operations.
 * This DTO is the root tenant DTO in the multi-tenant architecture.
 * 
 * Multi-Tenant Considerations:
 * - This is the root tenant DTO representing a restaurant
 * - Each restaurant acts as an isolated tenant
 * - Contains only necessary data for client operations
 * - Maintains proper data isolation through tenant-specific collections
 */

namespace SmartMenuOptim.Application.Dtos
{
    /// <summary>
    /// Data Transfer Object for Restaurant entity
    /// </summary>
    public class RestaurantDTO
    {
        /// <summary>
        /// Restaurant identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the restaurant
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Owner (AdminUser) identifier
        /// </summary>
        public int OwnerId { get; set; }

        /// <summary>
        /// List of categories in this restaurant
        /// </summary>
        public List<CategoryDTO> Categories { get; set; } = [];

        /// <summary>
        /// Average rating of the restaurant from all reviews (1-5)
        /// </summary>
        public double? AverageRating { get; set; }
    }
}