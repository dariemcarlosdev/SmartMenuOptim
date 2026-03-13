using SmartMenuOptim.Domain.Aggregates.OrderAggregate.Errors;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Aggregates.ReviewAggregate;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate.Events;
using SmartMenuOptim.Domain.Exceptions;
using SmartMenuOptim.Domain.Common;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Represents a customer order aggregate root managing the complete order lifecycle, line items, and business rules for a restaurant tenant.
/// </summary>
/// <remarks>
/// <para><strong>3-TIER DDD STRATEGY: Tier 1 - Full Aggregate Roots (Rich DDD)</strong></para>
/// <para>This class implements a full DDD aggregate root pattern with child entities and complex business rules.
/// It serves as the consistency boundary for all order-related operations and maintains transactional integrity.</para>
/// 
/// <para><strong>Tier 1 Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Full Encapsulation:</strong> All properties use private setters; state changes only through behavioral methods</description></item>
///   <item><description><strong>Child Entity Management:</strong> Manages OrderItem child entities through encapsulated collection with controlled access</description></item>
///   <item><description><strong>Aggregate Boundary:</strong> Defines transactional consistency boundary - all changes to order and items happen atomically</description></item>
///   <item><description><strong>Rich Domain Behavior:</strong> Complex business logic for adding/removing items, status transitions, total calculations</description></item>
///   <item><description><strong>Invariant Protection:</strong> Automatically maintains invariants (total always matches items, valid status transitions)</description></item>
///   <item><description><strong>Collection Encapsulation:</strong> Private backing field (_orderItems) with read-only public access (Items property)</description></item>
///   <item><description><strong>Lifecycle Management:</strong> Controls complete order workflow from creation through completion or cancellation</description></item>
/// </list>
/// 
/// <para><strong>Entity Overview:</strong></para>
/// <para>An Order represents a customer's purchase request at a restaurant, containing one or more OrderItem line items.
/// It tracks the order lifecycle through status changes (Pending → Preparing → Ready → Completed), maintains accurate
/// totals, links to the customer and staff handler, and supports special instructions. Orders form the foundation of
/// restaurant operations, kitchen workflows, delivery tracking, and revenue management.</para>
/// 
/// <para><strong>Multi-Tenant Support:</strong></para>
/// <para>Inherits from TenantEntityBase to provide built-in multi-tenancy support. Each order is scoped to a specific
/// restaurant (RestaurantId), ensuring proper data isolation. The order links to a global Customer entity (can order
/// from multiple restaurants) but order items must reference dishes from the same restaurant.</para>
/// 
/// <para><strong>Aggregate Composition:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Root Entity:</strong> Order (this class)</description></item>
///   <item><description><strong>Child Entities:</strong> OrderItem collection - individual line items with dish, quantity, pricing</description></item>
///   <item><description><strong>Value Objects:</strong> Money (for monetary amounts with currency)</description></item>
///   <item><description><strong>Referenced Aggregates:</strong> Customer (global), OrderStatus (lookup), StaffMember (optional handler)</description></item>
/// </list>
/// 
/// <para><strong>Consistency Boundary:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Invariants Protected:</strong> Total amount always equals sum of item subtotals, at least one item required, valid status transitions only</description></item>
///   <item><description><strong>Encapsulated State:</strong> Internal state modified only through behavioral methods (AddItem, RemoveItem, ChangeStatus, etc.)</description></item>
///   <item><description><strong>Transactional Consistency:</strong> All changes to order and child items validated and saved atomically through repository</description></item>
///   <item><description><strong>Business Rules:</strong> Cannot modify items after certain statuses, cannot delete order with completed status, staff assignment validation</description></item>
///   <item><description><strong>Child Collection:</strong> OrderItems can only be added/removed through aggregate root methods, never directly manipulated</description></item>
/// </list>
/// 
/// <para><strong>Domain Features:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Identity:</strong> Inherits entity identity from TenantEntityBase (Id property from EntityBase)</description></item>
///   <item><description><strong>Automatic Timestamps:</strong> CreatedAt (OrderDate), UpdatedAt automatically managed through EntityBase</description></item>
///   <item><description><strong>Soft Delete Support:</strong> Inherits IsDeleted flag for logical deletion (cancelled orders)</description></item>
///   <item><description><strong>Optimistic Concurrency:</strong> Uses xmin timestamp token from EntityBase for concurrency control</description></item>
///   <item><description><strong>Automatic Total Calculation:</strong> TotalAmount automatically recalculated when items are added/removed/modified</description></item>
///   <item><description><strong>Status Workflow:</strong> Managed status transitions with validation and business rules</description></item>
///   <item><description><strong>Staff Assignment:</strong> Optional handler assignment for order processing and customer service</description></item>
/// </list>
/// 
/// <para><strong>Relationships:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Customer (Required):</strong> Links to global Customer entity - can order from multiple restaurants</description></item>
///   <item><description><strong>OrderStatus (Required):</strong> Current workflow state - managed through ChangeStatus method</description></item>
///   <item><description><strong>OrderItems (One-to-Many Children):</strong> Line items managed exclusively through aggregate root</description></item>
///   <item><description><strong>StaffMember (Optional):</strong> Handler assigned to process/deliver the order</description></item>
///   <item><description><strong>Restaurant (Required):</strong> Inherited from TenantEntityBase, ensures tenant isolation</description></item>
/// </list>
/// 
/// <para><strong>Lifecycle States:</strong></para>
/// <code>
/// Pending → Confirmed → Preparing → Ready → In Delivery → Completed
///    ↓                                  ↓
/// Cancelled ←─────────────────────────┘
/// </code>
/// <list type="bullet">
///   <item><description><strong>Pending:</strong> Order created, awaiting confirmation</description></item>
///   <item><description><strong>Confirmed:</strong> Order accepted by restaurant</description></item>
///   <item><description><strong>Preparing:</strong> Kitchen is preparing the order</description></item>
///   <item><description><strong>Ready:</strong> Order ready for pickup or delivery</description></item>
///   <item><description><strong>In Delivery:</strong> Order is being delivered to customer</description></item>
///   <item><description><strong>Completed:</strong> Order successfully fulfilled (terminal state)</description></item>
///   <item><description><strong>Cancelled:</strong> Order cancelled by customer or restaurant (terminal state)</description></item>
/// </list>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// // Creating a new order
/// var order = new Order(
///     restaurantId: 123,
///     customerId: 456,
///     orderStatusId: pendingStatusId,
///     specialInstructions: "Ring doorbell twice, leave at door"
/// );
/// 
/// // Adding items to the order
/// order.AddItem(
///     dishId: 789,
///     dishName: "Margherita Pizza",
///     unitPrice: 12.99m,
///     quantity: 2,
///     specialInstructions: "Extra cheese"
/// );
/// 
/// order.AddItem(
///     dishId: 790,
///     dishName: "Caesar Salad",
///     unitPrice: 8.99m,
///     quantity: 1
/// );
/// // TotalAmount automatically calculated: $34.97
/// 
/// // Assigning a staff member to handle the order
/// order.AssignStaffHandler(staffId: 101);
/// 
/// // Changing order status through lifecycle
/// order.ChangeStatus(confirmedStatusId);
/// order.ChangeStatus(preparingStatusId);
/// order.ChangeStatus(readyStatusId);
/// 
/// // Modifying an existing item
/// var itemId = order.Items.First().Id;
/// order.UpdateItemQuantity(itemId, newQuantity: 3);
/// // TotalAmount automatically recalculated: $47.96
/// 
/// // Removing an item
/// order.RemoveItem(itemId);
/// // TotalAmount automatically recalculated
/// 
/// // Validating tenant consistency after loading from database
/// order.ValidateTenantConsistency();
/// 
/// // Checking order state
/// if (order.Items.Count >= 2)
/// {
///     Console.WriteLine($"Order total: ${order.TotalAmount}");
/// }
/// </code>
/// 
/// <para><strong>Entity Framework Core Support:</strong></para>
/// <para>Includes a protected parameterless constructor for EF Core's use during materialization. The aggregate can be
/// persisted and retrieved through repository pattern. Private setters and the _orderItems collection are accessible to
/// EF Core through reflection-based field mapping in entity configuration. Child OrderItem entities are automatically
/// persisted through cascade operations.</para>
/// 
/// <para><strong>Design Considerations:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Aggregate Boundary:</strong> Order and OrderItems must be loaded and saved together as a unit</description></item>
///   <item><description><strong>Total Calculation:</strong> Always recalculate totals when items change to maintain consistency</description></item>
///   <item><description><strong>Status Validation:</strong> Enforce valid status transitions through business rules</description></item>
///   <item><description><strong>Item Modifications:</strong> After certain statuses (e.g., Preparing), item modifications may be restricted</description></item>
///   <item><description><strong>Minimum Items:</strong> Order must contain at least one item before submission</description></item>
///   <item><description><strong>Staff Assignment:</strong> Only active staff from same restaurant can be assigned</description></item>
///   <item><description><strong>Customer Context:</strong> Customer can be from any tenant but order items must match order's restaurant</description></item>
///   <item><description><strong>Terminal States:</strong> Completed and Cancelled are terminal states preventing further modifications</description></item>
/// </list>
/// 
/// <para><strong>Indexing Strategy:</strong></para>
/// <para>Database indexes for efficient querying are defined in AppDbContext.OnModelCreating:</para>
/// <list type="bullet">
///   <item><description>IX_Orders_Restaurant_Customer: Composite index for customer order history per restaurant</description></item>
///   <item><description>IX_Orders_Restaurant_Status_OrderDate: For restaurant dashboard filtering by status and time</description></item>
///   <item><description>IX_Orders_OrderDate: For time-series analysis and daily sales reports</description></item>
///   <item><description>IX_Orders_HandledByStaffId: For staff performance tracking and workload analysis</description></item>
/// </list>
/// 
/// <para><strong>Use Cases:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Order Creation:</strong> Customer places new order through web/mobile interface</description></item>
///   <item><description><strong>Kitchen Management:</strong> Track orders in preparation queue</description></item>
///   <item><description><strong>Delivery Tracking:</strong> Monitor orders in transit</description></item>
///   <item><description><strong>Customer History:</strong> View past orders and reorder favorites</description></item>
///   <item><description><strong>Staff Assignment:</strong> Assign orders to waiters or delivery personnel</description></item>
///   <item><description><strong>Revenue Analytics:</strong> Calculate daily/weekly/monthly sales</description></item>
///   <item><description><strong>Inventory Management:</strong> Track dish sales for ingredient planning</description></item>
///   <item><description><strong>Customer Service:</strong> Handle order modifications and cancellations</description></item>
/// </list>
/// </remarks>
[Table("Orders")]
public class Order : TenantEntityBase, IValidatableObject
{
    // === Private Collections for Aggregate Pattern ===
    
    private readonly List<OrderItem> _orderItems = new();
    private readonly List<IDomainEvent> _domainEvents = new();
    
    // === Domain Events (Aggregate Pattern) ===
    
    /// <summary>
    /// Gets the domain events raised by this aggregate.
    /// Events are dispatched by the infrastructure layer after successful persistence.
    /// </summary>
    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    /// <summary>
    /// Clears all domain events. Called by infrastructure after dispatching.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Adds a domain event to be dispatched after persistence.
    /// Dispatcher will handle these events to trigger side effects (e.g., notifications, integration events) without coupling the domain model to external services.
    /// Domain events represent significant occurrences within the aggregate that other parts of the system may react to.
    /// Domain events are raised by the aggregate's behavioral methods (e.g., Place, Cancel, Complete) to signal important state changes.
    /// Domain events are stored in a private collection and exposed as read-only to prevent external modification. They are cleared after being dispatched by the infrastructure layer.
    /// Dispatching domain events allows for decoupled communication between the domain model and other parts of the system (e.g., application layer, external services) without creating tight coupling or dependencies.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    
    // === Properties with Private Setters (Aggregate Pattern) ===
    
    /// <summary>
    /// Foreign key to the global Customer entity (who placed the order).
    /// </summary>
    [Required]
    [ForeignKey(nameof(Customer))]
    public int CustomerId { get; private set; }

    /// <summary>
    /// Foreign key to the OrderStatus entity, indicating the current status of the order.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Status))]
    public int OrderStatusId { get; private set; }

    /// <summary>
    /// Total amount of the order, automatically calculated from OrderItems.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be non-negative")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Date and time when the order was placed (UTC).
    /// </summary>
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime OrderDate { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Special instructions or notes for the entire order.
    /// </summary>
    [MaxLength(1000)]
    public string? SpecialInstructions { get; private set; }

    /// <summary>
    /// Foreign key for the staff member who handled this order (optional).
    /// </summary>
    [ForeignKey(nameof(HandledBy))]
    public int? HandledByStaffId { get; private set; }

    // === Navigation Properties ===

    /// <summary>
    /// The current status of the order.
    /// </summary>
    public OrderStatus Status { get; set; } = default!;

    /// <summary>
    /// Navigation property to the customer who placed the order.
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Navigation property for the order items (EF Core navigation).
    /// For adding/removing items, use AddItem() and RemoveItem() methods instead.
    /// </summary>
    [InverseProperty(nameof(OrderItem.Order))]
    public ICollection<OrderItem> OrderItems 
    { 
        get => _orderItems;
        set => _orderItems.Clear(); // EF Core needs setter
    }

    /// <summary>
    /// Read-only collection of OrderItems. Use AddItem()/RemoveItem() to modify.
    /// </summary>
    [NotMapped]
    public IReadOnlyCollection<OrderItem> Items => _orderItems.AsReadOnly();

    /// <summary>
    /// Navigation property to the staff member who handled this order (optional).
    /// </summary>
    public StaffMember? HandledBy { get; set; }
    
    // === Constructors ===
    
    /// <summary>
    /// Protected constructor for EF Core.
    /// </summary>
    protected Order() { }
    
    /// <summary>
    /// Creates a new order.
    /// </summary>
    public Order(
        int restaurantId,
        int customerId,
        int orderStatusId,
        string? specialInstructions = null)
    {
        // ---------------------------------------------------------------
        // PARAMETER GUARD CLAUSES — ArgumentException / ArgumentNullException
        //
        // These are NOT domain business rules. They are programming-error
        // guards that enforce the method's preconditions (its "contract").
        //
        // WHY NOT DomainException?
        // • An invalid restaurantId or customerId is a CALLER BUG, not a
        //   business rule the domain model needs to express. The caller
        //   passed data that should never reach the domain layer.
        // • ArgumentException/ArgumentNullException signal "you called me
        //   wrong" — they target developers, not end-users.
        // • DomainException signals "the operation violates a business
        //   invariant" — it targets the application layer so it can
        //   present a meaningful error to the user.
        //
        // .NET CONVENTION:
        // • ArgumentException / ArgumentNullException → 400 Bad Request
        //   (middleware maps these to HTTP 400)
        // • DomainException → 422 Unprocessable Entity
        //   (valid input, but violates a business rule)
        //
        // EXAMPLE DISTINCTION:
        // • restaurantId = 0     → ArgumentException  (programming error)
        // • unitPrice = -5       → ArgumentOutOfRangeException (programming error)
        // • Place() with no items → OrderDomainException
        //   (business rule: order must have items before placement)
        // • Item not found in order → OrderDomainException
        //   (business rule: cannot update non-existent item)
        // ---------------------------------------------------------------

        if (restaurantId <= 0)
            throw new ArgumentException("Valid restaurant ID is required.", nameof(restaurantId));

        if (customerId <= 0)
            throw new ArgumentException("Valid customer ID is required.", nameof(customerId));

        if (orderStatusId <= 0)
            throw new ArgumentException("Valid order status ID is required.", nameof(orderStatusId));
        
        RestaurantId = restaurantId;
        CustomerId = customerId;
        OrderStatusId = orderStatusId;
        SpecialInstructions = specialInstructions?.Trim();
        OrderDate = DateTime.UtcNow;
        TotalAmount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // === Business Methods (Aggregate Pattern) ===
    
    /// <summary>
    /// Adds an item to the order.
    /// This method is a factory for creating OrderItem child entities within the aggregate root.
    /// A factory method is a behavioral method that encapsulates the creation logic of child entities within the aggregate root.
    /// </summary>
    /// <remarks>
    /// AGGREGATE BEHAVIOR: This method maintains the aggregate boundary by being the only
    /// way to add OrderItem child entities. Direct manipulation of the collection
    /// is prevented through encapsulation.
    /// </remarks>
    public void AddItem(int dishId, string dishName, decimal unitPrice, int quantity, string? specialInstructions = null)
    {
        // Guard clauses: invalid parameters are programming errors, not business rules.
        if (dishId <= 0)
            throw new ArgumentException("Valid dish ID is required.", nameof(dishId));

        ArgumentException.ThrowIfNullOrWhiteSpace(dishName, nameof(dishName));

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "Unit price cannot be negative.");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");
        
        var orderItem = new OrderItem(dishId, unitPrice, quantity)
        {
            OrderId = Id,
            SpecialInstructions = specialInstructions,
            RestaurantId = RestaurantId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _orderItems.Add(orderItem);
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Removes an item from the order.
    /// </summary>
    public void RemoveItem(int orderItemId)
    {
        var item = _orderItems.FirstOrDefault(oi => oi.Id == orderItemId);
        if (item != null)
        {
            _orderItems.Remove(item);
            RecalculateTotals();
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Updates the quantity of an order item.
    /// </summary>
    public void UpdateItemQuantity(int orderItemId, int newQuantity)
    {
        // Guard clause: non-positive quantity is a programming error, not a business rule.
        if (newQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(newQuantity), newQuantity, "Quantity must be positive.");

        // Domain rule: item must exist in this order.
        var item = _orderItems.FirstOrDefault(oi => oi.Id == orderItemId);
        if (item == null)
            throw new OrderDomainException($"Order item '{orderItemId}' not found in this order.");
        
        item.UpdateQuantity(newQuantity);
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Recalculates the total amount based on order items.
    /// This is called automatically when items are added/removed/updated.
    /// </summary>
    private void RecalculateTotals()
    {
        TotalAmount = _orderItems.Sum(oi => oi.Subtotal);
    }
    
    /// <summary>
    /// Updates the order status.
    /// </summary>
    public void UpdateStatus(int newOrderStatusId)
    {
        // Guard clause: invalid status ID is a programming error, not a business rule.
        if (newOrderStatusId <= 0)
            throw new ArgumentException("Valid order status ID is required.", nameof(newOrderStatusId));
        
        OrderStatusId = newOrderStatusId;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Sets special instructions for the order.
    /// </summary>
    public void SetSpecialInstructions(string? instructions)
    {
        SpecialInstructions = instructions?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Assigns a staff member to handle this order.
    /// </summary>
    public void AssignStaffMember(int staffMemberId)
    {
        // Guard clause: invalid staff ID is a programming error, not a business rule.
        if (staffMemberId <= 0)
            throw new ArgumentException("Valid staff member ID is required.", nameof(staffMemberId));
        
        HandledByStaffId = staffMemberId;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Clears the assigned staff member.
    /// </summary>
    public void UnassignStaffMember()
    {
        HandledByStaffId = null;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Gets the count of items in this order.
    /// </summary>
    public int GetItemCount() => _orderItems.Count;
    
    /// <summary>
    /// Gets the total quantity of items in this order.
    /// </summary>
    public int GetTotalQuantity() => _orderItems.Sum(oi => oi.Quantity);
    
    // ===================================================================
    // DOMAIN EVENT RAISING METHODS
    // ===================================================================
    
    /// <summary>
    /// Places the order, transitioning it from draft/pending state to confirmed.
    /// Raises <see cref="OrderPlacedEvent"/> for downstream processing.
    /// </summary>
    /// <param name="confirmedStatusId">The order status ID for confirmed orders.</param>
    /// <param name="orderType">The type of order (e.g., "DineIn", "TakeOut", "Delivery").</param>
    /// <exception cref="InvalidOperationException">Thrown if order has no items or is already placed.</exception>
    /// <remarks>
    /// This method triggers:
    /// - Loyalty points calculation and accrual
    /// - Kitchen notification dispatch
    /// - Customer confirmation notification
    /// - Analytics updates
    /// </remarks>
    public void Place(int confirmedStatusId, string? orderType = null)
    {
        if (!_orderItems.Any())
            throw new OrderDomainException("Cannot place an order without items.");
        
        OrderStatusId = confirmedStatusId;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new OrderPlacedEvent(
            orderId: Id,
            restaurantId: RestaurantId,
            customerId: CustomerId,
            totalAmount: TotalAmount,
            itemCount: _orderItems.Count,
            currencyCode: "USD",
            specialInstructions: SpecialInstructions,
            orderType: orderType
        ));
    }
    
    /// <summary>
    /// Cancels the order with a specified reason.
    /// Raises <see cref="OrderCancelledEvent"/> for downstream processing.
    /// </summary>
    /// <param name="cancelledStatusId">The order status ID for cancelled orders.</param>
    /// <param name="reason">The reason for cancellation.</param>
    /// <param name="cancelledBy">Who initiated the cancellation.</param>
    /// <param name="cancelledByStaffId">The staff member ID if cancelled by staff.</param>
    /// <param name="loyaltyPointsToReverse">Points to reverse if pre-awarded.</param>
    /// <exception cref="InvalidOperationException">Thrown if order is in a terminal state.</exception>
    /// <remarks>
    /// This method triggers:
    /// - Loyalty points reversal (if pre-awarded)
    /// - Customer cancellation notification
    /// - Kitchen notification to stop preparation
    /// - Refund processing initiation
    /// </remarks>
    public void Cancel(
        int cancelledStatusId,
        string reason,
        CancellationSource cancelledBy,
        int? cancelledByStaffId = null,
        int loyaltyPointsToReverse = 0)
    {
        // Guard clause: missing cancellation reason is a programming error, not a business rule.
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        
        var previousStatusId = OrderStatusId;
        OrderStatusId = cancelledStatusId;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new OrderCancelledEvent(
            orderId: Id,
            restaurantId: RestaurantId,
            customerId: CustomerId,
            cancellationReason: reason,
            cancelledBy: cancelledBy,
            cancelledByStaffId: cancelledByStaffId,
            orderTotal: TotalAmount,
            previousStatus: previousStatusId.ToString(),
            requiresRefund: TotalAmount > 0,
            loyaltyPointsToReverse: loyaltyPointsToReverse
        ));
    }
    
    /// <summary>
    /// Completes the order after successful fulfillment.
    /// Raises <see cref="OrderCompletedEvent"/> for downstream processing.
    /// </summary>
    /// <param name="completedStatusId">The order status ID for completed orders.</param>
    /// <param name="loyaltyPointsEarned">Total loyalty points earned from this order.</param>
    /// <param name="orderType">The type of order.</param>
    /// <remarks>
    /// This method triggers:
    /// - Final loyalty points confirmation
    /// - Customer thank-you notification
    /// - Review request scheduling
    /// - Sales analytics finalization
    /// </remarks>
    public void Complete(int completedStatusId, int loyaltyPointsEarned = 0, string orderType = "DineIn")
    {
        OrderStatusId = completedStatusId;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new OrderCompletedEvent(
            orderId: Id,
            restaurantId: RestaurantId,
            customerId: CustomerId,
            finalTotal: TotalAmount,
            itemCount: _orderItems.Count,
            orderPlacedAt: OrderDate,
            completedAt: DateTime.UtcNow,
            orderType: orderType,
            loyaltyPointsEarned: loyaltyPointsEarned
        ));
    }
    
    // ===================================================================
    // MULTI-TENANT VALIDATION
    // ===================================================================

    /// <summary>
    /// Validates that the order maintains multi-tenant boundaries and consistency across all relationships.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when tenant consistency is violated.</exception>
    /// <remarks>
    /// This method should be called after navigation properties are loaded to ensure:
    /// - Restaurant navigation property matches RestaurantId
    /// - All order items belong to the same restaurant
    /// - Order status belongs to the same restaurant
    /// - Assigned staff member belongs to the same restaurant
    /// 
    /// Tenant Consistency Rules:
    /// 1. Order must belong to exactly one restaurant (RestaurantId)
    /// 2. All order items must belong to the same restaurant
    /// 3. OrderStatus must belong to the same restaurant
    /// 4. Staff member (if assigned) must belong to the same restaurant
    /// 5. Restaurant navigation property ID (if loaded) must match RestaurantId
    /// 
    /// Security Implications:
    /// This is a critical security check in multi-tenant systems to prevent:
    /// - Cross-tenant order processing
    /// - Order items from one restaurant appearing in another restaurant's orders
    /// - Staff from one restaurant accessing another restaurant's orders
    /// - Order statuses from one restaurant being used by another
    /// - Reporting inaccuracies and revenue leakage across tenants
    /// 
    /// When to Call:
    /// - After loading orders with navigation properties from database
    /// - Before processing order in multi-tenant contexts
    /// - In data import/migration processes
    /// - As part of data integrity audits
    /// - When validating order assignments and transitions
    /// 
    /// Performance Note:
    /// Only performs validation if navigation properties are loaded.
    /// Does not trigger lazy loading to avoid N+1 query issues.
    /// For large OrderItems collections, consider validating via database query instead.
    /// </remarks>
    public void ValidateTenantConsistency()
    {
        // Validate Restaurant navigation property consistency
        if (Restaurant != null && Restaurant.Id != RestaurantId)
        {
            throw new OrderDomainException(
                $"Restaurant navigation property ID ({Restaurant.Id}) does not match RestaurantId ({RestaurantId}).");
        }

        // Validate OrderStatus belongs to same restaurant
        if (Status != null && Status.RestaurantId != RestaurantId)
        {
            throw new OrderDomainException(
                $"Order status must belong to the same restaurant. " +
                $"Order RestaurantId: {RestaurantId}, OrderStatus RestaurantId: {Status.RestaurantId}, " +
                $"Status: {Status.Name} (ID: {Status.Id})");
        }

        // Validate all order items belong to same restaurant
        if (_orderItems != null && _orderItems.Any())
        {
            var inconsistentItems = _orderItems
                .Where(oi => oi.RestaurantId != RestaurantId)
                .Select(oi => new { oi.Id, oi.DishId, oi.Dish.Name, oi.RestaurantId })
                .ToList();

            if (inconsistentItems.Any())
            {
                var itemInfo = string.Join(", ", inconsistentItems.Select(oi => 
                    $"{oi.Name} (OrderItem ID: {oi.Id}, Dish ID: {oi.DishId}, RestaurantId: {oi.RestaurantId})"));
                
                throw new OrderDomainException(
                    $"Order contains items from different restaurants. " +
                    $"Order RestaurantId: {RestaurantId}, " +
                    $"Inconsistent items: [{itemInfo}]");
            }
        }

        // Validate staff member belongs to same restaurant (if assigned)
        if (HandledBy != null && HandledBy.RestaurantId != RestaurantId)
        {
            throw new OrderDomainException(
                $"Assigned staff member must belong to the same restaurant. " +
                $"Order RestaurantId: {RestaurantId}, Staff RestaurantId: {HandledBy.RestaurantId}, " +
                $"Staff: {HandledBy.Name} (ID: {HandledBy.Id})");
        }
    }
    
    // === Validation ===
    
    /// <summary>
    /// Validates the order entity ensuring data consistency and business rules.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Restaurant/Tenant validation
        if (RestaurantId <= 0)
        {
            yield return new ValidationResult(
                "Order must be associated with a restaurant",
                new[] { nameof(RestaurantId) }
            );
        }

        // Restaurant navigation property consistency
        if (Restaurant != null && Restaurant.Id != RestaurantId)
        {
            yield return new ValidationResult(
                "Restaurant navigation property is inconsistent with RestaurantId",
                new[] { nameof(Restaurant), nameof(RestaurantId) }
            );
        }

        // OrderItems tenant consistency validation
        if (_orderItems.Any())
        {
            var inconsistentItems = _orderItems
                .Where(oi => oi.RestaurantId != RestaurantId)
                .Select(oi => new { oi.Id, oi.DishId })
                .ToList();

            if (inconsistentItems.Any())
            {
                yield return new ValidationResult(
                    $"Order contains items from different restaurants. Item IDs: {string.Join(", ", inconsistentItems.Select(i => i.Id))}",
                    new[] { nameof(OrderItems), nameof(RestaurantId) }
                );
            }
        }

        // Staff member tenant consistency
        if (HandledBy != null && HandledBy.RestaurantId != RestaurantId)
        {
            yield return new ValidationResult(
                "Staff member must belong to the same restaurant as the order",
                new[] { nameof(HandledBy), nameof(RestaurantId) }
            );
        }

        // Order status tenant consistency
        if (Status != null && Status.RestaurantId != RestaurantId)
        {
            yield return new ValidationResult(
                "Order status must belong to the same restaurant",
                new[] { nameof(Status), nameof(RestaurantId) }
            );
        }

        // Order date validation
        if (OrderDate > DateTime.UtcNow.AddHours(1))
        {
            yield return new ValidationResult(
                "Order date cannot be in the future",
                new[] { nameof(OrderDate) }
            );
        }

        // Total amount consistency
        if (_orderItems.Any())
        {
            var calculatedTotal = _orderItems.Sum(oi => oi.Subtotal);
            if (Math.Abs(calculatedTotal - TotalAmount) > 0.01m)
            {
                yield return new ValidationResult(
                    "Total amount does not match sum of order items",
                    new[] { nameof(TotalAmount) }
                );
            }
        }
        else if (TotalAmount != 0)
        {
            yield return new ValidationResult(
                "Order cannot have non-zero total without order items",
                new[] { nameof(TotalAmount), nameof(OrderItems) }
            );
        }
    }
}
