using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;
using System;
using System.Collections.Generic;

namespace SmartMenuOptim.Shared.Data.Entities.GlobalEntities
{
    /// <summary>
    /// Represents a customer in the system. Can be linked to reviews, but reviews can also be anonymous.
    /// Extended for use as a User in the Customer Portal (authentication/profile).
    /// </summary>
    /// <remarks>
    /// Shared Tenancy Model: This entity is global (not tenant-specific).
    /// Customers are shared across all restaurants (tenants) and can interact with multiple restaurants
    /// using the same account. Relationships such as reviews, orders, or reservations link the customer
    /// to a specific restaurant, ensuring proper data association in a multi-tenant environment.
    /// </remarks>
    [Table("Customers")]
    /// <summary>
    /// Composite unique index on Email + Username:
    /// - Ensures uniqueness and prevents accidental duplicates where email/username are swapped
    /// - Optimizes authentication queries that check both email and username
    /// </summary>
    [Index(nameof(Email), nameof(Username), IsUnique = true, Name = "IX_Customers_Email_Username_Unique")]
    /// <summary>
    /// Composite index for phone lookups and verification status:
    /// - Optimizes phone-based lookups (support, SMS verification)
    /// - Includes phone verification flag to quickly find unverified numbers
    /// </summary
    [Index(nameof(PhoneNumber), nameof(PhoneNumberConfirmed), Name = "IX_Customers_Phone_Verified")]
    /// <summary>
    /// Composite index for fast authentication/user lookups
    /// - Speeds up queries filtering by PhoneNumber and PhoneNumberConfirmed
    /// - Supports scenarios where active users are queried by username
    /// </summary>
    [Index(nameof(Username), nameof(IsActive), Name = "IX_Customers_Username_Active")]
    /// <summary>
    /// Composite index for activity and registration queries:
    /// - Speeds up analytics queries that filter by DateRegistered and LastActivityDate
    /// - Supports queries for recently active customers
    /// </summary>
    [Index(nameof(DateRegistered), nameof(LastActivityDate), Name = "IX_Customers_Registered_Activity")]
    public class Customer : UserBase
    {
        // === Personal Information ===

        /// <summary>
        /// Full name of the customer.
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s-']+$", ErrorMessage = "Name can only contain letters, spaces, hyphens and apostrophes")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Customer's preferred language for communications (ISO 639-1 code)
        /// </summary>
        [MaxLength(2)]
        [RegularExpression(@"^[a-z]{2}$", ErrorMessage = "Language code must be a valid ISO 639-1 code")]
        public string? PreferredLanguage { get; set; }

        /// <summary>
        /// Customer's timezone (IANA timezone identifier)
        /// </summary>
        [MaxLength(50)]
        public string? TimeZoneId { get; set; }

        // === Account Information ===

        /// <summary>
        /// Date when the customer registered (UTC).
        /// </summary>
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime DateRegistered { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date of the customer's last activity (UTC).
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime? LastActivityDate { get; set; }

        /// <summary>
        /// Marketing preferences flags
        /// </summary>
        public bool AcceptsMarketing { get; set; }

        /// <summary>
        /// Notes about the customer (internal use)
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        // === Contact Information ===

        /// <summary>
        /// Customer's phone number
        /// </summary>
        [Phone]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Whether the phone number has been verified
        /// </summary>
        public bool PhoneNumberConfirmed { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property for all reviews written by this customer.
        /// </summary>
        [InverseProperty(nameof(Review.Customer))]
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        /// <summary>
        /// Gets or sets the collection of orders associated with this customer.
        /// </summary>
        /// <remarks>
        /// Modifications to the collection, such as adding or removing orders, will affect the
        /// set of orders linked to this customer. The collection is initialized to an empty list by default.
        /// </remarks>
        [InverseProperty(nameof(Order.Customer))]
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        /// <summary>
        /// Gets or sets the collection of reservations associated with this customer.
        /// </summary>
        /// <remarks>
        /// Modifications to the collection, such as adding or removing reservations, will affect the
        /// set of reservations linked to this customer. The collection is initialized to an empty list by default.
        /// </remarks>
        [InverseProperty(nameof(Reservation.Customer))]
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        /// <summary>
        /// Gets or sets the collection of loyalty program associations for the customer.
        /// </summary>
        /// <remarks>
        /// Modifications to the collection, such as adding or removing loyalty associations, will affect the
        /// set of loyalty programs linked to this customer. The collection is initialized to an empty list by default.
        /// </remarks>
        [InverseProperty(nameof(CustomerLoyalty.Customer))]
        public ICollection<CustomerLoyalty> CustomerLoyalties { get; set; } = new List<CustomerLoyalty>();
    }
}
