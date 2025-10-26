using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;
using System.ComponentModel.DataAnnotations;

public abstract class TenantEntityBase
{
    // === Standalone Properties ===

    /// <summary>
    /// Primary key for the entity.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Date and time when the entity was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the entity was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if the entity is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Concurrency token for optimistic concurrency control.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // === Required Tenant Relationship ===

    /// <summary>
    /// Foreign key to the Restaurant entity. Each entity belongs to a single restaurant.
    /// </summary>
    public int RestaurantId { get; set; }

    /// <summary>
    /// Navigation property to the Restaurant this entity is associated with.
    /// </summary>
    public Restaurant? Restaurant { get; set; }
}