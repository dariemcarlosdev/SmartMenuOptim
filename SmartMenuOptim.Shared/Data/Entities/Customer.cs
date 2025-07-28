using System;
using System.Collections.Generic;

namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Represents a customer in the system. Can be linked to reviews, but reviews can also be anonymous.
    /// Extended for use as a User in the Customer Portal (authentication/profile).
    /// </summary>
    public class Customer : UserBase
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        /// <summary>
        /// Date the customer registered.
        /// </summary>
        public DateTime DateRegistered { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Optional role for access control (e.g., "User", "Admin").
        /// The Customer entity can handle roles related to customer-facing functionality, such as:
        /// • User: Regular customer, can browse menu, place orders, leave reviews.
        /// • PremiumUser: Customer with special privileges (e.g., discounts, loyalty rewards).
        /// • Guest: Temporary or limited-access user, may not require registration.
        /// • Blocked: Customer account is disabled or restricted due to violations.
        /// • Moderator: Can report or flag reviews, limited moderation capabilities (if you want some customers to help moderate content).
        /// </summary>
        public string? Role { get; set; }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
