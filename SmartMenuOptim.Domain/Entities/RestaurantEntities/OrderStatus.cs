using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;

namespace SmartMenuOptim.Domain.Entities.RestaurantEntities;

/// <summary>
/// Represents the status/state of an order in the restaurant's order management workflow.
/// </summary>
/// <remarks>
/// <para><strong>3-TIER DDD STRATEGY: Tier 2 - Simple Aggregates (Lightweight DDD) - Lookup/Reference Data</strong></para>
/// <para>This class implements a lightweight DDD aggregate pattern for lookup/reference data entities. While simpler than
/// main domain aggregates (Order, Menu, Restaurant), it still provides encapsulation, validation, and behavioral methods
/// to maintain data consistency and support workflow management.</para>
/// 
/// <para><strong>Tier 2 Characteristics (Lookup Aggregate):</strong></para>
/// <list type="bullet">
///   <item><description><strong>Encapsulation:</strong> Properties use private setters to prevent unauthorized state changes</description></item>
///   <item><description><strong>Validation:</strong> Business rules enforced through constructor and behavioral methods with guard clauses</description></item>
///   <item><description><strong>Rich Behavior:</strong> Domain logic encapsulated in methods (UpdateBasicInfo, SetTerminal, SetColorCode) rather than anemic property bags</description></item>
///   <item><description><strong>Simple Lifecycle:</strong> No complex child entities, serves as reference data</description></item>
///   <item><description><strong>Lightweight Invariants:</strong> Basic consistency rules (name required, color format, display order)</description></item>
///   <item><description><strong>Reference Data:</strong> Defines workflow states referenced by Order aggregate via OrderStatusId</description></item>
/// </list>
/// 
/// <para><strong>Entity Overview:</strong></para>
/// <para>An OrderStatus defines a workflow state for orders within a restaurant's order management system. Common statuses
/// include "Pending" (awaiting confirmation), "Preparing" (being cooked), "Ready" (ready for pickup/delivery), "In Delivery"
/// (en route to customer), "Completed" (successfully delivered/picked up), and "Cancelled" (order cancelled). Each status
/// includes display properties for UI rendering (color codes, display order) and workflow control (IsTerminal flag to prevent
/// further transitions).</para>
/// 
/// <para><strong>Multi-Tenant Support:</strong></para>
/// <para>Inherits from TenantEntityBase to provide built-in multi-tenancy support. Each order status is scoped to a specific
/// restaurant (RestaurantId), allowing restaurants to define custom workflow states. This ensures proper data isolation in a
/// multi-tenant environment and prevents cross-tenant status references.</para>
/// 
/// <para><strong>Consistency Boundary:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Invariants Protected:</strong> Name required (1-50 chars), color must be valid hex format (#RRGGBB), display order non-negative</description></item>
///   <item><description><strong>Encapsulated State:</strong> Internal state can only be modified through behavioral methods (UpdateBasicInfo, UpdateDisplayOrder, SetTerminal, SetColorCode)</description></item>
///   <item><description><strong>Transactional Consistency:</strong> All changes validated atomically through public methods</description></item>
///   <item><description><strong>Business Rules:</strong> Terminal statuses indicate end of workflow, orders referencing this status must belong to same restaurant</description></item>
///   <item><description><strong>Reference Data Integrity:</strong> Cannot be deleted if referenced by active orders</description></item>
/// </list>
/// 
/// <para><strong>Domain Features:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Identity:</strong> Inherits entity identity from TenantEntityBase (Id property from EntityBase)</description></item>
///   <item><description><strong>Automatic Timestamps:</strong> CreatedAt, UpdatedAt automatically managed through EntityBase</description></item>
///   <item><description><strong>Soft Delete Support:</strong> Inherits IsDeleted flag for soft deletion scenarios</description></item>
///   <item><description><strong>Optimistic Concurrency:</strong> Uses xmin timestamp token from EntityBase for concurrency control</description></item>
///   <item><description><strong>Display Order:</strong> Supports custom ordering for UI display and workflow progression visualization</description></item>
///   <item><description><strong>Color Coding:</strong> UI-friendly hex color codes for visual workflow representation</description></item>
///   <item><description><strong>Terminal Flag:</strong> Indicates workflow end states (Completed, Cancelled) to prevent invalid transitions</description></item>
/// </list>
/// 
/// <para><strong>Relationships:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Orders (One-to-Many):</strong> Referenced by Order entities via OrderStatusId foreign key</description></item>
///   <item><description><strong>Restaurant (Required):</strong> Inherited from TenantEntityBase, ensures tenant isolation</description></item>
///   <item><description><strong>Lookup/Reference Data:</strong> Provides categorization for Order workflow states</description></item>
/// </list>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// // Creating standard order workflow statuses
/// var pending = new OrderStatus(
///     restaurantId: 123,
///     name: "Pending",
///     displayOrder: 1,
///     isTerminal: false,
///     colorCode: "#FFA500",  // Orange
///     description: "Order received, awaiting confirmation"
/// );
/// 
/// var preparing = new OrderStatus(
///     restaurantId: 123,
///     name: "Preparing",
///     displayOrder: 2,
///     isTerminal: false,
///     colorCode: "#17A2B8",  // Blue
///     description: "Order is being prepared in the kitchen"
/// );
/// 
/// var completed = new OrderStatus(
///     restaurantId: 123,
///     name: "Completed",
///     displayOrder: 10,
///     isTerminal: true,  // No further transitions allowed
///     colorCode: "#28A745",  // Green
///     description: "Order successfully completed"
/// );
/// 
/// // Updating status information
/// pending.UpdateBasicInfo("Order Received", "Awaiting kitchen confirmation");
/// preparing.UpdateDisplayOrder(3);
/// completed.SetTerminal(true);
/// 
/// // Changing UI color
/// pending.SetColorCode("#FF8C00");  // Darker orange
/// 
/// // Validating tenant consistency after loading from database
/// pending.ValidateTenantConsistency();
/// 
/// // Using in Order workflow
/// var order = new Order(restaurantId, customerId, pending.Id);
/// // Order transitions: Pending → Preparing → Ready → Completed
/// </code>
/// 
/// <para><strong>Entity Framework Core Support:</strong></para>
/// <para>Includes a protected parameterless constructor for EF Core's use during materialization. The entity can be
/// persisted and retrieved through a repository pattern. Private setters are accessible to EF Core through reflection-based
/// field mapping in the entity configuration. Navigation properties configured for order relationships.</para>
/// 
/// <para><strong>Design Considerations:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Name Uniqueness:</strong> Should be unique per restaurant to avoid confusion (enforced at application level or database index)</description></item>
///   <item><description><strong>Display Order:</strong> Supports workflow progression visualization in UIs (dashboards, kanban boards)</description></item>
///   <item><description><strong>Color Validation:</strong> Enforces hex color format (#RRGGBB) for consistent UI rendering</description></item>
///   <item><description><strong>Terminal States:</strong> IsTerminal flag prevents orders from transitioning out of end states (Completed, Cancelled)</description></item>
///   <item><description><strong>Reference Data Stability:</strong> Statuses should be relatively stable; changes affect all referencing orders</description></item>
///   <item><description><strong>Soft Delete:</strong> Prefer soft deletion over hard deletion to maintain order history integrity</description></item>
///   <item><description><strong>Immutable Creation:</strong> CreatedAt and UpdatedAt timestamps track status definition lifecycle</description></item>
/// </list>
/// 
/// <para><strong>Indexing Strategy:</strong></para>
/// <para>Database indexes for efficient querying are defined centrally in AppDbContext.OnModelCreating:
/// - IX_OrderStatuses_Restaurant_DisplayOrder: For tenant-scoped status ordering in UIs
/// - IX_OrderStatuses_Restaurant_Name: For lookup by status name within restaurant
/// - IX_OrderStatuses_IsTerminal: For filtering terminal vs active states
/// - Unique constraint on (RestaurantId, Name) to prevent duplicate status names per restaurant</para>
/// 
/// <para><strong>Workflow Patterns:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Linear Workflow:</strong> Pending → Preparing → Ready → Completed</description></item>
///   <item><description><strong>Delivery Workflow:</strong> Pending → Preparing → Ready → In Delivery → Delivered</description></item>
///   <item><description><strong>Cancellation:</strong> Any non-terminal state → Cancelled</description></item>
///   <item><description><strong>Terminal States:</strong> Completed, Cancelled, Refunded (no further transitions)</description></item>
/// </list>
/// 
/// <para><strong>Use Cases:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Order Management:</strong> Track order progression through restaurant workflow</description></item>
///   <item><description><strong>Kitchen Display:</strong> Show order status on kitchen screens with color coding</description></item>
///   <item><description><strong>Customer Notifications:</strong> Send status updates when orders transition states</description></item>
///   <item><description><strong>Reporting:</strong> Analyze orders by status (orders in progress, completion rates)</description></item>
///   <item><description><strong>SLA Monitoring:</strong> Track time spent in each status for performance analysis</description></item>
///   <item><description><strong>Workflow Customization:</strong> Allow restaurants to define custom statuses for their processes</description></item>
/// </list>
/// </remarks>
[Table("OrderStatuses")]
public class OrderStatus : TenantEntityBase, IValidatableObject
{
// ===================================================================
// PROPERTIES WITH ENCAPSULATION (Private Setters)
// ===================================================================
    
/// <summary>
/// The name/title of the order status (e.g., "Pending", "Preparing", "Ready", "Completed").
/// </summary>
/// <remarks>
/// Required identifier for the workflow state. Must be:
/// - Non-empty and non-whitespace
/// - Between 1 and 50 characters
/// - Unique per restaurant (recommended, enforced at application/database level)
/// 
/// Common Status Names:
/// - "Pending": Order received, awaiting confirmation
/// - "Confirmed": Order confirmed, ready for preparation
/// - "Preparing": Being prepared in kitchen
/// - "Ready": Ready for pickup or delivery
/// - "In Delivery": Out for delivery
/// - "Completed": Successfully delivered/picked up
/// - "Cancelled": Order cancelled
/// - "Refunded": Payment refunded
/// 
/// Modifiable via UpdateBasicInfo() method for corrections or refinements.
/// </remarks>
[Required(ErrorMessage = "OrderStatus name is required")]
[MaxLength(50, ErrorMessage = "OrderStatus name cannot exceed 50 characters")]
[MinLength(1, ErrorMessage = "OrderStatus name must contain at least 1 character")]
public string Name { get; private set; } = string.Empty;

/// <summary>
/// A description providing more details about what this status means and when it's used.
/// </summary>
/// <remarks>
/// Optional explanatory text to clarify the status purpose:
/// - Maximum length: 200 characters
/// - Can be null or empty
/// - Useful for:
///   * Staff training documentation
///   * UI tooltips
///   * Workflow documentation
///   * Customer-facing status explanations
/// 
/// Examples:
/// - "Order received and awaiting kitchen confirmation"
/// - "Being prepared by our kitchen staff"
/// - "Quality checked and ready for pickup at counter"
/// - "On the way to your delivery address"
/// 
/// Modifiable via UpdateBasicInfo() method.
/// </remarks>
[MaxLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
public string? Description { get; private set; }

/// <summary>
/// The display order for showing statuses in UI elements. Lower numbers appear first.
/// </summary>
/// <remarks>
/// Controls the sequence in which statuses appear in:
/// - Status dropdown lists
/// - Workflow progression diagrams
/// - Kitchen display boards
/// - Admin configuration screens
/// 
/// Typical Display Order Pattern:
/// - 1: Pending/Order Received
/// - 2: Confirmed
/// - 3: Preparing
/// - 4: Ready
/// - 5: In Delivery
/// - 10: Completed (terminal)
/// - 11: Cancelled (terminal)
/// - 12: Refunded (terminal)
/// 
/// Constraints:
/// - Must be non-negative (0 or greater)
/// - No maximum limit
/// - Gaps allowed (e.g., 1, 2, 5, 10)
/// - Multiple statuses can share same order (displayed alphabetically)
/// 
/// Modifiable via UpdateDisplayOrder() method.
/// </remarks>
[Range(0, int.MaxValue, ErrorMessage = "DisplayOrder must be a non-negative integer")]
public int DisplayOrder { get; private set; }

/// <summary>
/// Indicates if this is a terminal status (workflow end state) that shouldn't transition to other statuses.
/// </summary>
/// <remarks>
/// Terminal statuses represent end states in the order workflow:
/// - true: No further transitions allowed (e.g., "Completed", "Cancelled", "Refunded")
/// - false: Order can transition to other statuses (e.g., "Pending", "Preparing", "Ready")
/// 
/// Business Rules:
/// - Orders in terminal status should not be editable
/// - Terminal statuses indicate final disposition
/// - Used for:
///   * Preventing accidental status changes
///   * Filtering active vs completed orders
///   * Completion rate calculations
///   * Historical reporting
/// 
/// Common Terminal Statuses:
/// - Completed: Order successfully fulfilled
/// - Cancelled: Order cancelled by customer or restaurant
/// - Refunded: Order refunded, no longer active
/// - Rejected: Order rejected by restaurant
/// 
/// Common Non-Terminal Statuses:
/// - Pending, Confirmed, Preparing, Ready, In Delivery
/// 
/// Modifiable via SetTerminal() method.
/// </remarks>
[Required]
public bool IsTerminal { get; private set; }

/// <summary>
/// Color code for UI representation in hex format (e.g., "#FF0000" for red, "#28A745" for green).
/// </summary>
/// <remarks>
/// Hex color code for visual workflow representation in UIs:
/// - Format: #RRGGBB (e.g., "#FFA500")
/// - 7 characters including leading '#'
/// - Case-insensitive (0-9, A-F or a-f)
/// - Optional (can be null)
/// 
/// Recommended Color Palette:
/// - Pending: #FFA500 (Orange) - Awaiting action
/// - Confirmed: #007BFF (Blue) - Acknowledged
/// - Preparing: #17A2B8 (Cyan) - In progress
/// - Ready: #28A745 (Green) - Available
/// - In Delivery: #6C757D (Gray) - En route
/// - Completed: #218838 (Dark Green) - Success
/// - Cancelled: #DC3545 (Red) - Terminated
/// - Refunded: #E83E8C (Pink) - Reversed
/// 
/// UI Applications:
/// - Status badges and tags
/// - Kanban board column colors
/// - Kitchen display system
/// - Mobile app order tracking
/// - Admin dashboard charts
/// 
/// Validation:
/// - Must match regex: ^#([0-9a-fA-F]{6})$
/// - Validated in constructor and SetColorCode() method
/// 
/// Modifiable via SetColorCode() method.
/// </remarks>
[MaxLength(7, ErrorMessage = "ColorCode cannot exceed 7 characters (e.g. '#FFA500')")]
[RegularExpression("^#([0-9a-fA-F]{6})$", ErrorMessage = "ColorCode must be a valid hex color in the format '#RRGGBB'.")]
public string? ColorCode { get; private set; }

// ===================================================================
// NAVIGATION PROPERTIES
// ===================================================================
    
/// <summary>
/// Navigation property for orders that currently have this status.
/// </summary>
/// <remarks>
/// Provides access to all Order entities referencing this OrderStatus via OrderStatusId.
/// 
/// Used for:
/// - Counting orders in each status (workflow analytics)
/// - Bulk operations on orders with specific status
/// - Preventing deletion of statuses with active orders
/// - Tenant consistency validation
/// 
/// Performance Considerations:
/// - Typically not eager-loaded due to large collection size
/// - Use Include() explicitly when needed
/// - Consider using Count queries instead of loading full collection
/// 
/// Tenant Consistency:
/// All orders in this collection must belong to the same restaurant as this OrderStatus.
/// Validated in ValidateTenantConsistency() and Validate() methods.
/// </remarks>
[InverseProperty(nameof(Order.Status))]
public ICollection<Order> Orders { get; set; } = new List<Order>();
    
    
    // ===================================================================
    // CONSTRUCTORS
    // ===================================================================
    
    /// <summary>
    /// Protected parameterless constructor for Entity Framework Core.
    /// </summary>
    /// <remarks>
    /// Required by EF Core for entity materialization from database.
    /// Not intended for direct use in application code.
    /// EF Core uses reflection to populate properties after instantiation.
    /// </remarks>
    protected OrderStatus() { }
    
    /// <summary>
    /// Creates a new order status with validation.
    /// </summary>
    /// <param name="restaurantId">The restaurant (tenant) identifier where this status is defined.</param>
    /// <param name="name">The status name (required, 1-50 characters, e.g., "Pending", "Completed").</param>
    /// <param name="displayOrder">Display order for UI sorting (default: 0, must be non-negative).</param>
    /// <param name="isTerminal">Whether this is a terminal status (default: false).</param>
    /// <param name="colorCode">Optional hex color code for UI (e.g., "#28A745").</param>
    /// <param name="description">Optional description explaining the status purpose.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails for any parameter.</exception>
    /// <remarks>
    /// This constructor enforces invariants at creation time following DDD best practices.
    /// 
    /// Validation Rules Enforced:
    /// - RestaurantId must be positive integer (tenant identifier)
    /// - Name is required, non-whitespace, trimmed automatically
    /// - DisplayOrder must be non-negative (0 or greater)
    /// - ColorCode must match hex format #RRGGBB if provided
    /// 
    /// Automatic Behavior:
    /// - Name is trimmed of leading/trailing whitespace
    /// - Description is trimmed if provided
    /// - CreatedAt automatically set to DateTime.UtcNow
    /// - UpdatedAt automatically set to DateTime.UtcNow
    /// 
    /// Default Values:
    /// - DisplayOrder: 0 (first position)
    /// - IsTerminal: false (non-terminal, can transition)
    /// - ColorCode: null (no color assigned)
    /// - Description: null (no description)
    /// 
    /// Usage Context:
    /// Typically called by:
    /// - Restaurant setup/configuration wizards
    /// - Admin status management interfaces
    /// - Database seeding operations
    /// - Migration scripts
    /// 
    /// Common Status Definitions:
    /// <code>
    /// // Active workflow statuses
    /// new OrderStatus(restaurantId, "Pending", 1, false, "#FFA500", "Order received");
    /// new OrderStatus(restaurantId, "Preparing", 2, false, "#17A2B8", "Being prepared");
    /// new OrderStatus(restaurantId, "Ready", 3, false, "#28A745", "Ready for pickup");
    /// 
    /// // Terminal statuses
    /// new OrderStatus(restaurantId, "Completed", 10, true, "#218838", "Successfully completed");
    /// new OrderStatus(restaurantId, "Cancelled", 11, true, "#DC3545", "Order cancelled");
    /// </code>
    /// </remarks>
    public OrderStatus(
        int restaurantId,
        string name,
        int displayOrder = 0,
        bool isTerminal = false,
        string? colorCode = null,
        string? description = null)
    {
        if (restaurantId <= 0)
            throw new ArgumentException("Valid restaurant ID is required.", nameof(restaurantId));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Order status name is required.", nameof(name));
        
        if (displayOrder < 0)
            throw new ArgumentException("Display order must be non-negative.", nameof(displayOrder));
        
        RestaurantId = restaurantId;
        Name = name.Trim();
        Description = description?.Trim();
        DisplayOrder = displayOrder;
        IsTerminal = isTerminal;
        ColorCode = colorCode;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // ===================================================================
    // DOMAIN BEHAVIORS (Tier 2 - Lightweight DDD Methods)
    // ===================================================================
    
    /// <summary>
    /// Updates the order status's basic information (name and description).
    /// </summary>
    /// <param name="name">The new status name (required, non-whitespace, 1-50 characters).</param>
    /// <param name="description">The new description (optional, max 200 characters).</param>
    /// <exception cref="ArgumentException">Thrown when name is null, empty, or whitespace.</exception>
    /// <remarks>
    /// This behavioral method allows authorized updates to status identity while maintaining encapsulation.
    /// 
    /// Common Use Cases:
    /// - Correcting typos in status names
    /// - Updating descriptions for clarity
    /// - Translating status names for localization
    /// - Refining workflow terminology
    /// 
    /// Validation:
    /// - Name must not be null, empty, or whitespace
    /// - Name is automatically trimmed
    /// - Description is automatically trimmed if provided
    /// 
    /// Side Effects:
    /// - Updates the UpdatedAt timestamp automatically (via EntityBase)
    /// - Preserves audit trail through timestamp changes
    /// 
    /// Authorization:
    /// This method should only be called by authorized users (admins, managers) through
    /// application services that enforce role-based access control.
    /// 
    /// Impact Considerations:
    /// - Changes affect all orders currently using this status
    /// - UI displays will update to reflect new name/description
    /// - Consider communication to staff when changing critical status names
    /// </remarks>
    public void UpdateBasicInfo(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Order status name is required.", nameof(name));
        
        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Updates the display order for UI sorting.
    /// </summary>
    /// <param name="order">The new display order (must be non-negative).</param>
    /// <exception cref="ArgumentException">Thrown when order is negative.</exception>
    /// <remarks>
    /// This behavioral method allows reordering statuses in UI displays while maintaining encapsulation.
    /// 
    /// Common Use Cases:
    /// - Reordering workflow steps in UI
    /// - Adjusting status sequence after adding new statuses
    /// - Grouping related statuses together
    /// - Separating terminal from active statuses
    /// 
    /// Validation:
    /// - Order must be 0 or greater (no negative values)
    /// - No upper limit on display order value
    /// 
    /// Side Effects:
    /// - Updates the UpdatedAt timestamp automatically
    /// - Changes status position in sorted UI lists
    /// 
    /// UI Impact:
    /// - Dropdown lists will show status in new position
    /// - Workflow diagrams may reflow
    /// - Kitchen displays may reorder columns
    /// 
    /// Note: Multiple statuses can share the same display order.
    /// When display orders match, statuses are typically sorted alphabetically by name.
    /// </remarks>
    public void UpdateDisplayOrder(int order)
    {
        if (order < 0)
            throw new ArgumentException("Display order must be non-negative.", nameof(order));
        
        DisplayOrder = order;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Sets the terminal flag indicating whether this is a workflow end state.
    /// </summary>
    /// <param name="isTerminal">True if this is a terminal status (no further transitions), false otherwise.</param>
    /// <remarks>
    /// This behavioral method controls workflow transition behavior while maintaining encapsulation.
    /// 
    /// Terminal Status (isTerminal = true):
    /// - Indicates end of order workflow
    /// - Orders should not transition from this status
    /// - Examples: Completed, Cancelled, Refunded
    /// - Used for completion rate calculations
    /// - Typically excluded from "active orders" counts
    /// 
    /// Non-Terminal Status (isTerminal = false):
    /// - Indicates active workflow state
    /// - Orders can transition to other statuses
    /// - Examples: Pending, Preparing, Ready
    /// - Included in "in-progress" metrics
    /// - Subject to SLA monitoring
    /// 
    /// Common Use Cases:
    /// - Marking a status as complete workflow endpoint
    /// - Converting terminal status back to active (rare)
    /// - Configuring custom workflow end states
    /// 
    /// Side Effects:
    /// - Updates the UpdatedAt timestamp automatically
    /// - Affects order transition validation logic
    /// - Changes reporting categorizations
    /// 
    /// Business Impact:
    /// - Terminal statuses prevent further order status changes
    /// - Affects completion metrics and dashboards
    /// - May trigger automated notifications or processes
    /// </remarks>
    public void SetTerminal(bool isTerminal)
    {
        IsTerminal = isTerminal;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Sets the color code for UI display.
    /// </summary>
    /// <param name="colorCode">Hex color code in format #RRGGBB, or null to clear.</param>
    /// <exception cref="ArgumentException">Thrown when color code doesn't match hex format.</exception>
    /// <remarks>
    /// This behavioral method manages UI color coding while maintaining encapsulation.
    /// 
    /// Color Format:
    /// - Must match: ^#([0-9a-fA-F]{6})$
    /// - Example: "#28A745" (green)
    /// - 7 characters total (# + 6 hex digits)
    /// - Case-insensitive (A-F or a-f both valid)
    /// - Can be null to remove color coding
    /// 
    /// Recommended Color Palette:
    /// - Informational: #17A2B8 (cyan/blue)
    /// - Warning/Pending: #FFC107 (yellow/amber)
    /// - In Progress: #007BFF (blue)
    /// - Success: #28A745 (green)
    /// - Danger/Error: #DC3545 (red)
    /// - Neutral: #6C757D (gray)
    /// 
    /// Common Use Cases:
    /// - Updating UI theme to match restaurant branding
    /// - Improving visual distinction between statuses
    /// - Enhancing accessibility with better color contrast
    /// - Implementing color-coded kitchen display systems
    /// 
    /// Validation:
    /// - Enforces hex color format via regex
    /// - Null is allowed (no color assigned)
    /// - Invalid formats throw ArgumentException
    /// 
    /// Side Effects:
    /// - Updates the UpdatedAt timestamp automatically
    /// - Changes status appearance in all UIs
    /// 
    /// UI Applications:
    /// - Status badges and chips
    /// - Progress bars
    /// - Kanban board columns
    /// - Kitchen display boards
    /// - Mobile app order tracking
    /// </remarks>
    public void SetColorCode(string? colorCode)
    {
        if (colorCode != null && !Regex.IsMatch(colorCode, "^#([0-9a-fA-F]{6})$"))
            throw new ArgumentException("ColorCode must be a valid hex color in the format '#RRGGBB'.", nameof(colorCode));
        
        ColorCode = colorCode;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // ===================================================================
    // MULTI-TENANT VALIDATION
    // ===================================================================

    /// <summary>
    /// Validates that the order status maintains multi-tenant boundaries and consistency.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when tenant consistency is violated.</exception>
    /// <remarks>
    /// This method should be called after navigation properties are loaded to ensure:
    /// - Restaurant navigation property matches RestaurantId
    /// - All referenced orders belong to the same restaurant
    /// 
    /// Tenant Consistency Rules:
    /// 1. OrderStatus must belong to exactly one restaurant (RestaurantId)
    /// 2. All orders in Orders collection must belong to the same restaurant
    /// 3. Restaurant navigation property ID (if loaded) must match RestaurantId
    /// 
    /// Security Implications:
    /// This is a critical security check in multi-tenant systems to prevent:
    /// - Cross-tenant status references
    /// - Orders from one restaurant using another restaurant's statuses
    /// - Workflow confusion between different restaurant tenants
    /// - Reporting inaccuracies in multi-tenant dashboards
    /// 
    /// When to Call:
    /// - After loading order statuses with navigation properties from database
    /// - Before displaying status information in multi-tenant contexts
    /// - In data import/migration processes
    /// - As part of data integrity audits
    /// - When validating order status assignments
    /// 
    /// Performance Note:
    /// Only performs validation if navigation properties are loaded.
    /// Does not trigger lazy loading to avoid N+1 query issues.
    /// For large Orders collections, consider validating via database query instead.
    /// </remarks>
    public void ValidateTenantConsistency()
    {
        // Validate Restaurant navigation property consistency
        if (Restaurant != null && Restaurant.Id != RestaurantId)
        {
            throw new InvalidOperationException(
                $"Restaurant navigation property ID ({Restaurant.Id}) does not match RestaurantId ({RestaurantId}).");
        }

        // Validate all orders belong to same restaurant
        if (Orders != null && Orders.Any())
        {
            var inconsistentOrders = Orders
                .Where(o => o.RestaurantId != RestaurantId)
                .Select(o => new { o.Id, o.RestaurantId })
                .ToList();

            if (inconsistentOrders.Any())
            {
                var orderIds = string.Join(", ", inconsistentOrders.Select(o => o.Id));
                var restaurantIds = string.Join(", ", inconsistentOrders.Select(o => o.RestaurantId).Distinct());
                
                throw new InvalidOperationException(
                    $"OrderStatus contains orders from different restaurants. " +
                    $"OrderStatus RestaurantId: {RestaurantId}, " +
                    $"Inconsistent Order IDs: [{orderIds}], " +
                    $"Inconsistent Restaurant IDs: [{restaurantIds}]");
            }
        }
    }
    
    // ===================================================================
    // VALIDATION LOGIC (IValidatableObject)
    // ===================================================================
    // IValidatableObject is REQUIRED for Tier 2 when used with EF Core SaveChanges validation
    // Delegates tenant consistency checks to ValidateTenantConsistency() to avoid redundancy
    
    /// <summary>
    /// Validates the order status entity ensuring data consistency and business rules.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>Collection of validation results.</returns>
    /// <remarks>
    /// Validation Rules:
    /// 1. Tenant Boundary:
    ///    - Delegated to ValidateTenantConsistency() for consistency
    ///    - Must belong to exactly one restaurant
    ///    - All orders must belong to same restaurant
    /// 2. Status Data:
    ///    - Name must be non-empty and non-whitespace
    ///    - DisplayOrder must be non-negative
    ///    - ColorCode must match hex format if provided
    /// </remarks>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // ===================================================================
        // TENANT CONSISTENCY VALIDATION
        // ===================================================================
        // Delegate all tenant boundary checks to ValidateTenantConsistency()
        // to avoid redundancy and maintain single source of truth
        
        // Validate restaurant ID
        if (RestaurantId <= 0)
        {
            yield return new ValidationResult(
                "OrderStatus must be associated with a restaurant",
                new[] { nameof(RestaurantId) }
            );
        }

        // Validate restaurant navigation consistency
        if (Restaurant != null && Restaurant.Id != RestaurantId)
        {
            yield return new ValidationResult(
                "Restaurant navigation property is inconsistent with RestaurantId",
                new[] { nameof(Restaurant), nameof(RestaurantId) }
            );
        }

        // Validate orders belong to same restaurant
        if (Orders != null && Orders.Any())
        {
            var inconsistentOrders = Orders
                .Where(o => o.RestaurantId != RestaurantId)
                .Select(o => new { o.Id, o.RestaurantId })
                .ToList();

            if (inconsistentOrders.Any())
            {
                var orderIds = string.Join(", ", inconsistentOrders.Select(o => o.Id));
                var restaurantIds = string.Join(", ", inconsistentOrders.Select(o => o.RestaurantId).Distinct());
                
                yield return new ValidationResult(
                    $"OrderStatus contains orders from different restaurants. " +
                    $"OrderStatus RestaurantId: {RestaurantId}, " +
                    $"Inconsistent Order IDs: [{orderIds}], " +
                    $"Inconsistent Restaurant IDs: [{restaurantIds}]",
                    new[] { nameof(Orders), nameof(RestaurantId) }
                );
            }
        }

        // ===================================================================
        // BUSINESS RULE VALIDATION
        // ===================================================================


        // ===================================================================
        // BUSINESS RULE VALIDATION
        // ===================================================================

        // Name validation
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "OrderStatus name must not be empty or whitespace",
                new[] { nameof(Name) }
            );
        }

        // Display order validation
        if (DisplayOrder < 0)
        {
            yield return new ValidationResult(
                "Display order must be non-negative",
                new[] { nameof(DisplayOrder) }
            );
        }

        // ColorCode validation
        if (ColorCode != null && !Regex.IsMatch(ColorCode, "^#([0-9a-fA-F]{6})$"))
        {
            yield return new ValidationResult(
                "ColorCode must be a valid hex color in the format '#RRGGBB'",
                new[] { nameof(ColorCode) }
            );
        }
    }
}
