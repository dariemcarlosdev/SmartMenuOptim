using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartMenuOptim.Domain.Aggregates.CustomerLoyaltyAggregate;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Aggregates.TableAggregate;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Entities.TenantSpecificEntities;

namespace SmartMenuOptim.Domain.Entities.ProfileEntities
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
    public class Customer : EntityBase
    {

        // === Delegated Identity Properties ===

        /*
        DESIGN NOTE: Delegated Properties vs Alternative Approaches
        
        This class uses delegated properties (Email, UserName) that forward to ApplicationUser 
        instead of alternative approaches like interfaces or base classes. Here's why:
        
        1. Delegated Properties (Current Approach):
           + Maintains single source of truth in ApplicationUser
           + Clear and explicit dependency on ApplicationUser
           + No inheritance complexity or interface overhead
           + Easy to modify or extend individual properties
           + Works well with Entity Framework's change tracking
        
        2. Interface-based Approach (Not Used):
           - Would require implementing same properties multiple times
           - Interfaces can't provide implementation, leading to code duplication
           - More complex to maintain across multiple profile types
           - Additional overhead of interface management
        
        3. Base Class Approach (Not Used):
           - Creates tight coupling through inheritance
           - Less flexible for future changes
           - Can lead to deep inheritance hierarchies
           - Potential conflicts with EntityBase inheritance
        
        The delegated approach provides the best balance of:
        - Maintainability: Changes only needed in one place
        - Flexibility: Easy to modify individual properties
        - Performance: Direct property access with no interface overhead
        - Clarity: Explicit relationship between entities
        */

        /// <summary>
        /// Gets or sets the email address of the customer.
        /// This property delegates to the associated ApplicationUser's Email property.
        /// </summary>
        [NotMapped]
        public string Email 
        { 
            get => ApplicationUser?.Email ?? string.Empty;
            set 
            {
                if (ApplicationUser != null)
                    ApplicationUser.Email = value;
            }
        }

        /// <summary>
        /// Gets or sets the username of the customer.
        /// This property delegates to the associated ApplicationUser's UserName property.
        /// </summary>
        [NotMapped]
        public string UserName
        {
            get => ApplicationUser?.UserName ?? string.Empty;
            set 
            {
                if (ApplicationUser != null)
                    ApplicationUser.UserName = value;
            }
        }

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

        // === Foreign Keys ===

        /// <summary>
        /// Foreign key to ApplicationUser. This is a string GUID that matches
        /// ApplicationUser's Id property, creating the one-to-one relationship
        /// between Customer profile and ApplicationUser identity.
        /// </summary>
        [Required]
        [MaxLength(450)] // Matches Identity's key length
        public string ApplicationUserId { get; set; } = null!;

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property to the identity `ApplicationUser`.
        /// </summary>
        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser? ApplicationUser { get; set; }

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
