using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;

namespace SmartMenuOptim.Shared.Data.Entities.GlobalEntities
{
    /// <summary>
    /// Represents a staff member who works at a restaurant. Extends UserBase for authentication
    /// while maintaining restaurant-specific associations.
    /// </summary>
    /// <remarks>
    /// Hybrid Tenancy Model: This entity combines global identity (UserBase) with tenant-specific assignment.
    /// Staff members have global authentication but are primarily associated with a specific restaurant.
    /// </remarks>
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
    [Table("StaffMembers")]
    /// <summary>
    /// Primary composite index for authentication and identity management:
    /// 1. Ensures uniqueness of both email and username
    /// 2. Optimizes authentication queries that check both fields
    /// 3. Improves performance for user lookup operations
    /// 4. Supports efficient duplicate detection
    /// </summary>
    [Index(nameof(Email), nameof(Username), IsUnique = true, Name = "IX_StaffMembers_Email_Username_Unique")]
    /// <summary>
    /// Composite index for staff management and scheduling:
    /// 1. Optimizes queries that filter by restaurant, role, and employment status
    /// 2. Improves performance for staff availability checks
    /// 3. Supports efficient staff assignment operations
    /// 4. Enables quick filtering of active staff by role
    /// </summary>
    [Index(nameof(RestaurantId), nameof(Role), nameof(EmploymentStatus), nameof(IsActive), 
           Name = "IX_StaffMembers_Restaurant_Role_Status")]
    /// <summary>
    /// Composite index for contact and emergency information:
    /// 1. Optimizes queries that need to find staff by phone number
    /// 2. Supports emergency contact lookups
    /// 3. Enables efficient communication-related queries
    /// 4. Includes verification status for phone-based features
    /// </summary>
    [Index(nameof(PhoneNumber), nameof(PhoneNumberConfirmed), 
           Name = "IX_StaffMembers_Phone_Verified")]
    /// <summary>
    /// Unique index on Email to:
    /// 1. Ensure each staff member has a unique email across the system
    /// 2. Optimize authentication queries that use email
    /// 3. Support fast lookups during login and password reset flows
    /// </summary>
    [Index(nameof(Email), IsUnique = true, Name = "IX_StaffMembers_Email_Unique")]
    /// <summary>
    /// Unique index on Username to:
    /// 1. Enforce unique usernames across all staff members
    /// 2. Speed up authentication lookups by username
    /// 3. Support efficient user identity validation
    /// </summary>
    [Index(nameof(Username), IsUnique = true, Name = "IX_StaffMembers_Username_Unique")]
    /// <summary>
    /// Composite index on RestaurantId and Role to optimize:
    /// 1. Queries that filter staff by restaurant and role (e.g., "all waiters in restaurant X")
    /// 2. Staff management operations that need to find employees by role within a restaurant
    /// 3. Schedule management that needs to find available staff by role
    /// 4. Permission checks that depend on both restaurant context and staff role
    /// </summary>
    [Index(nameof(RestaurantId), nameof(Role), Name = "IX_StaffMembers_Restaurant_Role")]
    /// <summary>
    /// Composite index for fast authentication/user lookups
    /// - Speeds up queries filtering by PhoneNumber and PhoneNumberConfirmed
    /// - Supports efficient login and user retrieval operations
    /// </summary>
    [Index(nameof(Username), nameof(IsActive), Name = "IX_Customers_Username_Active")]
    public class StaffMember : UserBase
    {
        // === Personal Information ===

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

        /// <summary>
        /// Primary restaurant where the staff member works.
        /// </summary>
        [Required(ErrorMessage = "Restaurant assignment is required")]
        [ForeignKey(nameof(Restaurant))]
        public int RestaurantId { get; set; }

        /// <summary>
        /// Navigation property to the associated restaurant.
        /// </summary>
        public Restaurant Restaurant { get; set; }

        // === Additional Properties ===

        /// <summary>
        /// Staff member's work schedule and availability.
        /// </summary>
        [InverseProperty(nameof(StaffSchedule.StaffMember))]
        public ICollection<StaffSchedule> Schedules { get; set; } = [];

        /// <summary>
        /// Orders handled by this staff member.
        /// </summary>
        [InverseProperty(nameof(Order.HandledBy))]
        public ICollection<Order> HandledOrders { get; set; } = [];
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
        Terminated = 4
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