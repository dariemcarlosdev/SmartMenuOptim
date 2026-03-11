using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Features.Restaurants;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;

namespace SmartMenuOptim.Domain.Entities.ProfileEntities
{

    /// <example>
    /// Common Query Patterns Supported:
    /// 
    /// 1. Authentication Queries (IX_StaffMembers_Email_Username_Unique):
    /// ```csharp
    /// // Login validation
    /// var staff = await dbContext.StaffMembers
    ///     .FirstOrDefaultAsync(s => s.Email == email && s.Username == username);
    /// 
    /// // Email availability check
    /// var isEmailAvailable = !await dbContext.StaffMembers
    ///     .AnyAsync(s => s.Email == email);
    /// ```
    /// 
    /// 2. Staff Management Queries (IX_StaffMembers_Restaurant_Role_Status):
    /// ```csharp
    /// // Find all active waiters in a restaurant
    /// var waiters = await dbContext.StaffMembers
    ///     .Where(s => s.RestaurantId == restaurantId 
    ///         && s.Role == StaffRole.Waiter 
    ///         && s.EmploymentStatus == EmploymentStatus.FullTime
    ///         && s.IsActive)
    ///     .ToListAsync();
    /// 
    /// // Count staff by role
    /// var staffCounts = await dbContext.StaffMembers
    ///     .Where(s => s.RestaurantId == restaurantId && s.IsActive)
    ///     .GroupBy(s => s.Role)
    ///     .Select(g => new { Role = g.Key, Count = g.Count() })
    ///     .ToListAsync();
    /// ```
    /// 
    /// 3. Contact Information Queries (IX_StaffMembers_Phone_Verified):
    /// ```csharp
    /// // Find staff by phone number
    /// var staff = await dbContext.StaffMembers
    ///     .FirstOrDefaultAsync(s => s.PhoneNumber == phone 
    ///         && s.PhoneNumberConfirmed);
    /// 
    /// // Get all staff with unverified phones
    /// var unverifiedStaff = await dbContext.StaffMembers
    ///     .Where(s => s.PhoneNumber != null 
    ///         && !s.PhoneNumberConfirmed)
    ///     .ToListAsync();
    /// ```
    /// 
    /// 4. Schedule Management Queries:
    /// ```csharp
    /// // Get available staff for a shift
    /// var availableStaff = await dbContext.StaffMembers
    ///     .Where(s => s.RestaurantId == restaurantId
    ///         && s.IsActive
    ///         && s.EmploymentStatus == EmploymentStatus.FullTime
    ///         && !s.Schedules.Any(sch => 
    ///             sch.ShiftStart <= shiftEnd && 
    ///             sch.ShiftEnd >= shiftStart))
    ///     .ToListAsync();
    /// 
    /// // Get staff with orders in date range
    /// var activeStaff = await dbContext.StaffMembers
    ///     .Where(s => s.RestaurantId == restaurantId
    ///         && s.HandledOrders.Any(o => 
    ///             o.OrderDate >= startDate && 
    ///             o.OrderDate <= endDate))
    ///     .ToListAsync();
    /// ```
    /// 
    /// 5. Employee Status Queries:
    /// ```csharp
    /// // Get terminated employees
    /// var formerStaff = await dbContext.StaffMembers
    ///     .Where(s => s.EmploymentStatus == EmploymentStatus.Terminated
    ///         && s.TerminationDate <= DateTime.UtcNow)
    ///     .ToListAsync();
    /// 
    /// // Get staff on leave
    /// var onLeaveStaff = await dbContext.StaffMembers
    ///     .Where(s => s.EmploymentStatus == EmploymentStatus.OnLeave)
    ///     .ToListAsync();
    /// ```
    /// </example>

    /// <summary>
    /// Represents a staff member who works at a restaurant.
    /// </summary>
    /// <remarks>
    /// Relationship with ApplicationUser:
    /// - One-to-One relationship where StaffMember acts as a profile entity for ApplicationUser
    /// - StaffMember.Id is an auto-incrementing integer primary key
    /// - StaffMember.ApplicationUserId is a string foreign key that matches ApplicationUser.Id (GUID)
    /// - This creates a separation between authentication (ApplicationUser) and business logic (StaffMember)
    /// 
    /// Key Design Points:
    /// 1. Identity Separation:
    ///    - ApplicationUser handles authentication/identity (inherits from IdentityUser)
    ///    - StaffMember handles business logic and staff-specific properties
    /// 
    /// 2. Key Strategy:
    ///    - StaffMember uses integer Id for internal references
    ///    - ApplicationUserId links to the ASP.NET Identity user (string GUID)
    ///    - This allows efficient indexing while maintaining Identity integration
    /// 
    /// 3. Property Delegation:
    ///    - Email and UserName properties delegate to ApplicationUser
    ///    - Maintains single source of truth for identity information
    ///    - Avoids duplication while providing convenient access
    /// 
    /// Multi-Tenancy Notes:
    /// - StaffMember is tenant-specific (unlike AdminUser which is global)
    /// - Each StaffMember belongs to exactly one Restaurant (tenant)
    /// - Uses RestaurantId to maintain tenant boundary
    /// </remarks>
    [Table("StaffMembers")]
    public class StaffMember : EntityBase
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
        /// Gets or sets the email address of the staff member.
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
        /// Gets or sets the username of the staff member.
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

        /// <summary>
        /// Staff member's full name.
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s-']+$", ErrorMessage = "Name can only contain letters, spaces, hyphens and apostrophes")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Staff member's role in the restaurant.
        /// </summary>
        [Required(ErrorMessage = "Staff role is required")]
        [EnumDataType(typeof(StaffRole))]
        public StaffRole Role { get; set; }

        /// <summary>
        /// Date when the staff member was hired.
        /// </summary>
        [Required(ErrorMessage = "Hire date is required")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Staff member's phone number for contact purposes
        /// </summary>
        [Phone(ErrorMessage = "Invalid phone number format")]
        [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Whether the staff member's phone number has been verified
        /// </summary>
        public bool PhoneNumberConfirmed { get; set; }

        /// <summary>
        /// Emergency contact name for the staff member
        /// </summary>
        [MaxLength(100, ErrorMessage = "Emergency contact name cannot exceed 100 characters")]
        public string? EmergencyContactName { get; set; }

        /// <summary>
        /// Emergency contact phone number
        /// </summary>
        [Phone(ErrorMessage = "Invalid emergency contact phone number format")]
        [MaxLength(20, ErrorMessage = "Emergency contact phone number cannot exceed 20 characters")]
        public string? EmergencyContactPhone { get; set; }

        // === Employment Information ===

        /// <summary>
        /// Staff member's employment status (e.g., Full-time, Part-time)
        /// </summary>
        [Required(ErrorMessage = "Employment status is required")]
        [EnumDataType(typeof(EmploymentStatus))]
        public EmploymentStatus EmploymentStatus { get; set; }

        /// <summary>
        /// Date when the staff member's employment ended (if applicable)
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? TerminationDate { get; set; }

        /// <summary>
        /// Notes about the staff member (internal use)
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }


        // === Foreign Keys ===

        /// <summary>
        /// Gets or sets the unique identifier of the associated application user.
        /// </summary>
        /// <remarks>This property serves as a foreign key reference to the application's user entity. The
        /// value must not be null and is limited to a maximum length of 450 characters to match the default key length
        /// used by ASP.NET Identity.</remarks>
        [Required]
        [MaxLength(450)] // Matches Identity's key length
        public string ApplicationUserId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the unique identifier of the associated restaurant. This is the foreign key
        /// </summary>
        /// <remarks>This property is used to establish a relationship with a restaurant entity. The value
        /// must correspond to a valid restaurant record in the database.</remarks>
        [Required(ErrorMessage = "Restaurant assignment is required")]
        [ForeignKey(nameof(Restaurant))]
        public virtual int RestaurantId { get; set; }

        // === Navigation Properties ===

        ////Declared as virtual for lazy loading support. Lazy loading enables EF Core to create proxy classes that support lazy loading of related entities.
        ///Lazy loading can improve performance by deferring the loading of related data until it is actually needed.

        /// <summary>
        /// Navigation property to the identity `ApplicationUser`.
        /// </summary>
        [ForeignKey(nameof(ApplicationUserId))]
        public virtual ApplicationUser ApplicationUser { get; set; } = null!;

        /// <summary>
        /// Navigation property to the associated restaurant.
        /// </summary>
        ///
        [ForeignKey(nameof(RestaurantId))]
        public virtual Restaurant? Restaurant { get; set; } //Declared as virtual for lazy loading support.

        /// <summary>
        /// Navigation property for the staff member's schedules.
        /// </summary>
        [InverseProperty(nameof(StaffSchedule.StaffMember))]
        public virtual ICollection<StaffSchedule> Schedules { get; set; } = [];

        /// <summary>
        /// Navigation property for orders handled by the staff member.
        /// </summary>
        [InverseProperty(nameof(Order.HandledBy))]
        public virtual ICollection<Order> HandledOrders { get; set; } = [];
        
        public int CompletedShifts { get; private set; }
        public int MissedShifts { get; private set; }
        public DateTime LastShiftDate { get; private set; }


        //----- Business Logic Methods (if any) can be added here -----

        // === Business Logic Methods ===
        /// <summary>
        /// Checks if the staff member is eligible for a shift at the specified time.
        /// </summary>
        public bool IsAvailableForShift(DateTime shiftStart, DateTime shiftEnd)
        {
            if (!IsActive || EmploymentStatus != EmploymentStatus.ActiveAndWorking)
                return false;

            // Check if shift conflicts with existing schedules
            return !Schedules.Any(s => s.ShiftStart < shiftEnd && s.ShiftEnd > shiftStart);
        }

        /// <summary>
        /// Calculates total hours scheduled for a given week
        /// </summary>
        public double GetScheduledHoursForWeek(DateTime weekStartDate)
        {
            var weekEndDate = weekStartDate.AddDays(7);
            return Schedules
                .Where(s => s.ShiftStart >= weekStartDate && s.ShiftStart < weekEndDate)
                .Sum(s => (s.ShiftEnd - s.ShiftStart).TotalHours);
        }

        /// <summary>
        /// Updates the performance metrics based on completed shift
        /// </summary>
        public void RecordShiftCompletion(bool wasPresent)
        {
            if (wasPresent)
                CompletedShifts++;
            else
                MissedShifts++;

            LastShiftDate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Specifies the employment status of staff members
    /// </summary>
    public enum EmploymentStatus
    {
        /// <summary>
        /// Full-time employee working standard hours
        /// </summary>
        FullTime = 0,

        /// <summary>
        /// Part-time employee working reduced hours
        /// </summary>
        PartTime = 1,

        /// <summary>
        /// Temporary or seasonal employee
        /// </summary>
        Temporary = 2,

        /// <summary>
        /// Employee on leave (medical, personal, etc.)
        /// </summary>
        OnLeave = 3,

        /// <summary>
        /// Former employee no longer with the organization
        /// </summary>
        Terminated = 4,

        /// <summary>
        /// Indicates if the staff member is currently active and working
        /// </summary>
        ActiveAndWorking = 5
    }

    /// <summary>
    /// Specifies the various staff roles available in a restaurant environment.
    /// </summary>
    /// <remarks>
    /// Use this enumeration to identify or assign specific responsibilities to staff members.
    /// Each role has distinct responsibilities and access levels within the system.
    /// </remarks>
    public enum StaffRole
    {
        /// <summary>
        /// Serves customers and manages orders
        /// </summary>
        Waiter = 0,

        /// <summary>
        /// Prepares dishes and manages kitchen operations
        /// </summary>
        Chef = 1,

        /// <summary>
        /// Oversees restaurant operations and staff
        /// </summary>
        Manager = 2,

        /// <summary>
        /// Greets and seats customers
        /// </summary>
        Host = 3,

        /// <summary>
        /// Cleans tables and maintains dining area
        /// </summary>
        Busser = 4,

        /// <summary>
        /// Prepares and serves beverages
        /// </summary>
        Bartender = 5
    }

}