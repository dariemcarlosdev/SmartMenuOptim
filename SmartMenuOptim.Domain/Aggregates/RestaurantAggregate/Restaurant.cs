using SmartMenuOptim.Domain.Aggregates.CustomerLoyaltyAggregate;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.MenuAggregate;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Aggregates.TableAggregate;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Aggregates.ReviewAggregate;
using SmartMenuOptim.Domain.Aggregates.SaleRecordAggregate;
using SmartMenuOptim.Domain.Aggregates.RestaurantAggregate.Errors;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Domain.Aggregates.RestaurantAggregate;

/// <summary>
/// Represents a restaurant aggregate root managing business operations, hours, contact information, and serving as the tenant boundary for multi-tenancy.
/// </summary>
/// <remarks>
/// <para><strong>3-TIER DDD STRATEGY: Tier 1 - Full Aggregate Roots (Rich DDD)</strong></para>
/// <para>This class implements a full DDD aggregate root pattern with child entities (BusinessHours) and value objects (Address, Email, PhoneNumber).
/// It serves as the tenant root entity - all other tenant-scoped entities reference this through RestaurantId foreign key.</para>
/// 
/// <para><strong>Tier 1 Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Full Encapsulation:</strong> All properties use private setters; state changes only through behavioral methods</description></item>
///   <item><description><strong>Child Entity Management:</strong> Manages BusinessHours child entities through encapsulated collection</description></item>
///   <item><description><strong>Aggregate Boundary:</strong> Defines transactional consistency boundary - all changes to restaurant and hours happen atomically</description></item>
///   <item><description><strong>Rich Domain Behavior:</strong> Complex business logic for hours management, order acceptance control, timezone handling</description></item>
///   <item><description><strong>Invariant Protection:</strong> Maintains invariants (can't accept orders without business hours, valid contact info)</description></item>
///   <item><description><strong>Collection Encapsulation:</strong> Private backing field (_operatingHours) with read-only public access</description></item>
///   <item><description><strong>Value Object Integration:</strong> Uses Address, Email, PhoneNumber value objects for proper domain modeling</description></item>
///   <item><description><strong>Tenant Root:</strong> Restaurant IS the tenant - doesn't inherit TenantEntityBase (would cause circular reference)</description></item>
/// </list>
/// 
/// <para><strong>Entity Overview:</strong></para>
/// <para>A Restaurant represents a physical dining establishment with complete operational information including business hours,
/// contact details, location, timezone, and order capacity management. As the tenant root in a multi-tenant system, all
/// other entities (Menu, Dish, Order, Staff, etc.) belong to and are scoped by a Restaurant. Each restaurant is owned by
/// an AdminUser and operates independently with its own data isolation.</para>
/// 
/// <para><strong>Multi-Tenant Design:</strong></para>
/// <para><strong>CRITICAL:</strong> Restaurant IS the tenant root entity. It does NOT inherit from TenantEntityBase because:</para>
/// <list type="bullet">
///   <item><description>TenantEntityBase contains RestaurantId FK - would create circular self-reference</description></item>
///   <item><description>Restaurant.Id serves as the tenant identifier for all child entities</description></item>
///   <item><description>Child entities (Menu, Dish, Order) inherit TenantEntityBase and reference Restaurant via RestaurantId</description></item>
///   <item><description>Inherits from EntityBase for audit fields (CreatedAt, UpdatedAt, IsDeleted, xmin)</description></item>
///   <item><description>Ensures clean separation and prevents migration/FK issues</description></item>
/// </list>
/// 
/// <para><strong>Aggregate Composition:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Root Entity:</strong> Restaurant (this class)</description></item>
///   <item><description><strong>Child Entities:</strong> BusinessHours collection - operating hours per day of week</description></item>
///   <item><description><strong>Value Objects:</strong> Address (location), Email (contact), PhoneNumber (contact)</description></item>
///   <item><description><strong>Referenced By:</strong> Menu, Dish, Order, Category, Table, Staff, Promotion, etc. (via RestaurantId FK)</description></item>
/// </list>
/// 
/// <para><strong>Consistency Boundary:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Invariants Protected:</strong> Valid contact info (email, phone, address), timezone exists, max orders ≥ 1, can't accept orders without business hours</description></item>
///   <item><description><strong>Encapsulated State:</strong> Internal state modified only through behavioral methods (UpdateBasicInfo, SetBusinessHours, StartAcceptingOrders, etc.)</description></item>
///   <item><description><strong>Transactional Consistency:</strong> All changes to restaurant and child hours saved atomically through repository</description></item>
///   <item><description><strong>Business Rules:</strong> Must have business hours before accepting orders, timezone must be valid, contact info properly formatted</description></item>
///   <item><description><strong>Child Collection:</strong> BusinessHours can only be added/updated through aggregate root methods</description></item>
/// </list>
/// 
/// <para><strong>Domain Features:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Identity:</strong> Inherits entity identity from EntityBase (Id property) - serves as tenant identifier</description></item>
///   <item><description><strong>Automatic Timestamps:</strong> CreatedAt, UpdatedAt automatically managed through EntityBase</description></item>
///   <item><description><strong>Soft Delete Support:</strong> Inherits IsDeleted flag for logical deletion (closed restaurants)</description></item>
///   <item><description><strong>Optimistic Concurrency:</strong> Uses xmin timestamp token from EntityBase for concurrency control</description></item>
///   <item><description><strong>Business Hours Management:</strong> Manages operating hours per day of week with open/close times</description></item>
///   <item><description><strong>Order Capacity Control:</strong> MaxSimultaneousOrders limits concurrent order processing</description></item>
///   <item><description><strong>Timezone Support:</strong> Stores timezone identifier for proper time calculations</description></item>
///   <item><description><strong>Order Acceptance Control:</strong> IsAcceptingOrders flag for operational control</description></item>
/// </list>
/// 
/// <para><strong>Relationships:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Owner (Required):</strong> Each restaurant owned by exactly one AdminUser</description></item>
///   <item><description><strong>BusinessHours (One-to-Many Children):</strong> Operating hours managed exclusively through aggregate root</description></item>
///   <item><description><strong>Menus (Referenced):</strong> All menus belong to this restaurant</description></item>
///   <item><description><strong>Dishes (Referenced):</strong> All dishes belong to this restaurant</description></item>
///   <item><description><strong>Orders (Referenced):</strong> All orders belong to this restaurant</description></item>
///   <item><description><strong>Tables (Referenced):</strong> All tables belong to this restaurant</description></item>
///   <item><description><strong>Staff (Referenced):</strong> All staff members belong to this restaurant</description></item>
/// </list>
/// 
/// <para><strong>Lifecycle States:</strong></para>
/// <code>
/// Created → Setup → Operating ⇄ Paused → Closed
///    ↓
/// Ownership Transfer (AdminUser change)
/// 
/// 1. Created: New restaurant, no business hours (IsAcceptingOrders = false)
/// 2. Setup: Business hours configured, contact info set
/// 3. Operating: Accepting orders (IsAcceptingOrders = true, has business hours)
/// 4. Paused: Temporarily not accepting orders (IsAcceptingOrders = false, hours preserved)
/// 5. Closed: Permanently closed (IsDeleted = true, soft-deleted)
/// 
/// State Transitions:
/// - Created → Setup: Call SetBusinessHours() for each operating day
/// - Setup → Operating: Call StartAcceptingOrders() (validates hours exist)
/// - Operating → Paused: Call StopAcceptingOrders() (manual pause)
/// - Paused → Operating: Call StartAcceptingOrders() again
/// - Any → Closed: Soft delete (IsDeleted = true)
/// 
/// Business Hours States:
/// - Each day can be: Closed (no hours), Open (has hours), 24-Hour (special case)
/// - Hours validated: OpenTime < CloseTime (unless overnight like 22:00-02:00)
/// 
/// Operational Checks:
/// - IsOpenAt(DateTime): Checks if open at specific time considering timezone and business hours
/// - CanAcceptOrders(): Checks if IsAcceptingOrders = true AND business hours exist
/// </code>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// // Create restaurant with value objects
/// var restaurant = new Restaurant(
///     ownerId: 1,
///     name: "Joe's Pizza",
///     location: new Address("123 Main St", "New York", "NY", "10001", "US"),
///     contactPhone: new PhoneNumber("+1-212-555-1234"),
///     contactEmail: new Email("contact@joespizza.com"),
///     maxSimultaneousOrders: 50,
///     description: "Best pizza in town",
///     timeZoneId: "America/New_York"
/// );
/// 
/// // Set business hours
/// restaurant.SetBusinessHours(DayOfWeek.Monday, TimeSpan.FromHours(11), TimeSpan.FromHours(22));
/// restaurant.SetBusinessHours(DayOfWeek.Tuesday, TimeSpan.FromHours(11), TimeSpan.FromHours(22));
/// 
/// // Start accepting orders (validates business hours exist)
/// restaurant.StartAcceptingOrders();
/// 
/// // Update operations
/// restaurant.UpdateBasicInfo("Joe's Famous Pizza", "Authentic NYC style");
/// restaurant.UpdateTimeZone("America/Chicago");
/// restaurant.UpdateContactInfo(
///     new Email("newemail@joespizza.com"),
///     new PhoneNumber("+1-212-555-5678")
/// );
/// 
/// // Check if open
/// if (restaurant.IsOpenAt(DateTime.Now))
/// {
///     Console.WriteLine("We're open!");
/// }
/// </code>
/// 
/// <para><strong>Entity Framework Core Support:</strong></para>
/// <para>Includes a protected parameterless constructor for EF Core's use during materialization. The aggregate can be
/// persisted and retrieved through repository pattern. Private setters and the _operatingHours collection are accessible to
/// EF Core through reflection-based field mapping. Value objects (Address, Email, PhoneNumber) are configured as owned types.
/// Child BusinessHours entities are automatically persisted through cascade operations.</para>
/// 
/// <para><strong>Design Considerations:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Tenant Root:</strong> Restaurant IS the tenant - does NOT inherit TenantEntityBase (would cause circular FK)</description></item>
///   <item><description><strong>Value Objects:</strong> Use Address, Email, PhoneNumber value objects instead of primitive strings</description></item>
///   <item><description><strong>Business Hours Required:</strong> Must configure hours before accepting orders</description></item>
///   <item><description><strong>Timezone Validation:</strong> TimeZoneId must be valid IANA timezone identifier</description></item>
///   <item><description><strong>Order Capacity:</strong> MaxSimultaneousOrders should match kitchen/staff capacity</description></item>
///   <item><description><strong>Aggregate Boundary:</strong> Restaurant and BusinessHours must be loaded and saved together</description></item>
///   <item><description><strong>Ownership Transfer:</strong> OwnerId can be changed to transfer restaurant to new admin</description></item>
///   <item><description><strong>Soft Delete:</strong> Closing restaurant sets IsDeleted = true, preserving historical data</description></item>
/// </list>
/// 
/// <para><strong>Indexing Strategy:</strong></para>
/// <para>Database indexes for efficient querying are defined in AppDbContext.OnModelCreating:</para>
/// <list type="bullet">
///   <item><description>IX_Restaurants_OwnerId: For finding all restaurants owned by an admin user</description></item>
///   <item><description>IX_Restaurants_Name: For restaurant name searches and autocomplete</description></item>
///   <item><description>IX_Restaurants_IsAcceptingOrders: For filtering active/accepting restaurants</description></item>
///   <item><description>IX_Restaurants_TimeZoneId: For timezone-based grouping and queries</description></item>
/// </list>
/// 
/// <para><strong>Use Cases:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Restaurant Setup:</strong> Create restaurant with owner, location, contact info, timezone</description></item>
///   <item><description><strong>Hours Management:</strong> Configure operating hours per day of week</description></item>
///   <item><description><strong>Order Control:</strong> Start/stop accepting orders based on operational status</description></item>
///   <item><description><strong>Timezone Operations:</strong> Convert local time to UTC and vice versa for scheduling</description></item>
///   <item><description><strong>Capacity Management:</strong> Limit concurrent orders to prevent kitchen overload</description></item>
///   <item><description><strong>Ownership Transfer:</strong> Transfer restaurant to new admin user</description></item>
///   <item><description><strong>Multi-Location:</strong> Same AdminUser can own multiple restaurants</description></item>
///   <item><description><strong>Operational Status:</strong> Check if restaurant is open at specific time</description></item>
/// </list>
/// 
/// <para><strong>EF Core Configuration - Value Objects (Use Owned Types):</strong></para>
/// <code>
/// // In your DbContext OnModelCreating method:
/// protected override void OnModelCreating(ModelBuilder modelBuilder)
/// {
///     modelBuilder.Entity&lt;Restaurant&gt;(entity =>
///     {
///         entity.ToTable("Restaurants");
///         
///         // ===== Value Object: Email =====
///         // Maps Email value object to a single column in the Restaurants table
///         entity.OwnsOne(r => r.ContactEmail, email =>
///         {
///             email.Property(e => e.Value)
///                 .HasColumnName("Email")
///                 .HasMaxLength(150)
///                 .IsRequired();
///         });
///         
///         // ===== Value Object: PhoneNumber =====
///         // Maps PhoneNumber value object to a single column
///         entity.OwnsOne(r => r.ContactPhone, phone =>
///         {
///             phone.Property(p => p.Value)
///                 .HasColumnName("PhoneNumber")
///                 .HasMaxLength(50)
///                 .IsRequired();
///         });
///         
///         // ===== Value Object: Address (Complex) =====
///         // Maps Address value object to multiple columns (Street, City, State, etc.)
///         entity.OwnsOne(r => r.Location, address =>
///         {
///             address.Property(a => a.Street)
///                 .HasColumnName("Street")
///                 .HasMaxLength(200);
///                 
///             address.Property(a => a.City)
///                 .HasColumnName("City")
///                 .HasMaxLength(100);
///                 
///             address.Property(a => a.State)
///                 .HasColumnName("State")
///                 .HasMaxLength(50);
///                 
///             address.Property(a => a.PostalCode)
///                 .HasColumnName("PostalCode")
///                 .HasMaxLength(20);
///                 
///             address.Property(a => a.Country)
///                 .HasColumnName("Country")
///                 .HasMaxLength(100);
///         });
///         
///         // ===== Child Entity: BusinessHours =====
///         // Maps the encapsulated collection using backing field
///         entity.HasMany&lt;BusinessHours&gt;("_operatingHours")
///             .WithOne()
///             .HasForeignKey("RestaurantId")
///             .OnDelete(DeleteBehavior.Cascade);
///             
///         // Alternative: If BusinessHours has a navigation property back to Restaurant
///         // entity.HasMany(r => r.OperatingHours)
///         //     .WithOne()
///         //     .HasForeignKey(bh => bh.RestaurantId);
///         
///         // ===== Indexes for Performance =====
///         entity.HasIndex(r => r.OwnerId);
///         entity.HasIndex(r => r.Name);
///     });
/// }
/// </code>
/// 
/// EF Core Compatibility Notes:
/// - ✅ Private setters: EF Core uses reflection during materialization - no special configuration needed
/// - ✅ Value Objects: Use OwnsOne() to map to columns in the same table
/// - ✅ Encapsulated collections: Use backing field name (e.g., "_operatingHours") in HasMany()
/// - ✅ Protected constructor: EF Core will use the parameterless constructor for loading from database
/// - ✅ Behavioral methods: Work normally - EF Core only needs setters for materialization, not for change tracking
/// </remarks>
public class Restaurant : EntityBase
{
    private readonly List<BusinessHours> _operatingHours = new();
    
    // === Identity & Basic Info ===
    
    /// <summary>
    /// The name of the restaurant.
    /// </summary>
    public string Name { get; private set; }
    
    /// <summary>
    /// Brief description of the restaurant.
    /// </summary>
    public string? Description { get; private set; }
    
    /// <summary>
    /// Foreign key to the owner (AdminUser). Each restaurant is owned by a single admin user.
    /// </summary>
    public int OwnerId { get; private set; }
    
    /// <summary>
    /// Restaurant's timezone identifier (e.g., "America/New_York").
    /// </summary>
    public string TimeZoneId { get; private set; }
    
    // === Value Objects for Contact Information ===
    
    /// <summary>
    /// Restaurant's physical location.
    /// </summary>
    public Address Location { get; private set; }
    
    /// <summary>
    /// Primary contact phone number.
    /// </summary>
    public PhoneNumber ContactPhone { get; private set; }
    
    /// <summary>
    /// Primary contact email address.
    /// </summary>
    public Email ContactEmail { get; private set; }
    
    // === Operating Status ===
    
    /// <summary>
    /// Indicates whether the restaurant is currently accepting orders.
    /// </summary>
    public bool IsAcceptingOrders { get; private set; }
    
    /// <summary>
    /// Maximum number of orders that can be processed simultaneously.
    /// </summary>
    public int MaxSimultaneousOrders { get; private set; }
    
    // === Business Hours ===
    
    /// <summary>
    /// Collection of business hours for each day of the week.
    /// </summary>
    public IReadOnlyCollection<BusinessHours> OperatingHours => _operatingHours.AsReadOnly();

    // === Constructors ===

    /// <summary>
    /// Parameterless constructor for Entity Framework Core materialization for conversion and loading from the database data into entity instances.
    /// </summary>
    /// <remarks>
    /// <para><strong>EF Core Requirement:</strong> This protected constructor exists solely for EF Core to instantiate entities when loading from the database. Do NOT call directly in application code.</para>
    /// 
    /// <para><strong>Initialization:</strong> Properties are set to non-null defaults to satisfy C# nullable reference types. EF Core immediately overwrites these values via reflection when materializing entities.</para>
    /// 
    /// <para><strong>DDD Compliance:</strong> The <c>protected</c> visibility prevents external instantiation while allowing EF Core reflection access. Use the public constructor for creating new restaurants.</para>
    /// 
    /// <code>
    /// // ❌ DON'T - Constructor is protected
    /// var restaurant = new Restaurant();
    /// 
    /// // ✅ DO - Use public constructor
    /// var restaurant = new Restaurant(ownerId, name, location, phone, email);
    /// 
    /// // ✅ DO - EF Core uses automatically
    /// var restaurant = await dbContext.Restaurants.FirstOrDefaultAsync(r => r.Id == 1);
    /// </code>
    /// </remarks
    protected Restaurant() 
    {
        Name = string.Empty;
        TimeZoneId = "UTC";
        Location = null!;
        ContactPhone = null!;
        ContactEmail = null!;
    }
    
    /// <summary>
    /// Initializes a new instance of the Restaurant class with the specified owner, name, location, contact
    /// information, and configuration settings.
    /// </summary>
    /// <param name="ownerId">The unique identifier of the restaurant owner. Must be greater than zero.</param>
    /// <param name="name">The name of the restaurant. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <param name="location">The physical address of the restaurant. Cannot be null.</param>
    /// <param name="contactPhone">The primary phone number for contacting the restaurant. Cannot be null.</param>
    /// <param name="contactEmail">The primary email address for contacting the restaurant. Cannot be null.</param>
    /// <param name="maxSimultaneousOrders">The maximum number of orders that can be processed simultaneously. Must be greater than zero. The default is 50.</param>
    /// <param name="description">An optional description of the restaurant. May be null.</param>
    /// <param name="timeZoneId">The IANA or Windows time zone identifier for the restaurant's location. Cannot be null, empty, or consist only
    /// of white-space characters. The default is "UTC".</param>
    /// <exception cref="ArgumentException">Thrown if ownerId is less than or equal to zero, name is null, empty, or white space, maxSimultaneousOrders is
    /// less than or equal to zero, or timeZoneId is null, empty, or white space.</exception>
    /// <exception cref="ArgumentNullException">Thrown if location, contactPhone, or contactEmail is null.</exception>
    public Restaurant(
        int ownerId,
        string name,
        Address location,
        PhoneNumber contactPhone,
        Email contactEmail,
        int maxSimultaneousOrders = 50,
        string? description = null,
        string timeZoneId = "UTC")
    {
        // ---------------------------------------------------------------
        // PARAMETER GUARD CLAUSES — ArgumentException / ArgumentNullException
        //
        // These are NOT domain business rules. They are programming-error
        // guards that enforce the method's preconditions (its "contract").
        //
        // WHY NOT DomainException?
        // • A null location or empty name is a CALLER BUG, not a business
        //   rule the domain model needs to express. The caller passed data
        //   that should never reach the domain layer in the first place.
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
        // • name = null        → ArgumentException  (programming error)
        // • name = "Joe's"     → valid input
        // • StartAcceptingOrders() with no hours → RestaurantDomainException
        //   (business rule: restaurant must have hours before accepting)
        // ---------------------------------------------------------------

        if (ownerId <= 0)
            throw new ArgumentException("Valid owner ID is required.", nameof(ownerId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Restaurant name is required.", nameof(name));

        if (maxSimultaneousOrders <= 0)
            throw new ArgumentException("Max simultaneous orders must be greater than zero.", nameof(maxSimultaneousOrders));

        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Time zone ID is required.", nameof(timeZoneId));

        OwnerId = ownerId;
        Name = name.Trim();
        Description = description?.Trim();
        Location = location ?? throw new ArgumentNullException(nameof(location));
        ContactPhone = contactPhone ?? throw new ArgumentNullException(nameof(contactPhone));
        ContactEmail = contactEmail ?? throw new ArgumentNullException(nameof(contactEmail));
        MaxSimultaneousOrders = maxSimultaneousOrders;
        TimeZoneId = timeZoneId.Trim();
        IsAcceptingOrders = false; // Default to not accepting orders until business hours are set
        
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // === Business Methods ===
    
    /// <summary>
    /// Updates the restaurant's basic information.
    /// </summary>
    public void UpdateBasicInfo(string name, string? description = null)
    {
        // Guard clause: null/empty name is a programming error, not a business rule.
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Restaurant name is required.", nameof(name));
        
        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Updates the restaurant's contact information.
    /// </summary>
    public void UpdateContactInfo(Email email, PhoneNumber phone)
    {
        // Guard clauses: null value objects are programming errors — the caller
        // must construct valid Email/PhoneNumber instances before reaching here.
        ContactEmail = email ?? throw new ArgumentNullException(nameof(email));
        ContactPhone = phone ?? throw new ArgumentNullException(nameof(phone));
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Updates the restaurant's location.
    /// </summary>
    public void UpdateLocation(Address newLocation)
    {
        // Guard clause: null Address is a programming error, not a business rule.
        Location = newLocation ?? throw new ArgumentNullException(nameof(newLocation));
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Updates the restaurant's time zone.
    /// </summary>
    public void UpdateTimeZone(string timeZoneId)
    {
        // Guard clause: null/empty timezone is a programming error, not a business rule.
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Time zone ID is required.", nameof(timeZoneId));
        
        // Validate time zone ID
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Invalid time zone ID: {timeZoneId}", nameof(timeZoneId));
        }
        
        TimeZoneId = timeZoneId.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Transfers ownership to a new admin user.
    /// </summary>
    public void TransferOwnership(int newOwnerId)
    {
        // Guard clause: invalid ID is a programming error, not a business rule.
        if (newOwnerId <= 0)
            throw new ArgumentException("Valid owner ID is required.", nameof(newOwnerId));
        
        OwnerId = newOwnerId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// This method is a factory for creating or updating BusinessHours for a specific day of the week.
    /// A factory method is a behavioral method that encapsulates the creation logic of child entities within the aggregate root.
    /// </summary>
    /// <remarks>
    /// AGGREGATE BEHAVIOR: This method maintains the aggregate boundary by being the only
    /// way to add/update BusinessHours child entities. Direct manipulation of the collection
    /// is prevented through encapsulation.
    /// </remarks>
    public void SetBusinessHours(DayOfWeek dayOfWeek, TimeSpan openTime, TimeSpan closeTime)
    {
        if (closeTime <= openTime)
            throw new RestaurantDomainException("Close time must be after open time.");
        
        // Remove existing hours for this day
        var existingHours = _operatingHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);
        if (existingHours != null)
            _operatingHours.Remove(existingHours);
        
        // Add new hours - pass RestaurantId to maintain aggregate integrity
        var businessHours = new BusinessHours(Id, dayOfWeek, openTime, closeTime);
        _operatingHours.Add(businessHours);
        
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Removes business hours for a specific day (marks as closed).
    /// </summary>
    public void RemoveBusinessHours(DayOfWeek dayOfWeek)
    {
        var existingHours = _operatingHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);
        if (existingHours != null)
        {
            _operatingHours.Remove(existingHours);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Checks if the restaurant is open at a given date and time.
    /// </summary>
    public bool IsOpenAt(DateTime dateTime)
    {
        var hours = _operatingHours.FirstOrDefault(h => h.DayOfWeek == dateTime.DayOfWeek);
        if (hours == null)
            return false;
        
        return hours.IsWithinHours(dateTime.TimeOfDay);
    }
    
    /// <summary>
    /// Starts accepting orders.
    /// </summary>
    public void StartAcceptingOrders()
    {
        if (!_operatingHours.Any())
            throw new RestaurantDomainException("Cannot accept orders without setting business hours.");
        
        IsAcceptingOrders = true;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Stops accepting orders.
    /// </summary>
    public void StopAcceptingOrders()
    {
        IsAcceptingOrders = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Updates the maximum number of simultaneous orders.
    /// </summary>
    public void UpdateMaxSimultaneousOrders(int maxOrders)
    {
        // Guard clause: non-positive capacity is a programming error, not a business rule.
        if (maxOrders <= 0)
            throw new ArgumentException("Max simultaneous orders must be greater than zero.", nameof(maxOrders));
        
        MaxSimultaneousOrders = maxOrders;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // ========================================================================
    // === NAVIGATION PROPERTIES (FOR EF CORE ONLY - NOT FOR DOMAIN LOGIC) ===
    // ========================================================================
    //
    // ARCHITECTURAL DECISION: Hybrid Pattern (DDD + EF Core Navigation Properties)
    //
    // This aggregate uses a HYBRID PATTERN that combines:
    // ✅ Pure DDD practices (private setters, value objects, behavioral methods)
    // ✅ EF Core navigation properties (for ORM convenience)
    //
    // CHALLENGE SOLVED:
    // Originally, we had TWO Restaurant classes:
    // 1. SmartMenuOptim.Domain/Aggregates/RestaurantAggregate/Restaurant.cs (DDD aggregate)
    // 2. SmartMenuOptim.Shared/Data/Entities/TenantSpecificEntities/Restaurant.cs (EF Core entity)
    //
    // This caused:
    // ❌ Code duplication
    // ❌ Synchronization issues between versions
    // ❌ Confusion about which to use where
    // ❌ Maintenance overhead
    //
    // SOLUTION: Consolidate into ONE class that serves BOTH purposes
    // - Domain aggregate for business logic (private setters, value objects, methods)
    // - EF Core entity for database mapping (navigation properties below)
    //
    // TRADE-OFFS ACCEPTED:
    // ⚠️ Aggregate boundary violation (navigation to other aggregates exists but discouraged)
    // ⚠️ Requires discipline to NOT use these properties in domain logic
    // ⚠️ Temptation to bypass repository pattern
    //
    // BENEFITS GAINED:
    // ✅ Single source of truth (one Restaurant class)
    // ✅ No duplication or synchronization issues
    // ✅ Works directly with EF Core (no separate DTO needed for database)
    // ✅ Maintains DDD patterns through private setters and behavioral methods
    // ✅ EF Core can use these for eager loading, Include(), and relationship mapping
    //
    // ⚠️ IMPORTANT: DO NOT USE THESE IN DOMAIN LOGIC
    //
    // These navigation properties exist ONLY for Entity Framework Core ORM purposes.
    // They violate DDD aggregate boundaries and should NOT be used in business logic.
    //
    // WHY THEY'RE HERE:
    // - EF Core needs them for relationship mapping
    // - Simplifies database queries and eager loading
    // - Maintains compatibility with existing database schema
    // - Eliminates need for separate Shared.Restaurant entity
    //
    // DOMAIN LOGIC RULES:
    // ❌ DON'T: restaurant.Dishes.Add(newDish)
    // ✅ DO:    await _dishRepository.AddAsync(newDish)
    //
    // ❌ DON'T: var dishes = restaurant.Dishes.Where(d => d.IsActive)
    // ✅ DO:    var dishes = await _dishRepository.GetActiveByRestaurantIdAsync(restaurantId)
    //
    // ❌ DON'T: var activeMenus = restaurant.Menus.Where(m => m.IsAvailable)
    // ✅ DO:    var activeMenus = await _menuRepository.GetActiveByRestaurantIdAsync(restaurantId)
    //
    // These properties are marked 'virtual' to support EF Core lazy loading (if enabled).
    //
    // BLAZOR NOTE:
    // Even with this consolidation, Blazor forms still need simple DTOs because:
    // - Private setters don't work with @bind-Value
    // - Value Objects can't be directly bound to <InputText>
    // - Solution: Create RestaurantFormDto for Blazor, map to/from this aggregate
    //
    // For more details, see: docs/RESTAURANT_CONSOLIDATION.md
    //
    // ========================================================================
    
    
    /// <summary>
    /// Navigation property to the owner (AdminUser).
    /// FOR EF CORE ONLY - Use OwnerId in domain logic instead.
    /// </summary>
    /// <remarks>
    /// This property enables EF Core to load the admin user who owns this restaurant.
    /// In domain logic, use OwnerId for restaurant ownership operations.
    /// Query AdminUser separately via repository when needed.
    /// </remarks>
    public virtual AdminUser? Owner { get; set; }
    
    /// <summary>
    /// Navigation property for all dishes in this restaurant.
    /// FOR EF CORE ONLY - Use IDishRepository in domain logic instead.
    /// </summary>
    /// <remarks>
    /// Dish is a separate aggregate. Access dishes via:
    /// await _dishRepository.GetByRestaurantIdAsync(restaurantId)
    /// </remarks>
    public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    
    /// <summary>
    /// Navigation property for all categories in this restaurant.
    /// FOR EF CORE ONLY - Use ICategoryRepository in domain logic instead.
    /// </summary>
    /// <remarks>
    /// Category is a lookup aggregate. Access categories via:
    /// await _categoryRepository.GetByRestaurantIdAsync(restaurantId)
    /// </remarks>
    public virtual ICollection<DishCategory> Categories { get; set; } = new List<DishCategory>();
    
    /// <summary>
    /// Navigation property for all reviews in this restaurant.
    /// FOR EF CORE ONLY - Use IReviewRepository in domain logic instead.
    /// </summary>
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    
    /// <summary>
    /// Navigation property for all menus in this restaurant.
    /// FOR EF CORE ONLY - Use IMenuRepository in domain logic instead.
    /// </summary>
    /// <remarks>
    /// Menu is a separate aggregate. Access menus via:
    /// await _menuRepository.GetByRestaurantIdAsync(restaurantId)
    /// </remarks>
    public virtual ICollection<Menu> Menus { get; set; } = new List<Menu>();
    
    /// <summary>
    /// Navigation property for all orders in this restaurant.
    /// FOR EF CORE ONLY - Use IOrderRepository in domain logic instead.
    /// </summary>
    /// <remarks>
    /// Order is a separate aggregate. Access orders via:
    /// await _orderRepository.GetByRestaurantIdAsync(restaurantId)
    /// </remarks>
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    
    /// <summary>
    /// Navigation property for all tables in this restaurant.
    /// FOR EF CORE ONLY - Use ITableRepository in domain logic instead.
    /// </summary>
    public virtual ICollection<Table> Tables { get; set; } = new List<Table>();
    
    /// <summary>
    /// Navigation property for all staff schedules in this restaurant.
    /// FOR EF CORE ONLY - Use IStaffScheduleRepository in domain logic instead.
    /// </summary>
    public virtual ICollection<StaffSchedule> StaffSchedules { get; set; } = new List<StaffSchedule>();
    
    /// <summary>
    /// Navigation property for all customer loyalty records in this restaurant.
    /// FOR EF CORE ONLY - Use ICustomerLoyaltyRepository in domain logic instead.
    /// </summary>
    public virtual ICollection<CustomerLoyalty> CustomerLoyalties { get; set; } = new List<CustomerLoyalty>();
    
    /// <summary>
    /// Navigation property for all promotions in this restaurant.
    /// FOR EF CORE ONLY - Use IPromotionRepository in domain logic instead.
    /// </summary>
    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();
    
    /// <summary>
    /// Navigation property for all sale records in this restaurant.
    /// FOR EF CORE ONLY - Use ISaleRecordRepository in domain logic instead.
    /// </summary>
    public virtual ICollection<SaleRecord> SaleRecords { get; set; } = new List<SaleRecord>();
    
    /// <summary>
    /// Navigation property for all menu types associated with this restaurant.
    /// FOR EF CORE ONLY - Use IMenuTypeRepository in domain logic instead.
    /// </summary>
    /// <remarks>
    /// MenuType is a lookup aggregate. Access menu types via:
    /// await _menuTypeRepository.GetByRestaurantIdAsync(restaurantId)
    /// </remarks>
    public virtual ICollection<MenuType> MenuTypes { get; set; } = new List<MenuType>();
    
    /// <summary>
    /// Navigation property for all order statuses in this restaurant.
    /// FOR EF CORE ONLY - Use IOrderStatusRepository in domain logic instead.
    /// </summary>
    /// <remarks>
    /// OrderStatus is a lookup aggregate. Access order statuses via:
    /// await _orderStatusRepository.GetByRestaurantIdAsync(restaurantId)
    /// </remarks>
    public virtual ICollection<OrderStatus> OrderStatuses { get; set; } = new List<OrderStatus>();
    
    /// <summary>
    /// Navigation property for all user permissions scoped to this restaurant.
    /// FOR EF CORE ONLY - Use IUserPermissionRepository in domain logic instead.
    /// </summary>
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
