using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;
using System;
using System.Collections.Generic;

namespace SmartMenuOptim.Shared.Data.Entities.GlobalEntities
{
    /// <summary>
    /// Represents a customer in the system. Can be linked to reviews, but reviews can also be anonymous.
    /// Extended for use as a User in the Customer Portal (authentication/profile). Global entity can interact with multiple restaurants.
    /// </summary>
    /// <remarks>
    /// Shared Tenancy Model: This entity is global (not tenant-specific). Customers are shared across all restaurants (tenants) and can interact with multiple restaurants using the same account. Relationships such as reviews, orders, or reservations link the customer to a specific restaurant, ensuring proper data association in a multi-tenant environment.
    /// </remarks>
    public class Customer : UserBase
    {
        // === Standalone Properties ===
        /// <summary>
        /// Name of the customer.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Email address of the customer (optional).
        /// </summary>
        public string? Email { get; set; }
        /// <summary>
        /// Date the customer registered (UTC).
        /// </summary>
        public DateTime DateRegistered { get; set; } = DateTime.UtcNow;

        // === Navigation Properties ===
        /// <summary>
        /// Navigation property for all reviews written by this customer.
        /// </summary>
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
