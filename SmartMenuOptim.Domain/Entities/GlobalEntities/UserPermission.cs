using System.ComponentModel.DataAnnotations;
using SmartMenuOptim.Domain.Features.Restaurants;

namespace SmartMenuOptim.Domain.Entities.GlobalEntities
{
    /// <summary>
    /// Represents a permission granted to an application user within a specific restaurant context.
    /// </summary>
    /// <remarks>UserPermission encapsulates information about individual permissions, including their name,
    /// description, expiration, and auditing details. It supports tenant-specific access control by associating
    /// permissions with both users and restaurants. This class is typically used to manage and query user access rights
    /// in multi-tenant restaurant management systems.</remarks>
    public class UserPermission : EntityBase
    {
        // === Standalone Properties ===

        /// <summary>
        /// The name/identifier of the permission (e.g., "ManageMenus", "ViewOrders")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }



        /// <summary>
        /// Detailed description of what this permission allows ( e.g., "Allows managing menu items and categories", "Allows viewing order details", etc)
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// The specific area or module this permission applies to (e.g., "Menu", "Orders", "Reports")
        /// </summary>
        [MaxLength(100)]
        public string? Area { get; set; }

        /// <summary>
        /// Optional expiration date for temporary permissions
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Identifier of the admin/user who granted this permission
        /// </summary>
        [MaxLength(450)]
        public string? GrantedBy { get; set; }

        /// <summary>
        /// When the permission was granted
        /// </summary>
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

        // === Access Level Control ===

        /// <summary>
        /// The level of access granted by this permission (e.g., Read, Write, Admin)
        /// </summary>
        [Required]
        public AccessLevel AccessLevel { get; set; }

        // === Relationship Properties (Foreign Keys) ===

        /// <summary>
        /// Gets or sets the identifier of the restaurant associated with this entity.
        /// </summary>
        /// <remarks>This property serves as a foreign key reference to the Restaurant entity, enabling
        /// tenant-specific permissions and data segregation.</remarks>
        public required int RestaurantId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the associated application user.
        /// </summary>
        /// <remarks>This property serves as a foreign key reference to the related ApplicationUser
        /// entity. It is typically used to establish relationships between user-specific data and the corresponding
        /// user record.</remarks>
        [Required]
        public required string ApplicationUserId { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property to the Restaurant entity
        /// </summary>
        public Restaurant? Restaurant { get; set; }

        /// <summary>
        /// Navigation property to the ApplicationUser entity
        /// </summary>
        public ApplicationUser? ApplicationUser { get; set; }

        // === Business Logic Methods ===

        /// <summary>
        /// Checks if the permission is currently valid (active and not expired)
        /// </summary>
        public bool IsValid()
        {
            if (!IsActive) return false;
            if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow) return false;
            return true;
        }

        /// <summary>
        /// Checks if this permission grants the specified access level or higher
        /// </summary>
        public bool HasAccessLevel(AccessLevel requiredLevel)
        {
            return AccessLevel >= requiredLevel;
        }

    }

    /// <summary>
    /// Represents different levels of access that can be granted by a permission for granular control.
    /// </summary>
    public enum AccessLevel
    {
        None = 0,
        Read = 1,
        Write = 2,
        Admin = 3
    }



}