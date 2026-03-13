using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.SaleRecordAggregate.Events;
using SmartMenuOptim.Domain.Common;
using SmartMenuOptim.Domain.ValueObjects;
using System.Runtime.ConstrainedExecution;

namespace SmartMenuOptim.Domain.Aggregates.SaleRecordAggregate
{
    /// <summary>
    /// Represents a sales transaction record for a dish in a specific restaurant, tracking quantity sold and revenue generated.
    /// </summary>
    /// <remarks>
    /// <para><strong>3-TIER DDD STRATEGY: Tier 2 - Simple Aggregates (Lightweight DDD)</strong></para>
    /// <para>This class implements a lightweight DDD aggregate pattern suitable for entities that need rich domain behavior
    /// without the complexity of full aggregate roots. It balances encapsulation and validation with practical implementation.</para>
    /// 
    /// <para><strong>Tier 2 Characteristics:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Encapsulation:</strong> Properties use private setters to prevent unauthorized state changes</description></item>
    ///   <item><description><strong>Validation:</strong> Business rules enforced through constructor and behavioral methods with guard clauses</description></item>
    ///   <item><description><strong>Rich Behavior:</strong> Domain logic encapsulated in methods (UpdateSaleAmount, UpdateQuantity) rather than anemic property bags</description></item>
    ///   <item><description><strong>Simple Lifecycle:</strong> No complex child entities or deep object graphs</description></item>
    ///   <item><description><strong>Lightweight Invariants:</strong> Basic consistency rules (positive quantity, valid amounts, date validation)</description></item>
    ///   <item><description><strong>Immutability:</strong> Sales records are largely immutable after creation (except for corrections via behavioral methods)</description></item>
    /// </list>
    /// 
    /// <para><strong>Entity Overview:</strong></para>
    /// <para>A SaleRecord captures a completed sales transaction for a dish within a restaurant's point-of-sale system. It includes
    /// the quantity sold, total sale amount (using the Money value object for currency handling), sale timestamp, and links to the
    /// dish that was sold. These records form the foundation for sales analytics, revenue tracking, and dish performance analysis.</para>
    /// 
    /// <para><strong>Multi-Tenant Support:</strong></para>
    /// <para>Inherits from TenantEntityBase to provide built-in multi-tenancy support. Each sale record is scoped to a specific
    /// restaurant (RestaurantId), ensuring proper data isolation in a multi-tenant environment. The sale record must belong to
    /// the same restaurant as the dish being sold to maintain tenant boundary integrity.</para>
    /// 
    /// <para><strong>Consistency Boundary:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Invariants Protected:</strong> Quantity must be positive, sale amount cannot be negative, sale date cannot be in future or older than 5 years</description></item>
    ///   <item><description><strong>Encapsulated State:</strong> Internal state can only be modified through behavioral methods (UpdateSaleAmount, UpdateQuantity)</description></item>
    ///   <item><description><strong>Transactional Consistency:</strong> All changes validated atomically through public methods</description></item>
    ///   <item><description><strong>Business Rules:</strong> Cannot record sales for deleted/inactive dishes, must maintain tenant consistency</description></item>
    ///   <item><description><strong>Value Object Integration:</strong> Uses Money value object to ensure monetary amounts are handled consistently</description></item>
    /// </list>
    /// 
    /// <para><strong>Domain Features:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Identity:</strong> Inherits entity identity from TenantEntityBase (Id property from EntityBase)</description></item>
    ///   <item><description><strong>Automatic Timestamps:</strong> CreatedAt, UpdatedAt automatically managed through EntityBase</description></item>
    ///   <item><description><strong>Soft Delete Support:</strong> Inherits IsDeleted flag for soft deletion scenarios</description></item>
    ///   <item><description><strong>Optimistic Concurrency:</strong> Uses xmin timestamp token from EntityBase for concurrency control</description></item>
    ///   <item><description><strong>Money Value Object:</strong> Encapsulates currency amounts with proper decimal precision and currency handling</description></item>
    ///   <item><description><strong>Correction Support:</strong> Allows authorized corrections via UpdateSaleAmount and UpdateQuantity methods</description></item>
    /// </list>
    /// 
    /// <para><strong>Relationships:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Dish (Required):</strong> Each sale record must be for exactly one dish</description></item>
    ///   <item><description><strong>Restaurant (Required):</strong> Inherited from TenantEntityBase, ensures tenant isolation</description></item>
    ///   <item><description><strong>No Customer Link:</strong> Sale records track transactions, not customer identity (handled separately in orders)</description></item>
    /// </list>
    /// 
    /// <para><strong>Example Usage:</strong></para>
    /// <code>
    /// // Recording a sale for 3 burgers at $12.99 each
    /// var saleAmount = new Money(38.97m, "USD");
    /// var saleRecord = new SaleRecord(
    ///     restaurantId: 123,
    ///     dishId: 456,
    ///     saleAmount: saleAmount,
    ///     quantitySold: 3
    /// );
    /// // SaleDate automatically set to DateTime.UtcNow
    /// 
    /// // Correcting the sale amount (e.g., discount applied)
    /// var correctedAmount = new Money(35.00m, "USD");
    /// saleRecord.UpdateSaleAmount(correctedAmount);
    /// 
    /// // Correcting quantity (e.g., one item was returned)
    /// saleRecord.UpdateQuantity(2);
    /// 
    /// // Validating tenant consistency after loading from database
    /// saleRecord.ValidateTenantConsistency();
    /// 
    /// // Analyzing sales data
    /// var totalRevenue = saleRecords.Sum(sr => sr.SaleAmount.Amount);
    /// var totalQuantity = saleRecords.Sum(sr => sr.QuantitySold);
    /// var averagePrice = totalRevenue / totalQuantity;
    /// </code>
    /// 
    /// <para><strong>Entity Framework Core Support:</strong></para>
    /// <para>Includes a protected parameterless constructor for EF Core's use during materialization. The entity can be
    /// persisted and retrieved through a repository pattern. Private setters are accessible to EF Core through reflection-based
    /// field mapping in the entity configuration. The Money value object is configured as an owned entity type in EF Core.</para>
    /// 
    /// <para><strong>Design Considerations:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Quantity Validation:</strong> Quantity must be positive (cannot record zero or negative sales)</description></item>
    ///   <item><description><strong>Amount Validation:</strong> Sale amount cannot be negative (but can be zero for promotional items)</description></item>
    ///   <item><description><strong>Date Constraints:</strong> Sale date cannot be in the future (with 1-minute grace period for clock skew)</description></item>
    ///   <item><description><strong>Historical Limit:</strong> Sales older than 5 years cannot be recorded (prevents data entry errors)</description></item>
    ///   <item><description><strong>Immutable Creation:</strong> Sale date set once at creation and never modified</description></item>
    ///   <item><description><strong>Correction Workflow:</strong> Updates via behavioral methods preserve audit trail through UpdatedAt timestamp</description></item>
    ///   <item><description><strong>Money Value Object:</strong> Ensures proper decimal precision, currency consistency, and prevents primitive obsession</description></item>
    ///   <item><description><strong>Aggregate Analytics:</strong> Sale records are aggregated for reporting (daily sales, dish performance, revenue trends)</description></item>
    /// </list>
    /// 
    /// <para><strong>Indexing Strategy:</strong></para>
    /// <para>Database indexes for efficient querying are defined centrally in AppDbContext.OnModelCreating:
    /// - IX_SaleRecords_Restaurant_Dish_Date: Composite index for tenant-scoped dish sales analysis over time
    /// - IX_SaleRecords_SaleDate: For time-series queries and daily/monthly sales reports
    /// - IX_SaleRecords_Dish: For dish-specific sales performance analysis
    /// - Covering indexes may include SaleAmount and QuantitySold for aggregate queries</para>
    /// 
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Revenue Tracking:</strong> Calculate total sales revenue by dish, day, week, or month</description></item>
    ///   <item><description><strong>Inventory Management:</strong> Track dish sales volume for ingredient forecasting</description></item>
    ///   <item><description><strong>Menu Optimization:</strong> Identify best-selling and underperforming dishes</description></item>
    ///   <item><description><strong>Pricing Analysis:</strong> Analyze average sale prices and discount impact</description></item>
    ///   <item><description><strong>Sales Trends:</strong> Time-series analysis of sales patterns and seasonality</description></item>
    ///   <item><description><strong>Audit Trail:</strong> Maintain historical record of all sales transactions</description></item>
    /// </list>
    /// </remarks>
    [Table("SaleRecords")]
    public class SaleRecord : TenantEntityBase
    {
        // ===================================================================
        // PRIVATE FIELDS
        // ===================================================================
        
        /// <summary>
        /// Maximum number of years in the past that a sale record can be created for.
        /// Prevents data entry errors from creating sales records with incorrect historical dates.
        /// </summary>
        private const int MaxYearsInPast = 5;
        
        private readonly List<IDomainEvent> _domainEvents = new();
        
        // ===================================================================
        // DOMAIN EVENTS
        // ===================================================================
        
        /// <summary>
        /// Gets the domain events raised by this entity.
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
        /// </summary>
        protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        // ===================================================================
        // PROPERTIES WITH ENCAPSULATION (Private Setters)
        // ===================================================================

        /// <summary>
        /// Foreign key to the Dish entity. Each sale record is for a single dish.
        /// </summary>
        /// <remarks>
        /// Required relationship. The dish must belong to the same restaurant as the sale record (tenant boundary enforcement).
        /// Used to link sales data to menu items for performance analytics and inventory management.
        /// </remarks>
        public int DishId { get; private set; }

        /// <summary>
        /// The total amount of the sale using the Money value object.
        /// </summary>
        /// <remarks>
        /// Ensures monetary values are handled consistently across the domain with proper:
        /// - Decimal precision (typically 2 decimal places for currency)
        /// - Currency code tracking (USD, EUR, etc.)
        /// - Arithmetic operations without floating-point errors
        /// - Validation of non-negative amounts
        /// Cannot be negative, but can be zero for promotional/complimentary items.
        /// Modifiable via UpdateSaleAmount() method for authorized corrections.
        /// </remarks>
        public Money SaleAmount { get; private set; }

        /// <summary>
        /// Quantity of the dish sold in this record. Must be positive.
        /// </summary>
        /// <remarks>
        /// Represents the number of units (servings) of the dish sold in this transaction.
        /// Must be at least 1 - cannot record zero or negative sales.
        /// Used for:
        /// - Inventory tracking and ingredient forecasting
        /// - Dish popularity analysis
        /// - Revenue per unit calculations
        /// Modifiable via UpdateQuantity() method for authorized corrections (e.g., returns, errors).
        /// </remarks>
        public int QuantitySold { get; private set; }

        /// <summary>
        /// Date and time of the sale (UTC). Set at creation time.
        /// </summary>
        /// <remarks>
        /// Timestamp when the sale transaction occurred, automatically set to DateTime.UtcNow during creation.
        /// Immutable after creation to maintain audit trail integrity.
        /// Constraints:
        /// - Cannot be in the future (with 1-minute grace period for clock skew)
        /// - Cannot be older than 5 years (MaxYearsInPast constant)
        /// Used for:
        /// - Time-series sales analysis
        /// - Daily/weekly/monthly sales reports
        /// - Revenue trend identification
        /// - Seasonal pattern analysis
        /// </remarks>
        public DateTime SaleDate { get; private set; }

        // ===================================================================
        // NAVIGATION PROPERTIES
        // ===================================================================

        /// <summary>
        /// Navigation property to the Dish this sales record is for.
        /// </summary>
        /// <remarks>
        /// Optional for lazy loading scenarios. When loaded, provides access to:
        /// - Dish name and description for reporting
        /// - Dish category for sales analysis by category
        /// - Current dish pricing for comparison
        /// Used in ValidateTenantConsistency() to ensure dish belongs to same restaurant.
        /// May be null if not eager-loaded or explicitly loaded.
        /// </remarks>
        [InverseProperty(nameof(Dish.SaleRecords))]
        public Dish? Dish { get; private set; }

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
        protected SaleRecord() { }

        /// <summary>
        /// Creates a new sale record with validation.
        /// </summary>
        /// <param name="restaurantId">The restaurant (tenant) identifier where the sale occurred.</param>
        /// <param name="dishId">The dish identifier for the item sold.</param>
        /// <param name="saleAmount">The total sale amount using the Money value object (must be non-negative).</param>
        /// <param name="quantitySold">The quantity sold (must be positive, minimum 1).</param>
        /// <exception cref="ArgumentException">Thrown when validation fails for any parameter.</exception>
        /// <exception cref="ArgumentNullException">Thrown when saleAmount is null.</exception>
        /// <remarks>
        /// This constructor enforces invariants at creation time following DDD best practices.
        /// 
        /// Validation Rules Enforced:
        /// - RestaurantId must be positive integer (tenant identifier)
        /// - DishId must be positive integer (dish reference)
        /// - SaleAmount cannot be null and must be non-negative (uses Money value object)
        /// - QuantitySold must be at least 1 (no zero or negative sales)
        /// 
        /// Automatic Behavior:
        /// - SaleDate is automatically set to current UTC time (DateTime.UtcNow)
        /// - CreatedAt timestamp automatically set by EntityBase
        /// - Initial UpdatedAt set to match CreatedAt
        /// 
        /// Post-Creation Validation:
        /// After persisting and loading with navigation properties, call ValidateTenantConsistency()
        /// to ensure the dish belongs to the same restaurant.
        /// 
        /// Usage Context:
        /// Typically called by:
        /// - Point-of-sale systems when completing orders
        /// - Order completion workflows
        /// - Sales import/migration processes
        /// - Manual sales entry interfaces
        /// </remarks>
        public SaleRecord(
            int restaurantId,
            int dishId,
            Money saleAmount,
            int quantitySold)
        {
            // Validate invariants
            ValidateRestaurantId(restaurantId);
            ValidateDishId(dishId);
            ValidateSaleAmount(saleAmount);
            ValidateQuantity(quantitySold);

            // Set properties
            RestaurantId = restaurantId;
            DishId = dishId;
            SaleAmount = saleAmount;
            QuantitySold = quantitySold;
            SaleDate = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Records a sale with full context and raises the <see cref="SaleRecordedEvent"/>.
        /// Use this factory method when all context is available.
        /// </summary>
        /// <param name="restaurantId">The restaurant identifier.</param>
        /// <param name="orderId">The associated order identifier.</param>
        /// <param name="dishId">The dish identifier.</param>
        /// <param name="dishName">The name of the dish.</param>
        /// <param name="categoryName">The category name.</param>
        /// <param name="saleAmount">The total sale amount.</param>
        /// <param name="unitPrice">The unit price at time of sale.</param>
        /// <param name="quantitySold">The quantity sold.</param>
        /// <param name="customerId">Optional customer identifier.</param>
        /// <param name="processedByStaffId">Optional staff identifier.</param>
        /// <param name="orderType">Optional order type.</param>
        /// <returns>A new SaleRecord instance with the event raised.</returns>
        public static SaleRecord RecordSale(
            int restaurantId,
            int orderId,
            int dishId,
            string dishName,
            string categoryName,
            Money saleAmount,
            decimal unitPrice,
            int quantitySold,
            int? customerId = null,
            int? processedByStaffId = null,
            string? orderType = null)
        {
            var record = new SaleRecord(restaurantId, dishId, saleAmount, quantitySold);
            
            record.AddDomainEvent(new SaleRecordedEvent(
                saleRecordId: record.Id,
                restaurantId: restaurantId,
                orderId: orderId,
                dishId: dishId,
                dishName: dishName,
                categoryName: categoryName,
                quantitySold: quantitySold,
                unitPrice: unitPrice,
                totalAmount: saleAmount.Amount,
                saleDateTime: record.SaleDate,
                currencyCode: saleAmount.Currency,
                customerId: customerId,
                processedByStaffId: processedByStaffId,
                orderType: orderType
            ));
            
            return record;
        }

        // ===================================================================
        // DOMAIN BEHAVIORS (Tier 2 - Lightweight DDD Methods)
        // ===================================================================

        /// <summary>
        /// Updates the sale amount if needed (e.g., for corrections, discounts applied after sale, or refunds).
        /// </summary>
        /// <param name="newAmount">The corrected sale amount using the Money value object (must be non-negative).</param>
        /// <exception cref="ArgumentException">Thrown when amount is invalid or negative.</exception>
        /// <exception cref="ArgumentNullException">Thrown when newAmount is null.</exception>
        /// <remarks>
        /// This behavioral method allows authorized corrections to the sale amount while maintaining encapsulation.
        /// 
        /// Common Use Cases:
        /// - Applying post-sale discounts or promotions
        /// - Correcting data entry errors
        /// - Processing partial refunds
        /// - Adjusting for price changes or system errors
        /// 
        /// Validation:
        /// - Validates the new amount is not null
        /// - Ensures amount is non-negative (zero allowed for full refunds)
        /// - Maintains Money value object constraints
        /// 
        /// Side Effects:
        /// - Updates the UpdatedAt timestamp automatically (via EntityBase)
        /// - Preserves audit trail through timestamp changes
        /// 
        /// Authorization:
        /// This method should only be called by authorized users (managers, admins) through
        /// application services that enforce role-based access control.
        /// 
        /// Note: Does not modify QuantitySold - use UpdateQuantity() separately if needed.
        /// </remarks>
        public void UpdateSaleAmount(Money newAmount)
        {
            ValidateSaleAmount(newAmount);
            SaleAmount = newAmount;
        }

        /// <summary>
        /// Updates the quantity sold if needed (e.g., for corrections, returns, or data entry errors).
        /// </summary>
        /// <param name="newQuantity">The corrected quantity (must be positive, minimum 1).</param>
        /// <exception cref="ArgumentException">Thrown when quantity is invalid (zero or negative).</exception>
        /// <remarks>
        /// This behavioral method allows authorized corrections to the quantity while maintaining encapsulation.
        /// 
        /// Common Use Cases:
        /// - Correcting data entry errors in quantity
        /// - Processing partial returns (reducing quantity)
        /// - Adjusting for system errors or duplicate entries
        /// 
        /// Validation:
        /// - Quantity must be at least 1 (cannot set to zero or negative)
        /// - For full returns, the entire sale record should be soft-deleted instead
        /// 
        /// Side Effects:
        /// - Updates the UpdatedAt timestamp automatically (via EntityBase)
        /// - Preserves audit trail through timestamp changes
        /// 
        /// Authorization:
        /// This method should only be called by authorized users (managers, admins) through
        /// application services that enforce role-based access control.
        /// 
        /// Note: Does not modify SaleAmount - use UpdateSaleAmount() separately to adjust total.
        /// For returns, typically both quantity and amount should be updated together.
        /// </remarks>
        public void UpdateQuantity(int newQuantity)
        {
            ValidateQuantity(newQuantity);
            QuantitySold = newQuantity;
        }

        /// <summary>
        /// Updates the sale date if needed (e.g., for correcting data entry errors or migrating historical data).
        /// </summary>
        /// <param name="newSaleDate">The corrected sale date (cannot be in future or older than 5 years).</param>
        /// <exception cref="ArgumentException">Thrown when sale date is in the future or too far in the past.</exception>
        /// <remarks>
        /// This behavioral method allows authorized corrections to the sale date while maintaining encapsulation.
        /// 
        /// Common Use Cases:
        /// - Correcting data entry errors in sale timestamp
        /// - Migrating historical sales data with accurate timestamps
        /// - Adjusting for timezone conversion errors
        /// - Fixing system clock issues that recorded incorrect timestamps
        /// 
        /// Validation:
        /// - Sale date cannot be in the future (with 1-minute grace period for clock skew)
        /// - Sale date cannot be older than 5 years (MaxYearsInPast constant)
        /// 
        /// Side Effects:
        /// - Updates the UpdatedAt timestamp automatically (via EntityBase)
        /// - Preserves audit trail through timestamp changes
        /// 
        /// Authorization:
        /// This method should only be called by authorized users (managers, admins) through
        /// application services that enforce role-based access control.
        /// 
        /// Security Warning:
        /// Changing sale dates can affect:
        /// - Revenue reporting by date/period
        /// - Sales trend analysis and forecasting
        /// - Inventory calculations tied to time periods
        /// - Audit trail integrity
        /// Use with caution and log all date modifications for compliance.
        /// </remarks>
        public void UpdateSaleDate(DateTime newSaleDate)
        {
            ValidateSaleDate(newSaleDate);
            SaleDate = newSaleDate;
        }

        // ===================================================================
        // MULTI-TENANT VALIDATION
        // ===================================================================

        /// <summary>
        /// Validates that the sale record maintains multi-tenant boundaries and consistency.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when tenant consistency is violated.</exception>
        /// <remarks>
        /// This method should be called after navigation properties are loaded to ensure:
        /// - Dish navigation property matches DishId
        /// - Dish belongs to the same restaurant as the sale record
        /// - Dish is active and not deleted
        /// 
        /// Tenant Consistency Rules:
        /// 1. Sale record must belong to exactly one restaurant (RestaurantId)
        /// 2. Dish must belong to the same restaurant as the sale record
        /// 3. Dish must be active (not soft-deleted or marked inactive)
        /// 4. Dish navigation property ID must match DishId foreign key
        /// 
        /// Security Implications:
        /// This is a critical security check in multi-tenant systems to prevent:
        /// - Cross-tenant data leakage
        /// - Sales being attributed to wrong restaurant
        /// - Reporting inaccuracies
        /// - Revenue calculation errors across tenants
        /// 
        /// When to Call:
        /// - After loading sale records with navigation properties from database
        /// - Before displaying sales data in multi-tenant contexts
        /// - In data import/migration processes
        /// - As part of data integrity audits
        /// 
        /// Performance Note:
        /// Only performs validation if Dish navigation property is loaded.
        /// Does not trigger lazy loading to avoid N+1 query issues.
        /// </remarks>
        public void ValidateTenantConsistency()
        {
            if (Dish != null)
            {
                if (Dish.Id != DishId)
                {
                    throw new InvalidOperationException(
                        $"Dish navigation property ID ({Dish.Id}) does not match DishId ({DishId}).");
                }

                if (Dish.RestaurantId != RestaurantId)
                {
                    throw new InvalidOperationException(
                        $"Sale record must belong to same restaurant as the Dish. " +
                        $"SaleRecord RestaurantId: {RestaurantId}, Dish RestaurantId: {Dish.RestaurantId}");
                }

                if (!Dish.IsActive || Dish.IsDeleted)
                {
                    throw new InvalidOperationException(
                        "Cannot have sale record for inactive or deleted dish.");
                }
            }
        }

        // ===================================================================
        // PRIVATE VALIDATION METHODS (Guard Clauses)
        // ===================================================================

        /// <summary>
        /// Validates that the restaurant identifier is valid.
        /// </summary>
        /// <param name="restaurantId">The restaurant identifier to validate.</param>
        /// <exception cref="ArgumentException">Thrown when restaurant ID is zero or negative.</exception>
        /// <remarks>
        /// Ensures the sale record can be properly associated with a tenant (restaurant).
        /// Zero or negative IDs are invalid as they don't correspond to any database record.
        /// </remarks>
        private static void ValidateRestaurantId(int restaurantId)
        {
            if (restaurantId <= 0)
            {
                throw new ArgumentException(
                    "RestaurantId must be a positive integer.",
                    nameof(restaurantId));
            }
        }

        /// <summary>
        /// Validates that the dish identifier is valid.
        /// </summary>
        /// <param name="dishId">The dish identifier to validate.</param>
        /// <exception cref="ArgumentException">Thrown when dish ID is zero or negative.</exception>
        /// <remarks>
        /// Ensures the sale record can be properly linked to a menu item.
        /// Zero or negative IDs are invalid as they don't correspond to any database record.
        /// </remarks>
        private static void ValidateDishId(int dishId)
        {
            if (dishId <= 0)
            {
                throw new ArgumentException(
                    "DishId must be a positive integer.",
                    nameof(dishId));
            }
        }

        /// <summary>
        /// Validates that the sale amount meets business rules.
        /// </summary>
        /// <param name="saleAmount">The sale amount to validate using the Money value object.</param>
        /// <exception cref="ArgumentNullException">Thrown when sale amount is null.</exception>
        /// <exception cref="ArgumentException">Thrown when sale amount is negative.</exception>
        /// <remarks>
        /// Enforces monetary value constraints:
        /// - Amount cannot be null (required field)
        /// - Amount cannot be negative (sales must be zero or positive)
        /// - Zero amounts allowed for promotional/complimentary items
        /// 
        /// The Money value object provides additional validation:
        /// - Proper decimal precision
        /// - Currency code validation
        /// - Arithmetic operation safety
        /// </remarks>
        private static void ValidateSaleAmount(Money saleAmount)
        {
            if (saleAmount == null)
            {
                throw new ArgumentNullException(
                    nameof(saleAmount),
                    "SaleAmount cannot be null.");
            }

            if (saleAmount.Amount < 0)
            {
                throw new ArgumentException(
                    "SaleAmount cannot be negative.",
                    nameof(saleAmount));
            }
        }

        /// <summary>
        /// Validates that the quantity meets business rules.
        /// </summary>
        /// <param name="quantity">The quantity sold to validate.</param>
        /// <exception cref="ArgumentException">Thrown when quantity is zero or negative.</exception>
        /// <remarks>
        /// Enforces quantity constraints:
        /// - Must be at least 1 (cannot sell zero items)
        /// - No negative quantities (returns handled separately)
        /// 
        /// Business Rules:
        /// - A sale must include at least one item
        /// - For full refunds, soft-delete the entire record instead of setting quantity to zero
        /// - For partial returns, reduce quantity but keep it positive
        /// </remarks>
        private static void ValidateQuantity(int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException(
                    "QuantitySold must be a positive integer.",
                    nameof(quantity));
            }
        }

        /// <summary>
        /// Validates that the sale date meets business rules.
        /// </summary>
        /// <param name="saleDate">The sale date to validate.</param>
        /// <exception cref="ArgumentException">Thrown when sale date is in the future or too far in the past.</exception>
        /// <remarks>
        /// Enforces temporal constraints:
        /// - Cannot be in the future (with 1-minute grace period for clock skew)
        /// - Cannot be older than 5 years (MaxYearsInPast constant)
        /// 
        /// Rationale:
        /// - Future dates indicate data entry errors or system clock issues
        /// - Very old dates suggest incorrect year entry or data migration problems
        /// - 1-minute grace period accommodates minor clock synchronization issues
        /// 
        /// Clock Skew Handling:
        /// Allows up to 1 minute in the future to handle minor time differences
        /// between application servers and database servers.
        /// </remarks>
        private void ValidateSaleDate(DateTime saleDate)
        {
            if (saleDate > DateTime.UtcNow.AddMinutes(1)) // Allow slight clock skew
            {
                throw new ArgumentException(
                    "SaleDate cannot be in the future.",
                    nameof(saleDate));
            }

            var oldestAllowedDate = DateTime.UtcNow.AddYears(-MaxYearsInPast);
            if (saleDate < oldestAllowedDate)
            {
                throw new ArgumentException(
                    $"SaleDate cannot be more than {MaxYearsInPast} years in the past.",
                    nameof(saleDate));
            }
        }
    }
}
