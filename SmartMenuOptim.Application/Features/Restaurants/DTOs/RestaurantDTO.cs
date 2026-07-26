namespace SmartMenuOptim.Application.Features.Restaurants.DTOs;

/// <summary>
/// Data Transfer Object for Restaurant entity.
/// </summary>
/// <remarks>
/// <para><strong>Multi-Tenant Root DTO:</strong></para>
/// <para>This DTO represents the root tenant in the multi-tenant architecture.
/// Each restaurant acts as an isolated tenant with proper data isolation.</para>
/// 
/// <para><strong>Blazor CRUD Best Practices:</strong></para>
/// <list type="bullet">
///   <item><description>All properties are mutable (get/set)</description></item>
///   <item><description>Use nullable types for optional fields</description></item>
///   <item><description>Use default values for collections</description></item>
///   <item><description>Simple POCOs suitable for model binding and form editing</description></item>
/// </list>
/// 
/// <para><strong>Usage:</strong></para>
/// <para>Use this DTO for list views, cards, and basic restaurant operations.
/// For full details with menus, dishes, and categories, use <see cref="RestaurantDetailDTO"/>.</para>
/// </remarks>
public class RestaurantDTO
{
    // === Identity ===

    /// <summary>
    /// Restaurant identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the restaurant.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the restaurant.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Owner (AdminUser) identifier.
    /// </summary>
    public int OwnerId { get; set; }

    // === Contact Information ===

    /// <summary>
    /// Contact email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Physical location/address of the restaurant.
    /// </summary>
    public AddressDTO Address { get; set; } = new();

    // === Configuration ===

    /// <summary>
    /// IANA timezone identifier (e.g., "America/New_York", "Europe/London").
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// Maximum number of simultaneous orders the restaurant can handle.
    /// </summary>
    public int MaxSimultaneousOrders { get; set; }

    // === Operating Status ===

    /// <summary>
    /// Whether the restaurant is currently accepting orders.
    /// </summary>
    public bool IsAcceptingOrders { get; set; }

    /// <summary>
    /// Business hours for each day of the week.
    /// </summary>
    public List<BusinessHoursDTO> BusinessHours { get; set; } = [];

    // === Audit Fields ===

    /// <summary>
    /// Date and time when the restaurant was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date and time when the restaurant was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Indicates if the restaurant has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // === Computed Properties for Display ===

    /// <summary>
    /// Gets the current operational status for display.
    /// </summary>
    public string StatusDisplay => IsAcceptingOrders ? "Open" : "Closed";
}
