using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;
using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;

/// <summary>
/// Represents a staff member who works at a restaurant. Extends UserBase for authentication
/// while maintaining restaurant-specific associations.
/// </summary>
/// <remarks>
/// Hybrid Tenancy Model: This entity combines global identity (UserBase) with tenant-specific assignment.
/// Staff members have global authentication but are primarily associated with a specific restaurant.
/// </remarks>
public class StaffMember : UserBase
{
    // === Standalone Properties ===

    /// <summary>
    /// Staff member's full name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Staff member's role in the restaurant.
    /// </summary>
    public StaffRole Role { get; set; }

    /// <summary>
    /// Date when the staff member was hired.
    /// </summary>
    public DateTime HireDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Contact email for the staff member.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    // === Tenant Association ===

    /// <summary>
    /// Primary restaurant where the staff member works.
    /// </summary>
    public int RestaurantId { get; set; }

    /// <summary>
    /// Navigation property to the associated restaurant.
    /// </summary>
    public Restaurant? Restaurant { get; set; }

    // === Additional Properties ===

    /// <summary>
    /// Staff member's work schedule and availability.
    /// </summary>
    public ICollection<StaffSchedule> Schedules { get; set; } = [];

    /// <summary>
    /// Orders handled by this staff member.
    /// </summary>
    public ICollection<Order> HandledOrders { get; set; } = [];
}

public enum StaffRole
{
    Waiter,
    Chef,
    Manager,
    Host,
    Busser,
    Bartender
}