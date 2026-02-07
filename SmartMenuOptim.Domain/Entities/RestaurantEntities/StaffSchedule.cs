using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartMenuOptim.Domain.Entities.ProfileEntities;

namespace SmartMenuOptim.Domain.Entities.RestaurantEntities
{
    /// <summary>
    /// Represents a staff member's work schedule for a specific restaurant.
    /// </summary>
    /// <remarks>
    /// DDD Entity Characteristics (Tier 3 - Simple Entity):
    /// - Anemic domain model (data-focused, minimal behavior)
    /// - Public setters for properties (no encapsulation)
    /// - Validation through data annotations and IValidatableObject
    /// - Tenant-scoped entity: RestaurantId for multi-tenancy isolation
    /// - Entity with identity (inherited Id from TenantEntityBase)
    /// 
    /// Tier 3 Principles (Simple Entity):
    /// - POCO-style entity with public properties
    /// - Validation through attributes and IValidatableObject
    /// - No behavioral methods (except validation helpers)
    /// - Data transfer and persistence focused
    /// - EF Core compatible with parameterless constructor
    /// 
    /// Multi-Tenancy:
    /// - Every schedule belongs to exactly one restaurant (RestaurantId)
    /// - Staff member must belong to the same restaurant
    /// - Admin users must have permission for the restaurant
    /// - Enforced through ValidateTenantConsistency()
    /// 
    /// Business Rules:
    /// - Shift end must be after shift start
    /// - Shift duration: minimum 30 minutes, maximum 24 hours
    /// - Recurring schedules must specify a day of week
    /// - Cannot schedule more than 6 months in advance
    /// - Staff member must be active and not deleted
    /// - No overlapping shifts for the same staff member
    /// - Completed status only for past shifts
    /// - Admin audit trail required (created by, modified by)
    /// 
    /// Status Workflow:
    /// - Pending: Schedule created, awaiting approval
    /// - Approved: Schedule confirmed by management
    /// - Completed: Shift has been worked
    /// - Cancelled: Shift cancelled by management
    /// - SickLeave: Staff member called in sick
    /// - Vacation: Scheduled time off
    /// - NeedsCoverage: Shift needs a replacement
    /// 
    /// Usage Example:
    /// <code>
    /// var schedule = new StaffSchedule
    /// {
    ///     RestaurantId = 1,
    ///     StaffMemberId = 5,
    ///     ShiftStart = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
    ///     ShiftEnd = DateTime.UtcNow.AddDays(1).Date.AddHours(17),
    ///     Status = ScheduleStatus.Pending,
    ///     CreatedByAdminUserId = adminId,
    ///     Notes = "Morning shift"
    /// };
    /// 
    /// // Validate tenant consistency after loading navigation properties
    /// schedule.ValidateTenantConsistency();
    /// </code>
    /// </remarks>
    [Table("StaffSchedules")]
    public class StaffSchedule : TenantEntityBase, IValidatableObject
    {
        // ===================================================================
        // SHIFT TIMING PROPERTIES
        // ===================================================================

        /// <summary>
        /// Gets or sets the scheduled start time of the shift (UTC).
        /// </summary>
        /// <remarks>
        /// - Must be before ShiftEnd
        /// - Cannot be more than 6 months in the future
        /// - Used to determine shift duration and overlap detection
        /// </remarks>
        [Required]
        public DateTime ShiftStart { get; set; }

        /// <summary>
        /// Gets or sets the scheduled end time of the shift (UTC).
        /// </summary>
        /// <remarks>
        /// - Must be after ShiftStart
        /// - Shift duration must be between 30 minutes and 24 hours
        /// - Used to determine when shift is complete
        /// </remarks>
        [Required]
        public DateTime ShiftEnd { get; set; }

        // ===================================================================
        // RECURRENCE PROPERTIES
        // ===================================================================

        /// <summary>
        /// Gets or sets a value indicating whether this schedule repeats weekly.
        /// </summary>
        /// <remarks>
        /// - When true, RecurringDay must be specified
        /// - Recurring schedules repeat every week on the specified day
        /// - Non-recurring schedules are one-time events
        /// </remarks>
        public bool IsRecurring { get; set; }

        /// <summary>
        /// Gets or sets the day of the week on which the recurring shift occurs.
        /// </summary>
        /// <remarks>
        /// - Required when IsRecurring is true
        /// - Must be a valid DayOfWeek enum value (Sunday = 0 through Saturday = 6)
        /// - Null for non-recurring schedules
        /// </remarks>
        public DayOfWeek? RecurringDay { get; set; }

        // ===================================================================
        // STATUS AND NOTES
        // ===================================================================

        /// <summary>
        /// Gets or sets the current status of the schedule.
        /// </summary>
        /// <remarks>
        /// Status Workflow:
        /// - Pending: Initial state, awaiting approval
        /// - Approved: Confirmed by management
        /// - Completed: Shift has been worked (only for past shifts)
        /// - Cancelled: Shift cancelled by management
        /// - SickLeave: Staff member called in sick
        /// - Vacation: Scheduled time off
        /// - NeedsCoverage: Requires replacement staff
        /// </remarks>
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Pending; // Default to Pending. The Status property will be automatically set to Pending in the constructors, so no changes are needed in DbSeeder.cs. 

        /// <summary>
        /// Gets or sets optional notes or comments associated with the schedule.
        /// </summary>
        /// <remarks>
        /// - Maximum length: 500 characters
        /// - Can contain shift-specific instructions, special requirements, or other relevant information
        /// - Examples: "Bring food handler certification", "Training new employee", "Cover for manager"
        /// </remarks>
        [MaxLength(500)]
        public string? Notes { get; set; }

        // ===================================================================
        // STAFF ASSIGNMENT
        // ===================================================================

        /// <summary>
        /// Gets or sets the ID of the staff member assigned to this schedule.
        /// </summary>
        /// <remarks>
        /// - Required for every schedule
        /// - Must reference an active, non-deleted staff member
        /// - Staff member must belong to the same restaurant (tenant consistency)
        /// - Used for overlap detection and schedule queries
        /// </remarks>
        [Required]
        public int StaffMemberId { get; set; }

        /// <summary>
        /// Navigation property to the assigned staff member.
        /// </summary>
        /// <remarks>
        /// - Enables EF Core relationship mapping
        /// - Loaded for tenant consistency validation
        /// - Used to check staff member active status and prevent overlapping shifts
        /// - May be null if not loaded (lazy loading scenario)
        /// </remarks>
        public StaffMember? StaffMember { get; set; }

        // Audit: who created / last modified
        // Only AdminUser can create/modify schedules in this streamlined model.
        // Staff-related audit properties are commented out below for clarity and maintainability.
        // To re-enable staff audit, uncomment and update business logic accordingly.

        /*
        // Purpose: Tracks which staff member created the schedule.
        // Best Practice: Needed if staff managers can create schedules and you want audit trails for staff actions.
        // Not needed if only admin users create schedules, or you don’t need to track staff creators.
        public int? CreatedByStaffId { get; set; }
        public StaffMember? CreatedBy { get; set; }

        // Purpose: Tracks which staff member last modified the schedule.
        // Best Practice: Needed if staff managers can edit schedules and you want to audit staff changes.
        // Not needed if only admin users modify schedules, or you don’t need to track staff modifications.
        public int? LastModifiedByStaffId { get; set; }
        public StaffMember? LastModifiedBy { get; set; }
        */

        [ForeignKey(nameof(CreatedByAdminUser))]
        public int? CreatedByAdminUserId { get; set; }
        public AdminUser? CreatedByAdminUser { get; set; }

        public DateTime? LastModified { get; set; }

        [ForeignKey(nameof(LastModifiedByAdminUser))]
        public int? LastModifiedByAdminUserId { get; set; }
        public AdminUser? LastModifiedByAdminUser { get; set; }

        // ===================================================================
        // HELPER METHODS
        // ===================================================================
        // Note: These methods do not perform database checks.
        // Navigation properties must be loaded, or checks performed at service layer.

        /// <summary>
        /// Determines whether the specified admin user can manage this schedule.
        /// </summary>
        /// <param name="admin">The admin user to check permissions for.</param>
        /// <returns>True if the admin can manage schedules for this restaurant; otherwise, false.</returns>
        /// <remarks>
        /// Delegates to AdminUser.CanManageStaffSchedules to maintain single source of truth.
        /// This ensures consistent authorization policy across the application.
        /// 
        /// Authorization Rules:
        /// - SystemAdmin: Can manage schedules for any restaurant
        /// - Owner: Can manage schedules only for owned restaurants
        /// - Other admin types: No schedule management permissions
        /// 
        /// To modify authorization policy, update AdminUser.CanManageStaffSchedules.
        /// </remarks>
        public bool CanBeManagedBy(AdminUser? admin)
        {
            if (admin == null) return false;
            
            // Delegate to AdminUser method to keep single source of truth for admin schedule management policy
            return admin.CanManageStaffSchedules(RestaurantId);
        }

        // ===================================================================
        // MULTI-TENANT VALIDATION
        // ===================================================================

        /// <summary>
        /// Validates that the schedule maintains multi-tenant boundaries and consistency.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when tenant consistency is violated.</exception>
        /// <remarks>
        /// This method should be called after navigation properties are loaded to ensure:
        /// - Restaurant navigation property matches RestaurantId
        /// - Staff member belongs to the same restaurant
        /// - Staff member is active and not deleted
        /// 
        /// Tenant Consistency Rules:
        /// 1. Schedule must belong to exactly one restaurant
        /// 2. Staff member must belong to the same restaurant
        /// 3. Staff member must be active and not deleted
        /// 4. Restaurant navigation (if loaded) must match RestaurantId
        /// 5. Admin users must have permission for the restaurant
        /// 
        /// This is a critical security and data integrity check for multi-tenant systems.
        /// </remarks>
        public void ValidateTenantConsistency()
        {
            // Validate Restaurant navigation property consistency
            if (Restaurant != null && Restaurant.Id != RestaurantId)
            {
                throw new InvalidOperationException(
                    $"Restaurant navigation property ID ({Restaurant.Id}) does not match RestaurantId ({RestaurantId}).");
            }

            // Validate Staff Member tenant consistency
            if (StaffMember != null)
            {
                if (StaffMember.Id != StaffMemberId)
                {
                    throw new InvalidOperationException(
                        $"StaffMember navigation property ID ({StaffMember.Id}) does not match StaffMemberId ({StaffMemberId}).");
                }

                if (StaffMember.RestaurantId != RestaurantId)
                {
                    throw new InvalidOperationException(
                        $"Staff schedule must belong to same restaurant as the Staff Member. " +
                        $"Schedule RestaurantId: {RestaurantId}, StaffMember RestaurantId: {StaffMember.RestaurantId}");
                }

                if (!StaffMember.IsActive || StaffMember.IsDeleted)
                {
                    throw new InvalidOperationException(
                        $"Cannot have schedule for inactive or deleted staff member (StaffMemberId: {StaffMemberId}).");
                }
            }

            // Validate Admin User tenant consistency (if loaded)
            if (CreatedByAdminUser != null)
            {
                if (CreatedByAdminUser.Id != CreatedByAdminUserId)
                {
                    throw new InvalidOperationException(
                        $"CreatedByAdminUser navigation property ID ({CreatedByAdminUser.Id}) does not match CreatedByAdminUserId ({CreatedByAdminUserId}).");
                }

                if (!CreatedByAdminUser.CanManageStaffSchedules(RestaurantId))
                {
                    throw new InvalidOperationException(
                        $"Admin user (ID: {CreatedByAdminUserId}) does not have permission to manage schedules for restaurant (ID: {RestaurantId}).");
                }
            }

            if (LastModifiedByAdminUser != null && LastModifiedByAdminUserId.HasValue)
            {
                if (LastModifiedByAdminUser.Id != LastModifiedByAdminUserId.Value)
                {
                    throw new InvalidOperationException(
                        $"LastModifiedByAdminUser navigation property ID ({LastModifiedByAdminUser.Id}) does not match LastModifiedByAdminUserId ({LastModifiedByAdminUserId}).");
                }

                if (!LastModifiedByAdminUser.CanManageStaffSchedules(RestaurantId))
                {
                    throw new InvalidOperationException(
                        $"Admin user (ID: {LastModifiedByAdminUserId}) does not have permission to manage schedules for restaurant (ID: {RestaurantId}).");
                }
            }
        }

        // ===================================================================
        // VALIDATION LOGIC (IValidatableObject)
        // ===================================================================
        // IValidatableObject is REQUIRED for Tier 3 - Simple Entity because:
        // - Tier 3 entities have public setters (no encapsulation)
        // - Without this, business rules cannot be enforced
        // - Data annotations alone cannot handle complex validation logic
        // - EF Core automatically calls Validate() on SaveChanges()

        /// <summary>
        /// Validates the staff schedule ensuring data consistency and business rules.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>Collection of validation results.</returns>
        /// <remarks>
        /// Validation Rules:
        /// 1. Tenant Boundary:
        ///    - Delegated to ValidateTenantConsistency() for consistency
        ///    - Must belong to exactly one restaurant
        ///    - Staff member must belong to same restaurant
        /// 2. Schedule Data:
        ///    - Valid shift times
        ///    - Valid recurring pattern
        /// 3. Staff Assignment:
        ///    - Valid staff member reference
        ///    - Staff member must be active
        /// 4. Audit Trail:
        ///    - Valid admin user references
        /// </remarks>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // ===================================================================
            // TENANT CONSISTENCY VALIDATION
            // ===================================================================
            // Delegate all tenant boundary checks to ValidateTenantConsistency()
            // to avoid redundancy and maintain single source of truth
            InvalidOperationException? tenantConsistencyException = null;
            try
            {
                ValidateTenantConsistency();
            }
            catch (InvalidOperationException ex)
            {
                tenantConsistencyException = ex;
            }
            if (tenantConsistencyException != null)
            {
                yield return new ValidationResult(
                    tenantConsistencyException.Message,
                    new[] { nameof(RestaurantId), nameof(StaffMemberId) }
                );
            }

            // ===================================================================
            // BUSINESS RULE VALIDATION
            // ===================================================================

            // Staff member validation (basic ID check)
            if (StaffMemberId <= 0)
            {
                yield return new ValidationResult(
                    "StaffMemberId must be a positive integer",
                    new[] { nameof(StaffMemberId) }
                );
            }

            // Shift time validation
            if (ShiftEnd <= ShiftStart)
            {
                yield return new ValidationResult(
                    "ShiftEnd must be after ShiftStart",
                    new[] { nameof(ShiftStart), nameof(ShiftEnd) }
                );
            }

            // Maximum shift duration
            var duration = ShiftEnd - ShiftStart;
            if (duration > TimeSpan.FromHours(24))
            {
                yield return new ValidationResult(
                    "Shift duration cannot exceed 24 hours",
                    new[] { nameof(ShiftStart), nameof(ShiftEnd) }
                );
            }

            // Minimum shift duration
            if (duration < TimeSpan.FromMinutes(30))
            {
                yield return new ValidationResult(
                    "Shift duration must be at least 30 minutes",
                    new[] { nameof(ShiftStart), nameof(ShiftEnd) }
                );
            }

            // Recurring schedule validation
            if (IsRecurring && !RecurringDay.HasValue)
            {
                yield return new ValidationResult(
                    "Recurring schedules must specify a RecurringDay",
                    new[] { nameof(RecurringDay) }
                );
            }

            // Prevent scheduling too far in the future
            var maxFutureDate = DateTime.UtcNow.AddMonths(6);
            if (ShiftStart > maxFutureDate)
            {
                yield return new ValidationResult(
                    "Cannot schedule shifts more than 6 months in advance",
                    new[] { nameof(ShiftStart) }
                );
            }

            // Created by admin validation
            if (!CreatedByAdminUserId.HasValue || CreatedByAdminUserId <= 0)
            {
                yield return new ValidationResult(
                    "CreatedByAdminUser must be provided",
                    new[] { nameof(CreatedByAdminUserId) }
                );
            }

            // Last modified validation
            if (LastModified.HasValue)
            {
                if (!LastModifiedByAdminUserId.HasValue || LastModifiedByAdminUserId <= 0)
                {
                    yield return new ValidationResult(
                        "LastModifiedByAdminUser must be provided when LastModified is set",
                        new[] { nameof(LastModifiedByAdminUserId), nameof(LastModified) }
                    );
                }

                if (LastModified.Value > DateTime.UtcNow)
                {
                    yield return new ValidationResult(
                        "LastModified cannot be in the future",
                        new[] { nameof(LastModified) }
                    );
                }
            }

            // Status validation for completed shifts
            if (Status == ScheduleStatus.Completed && ShiftEnd > DateTime.UtcNow)
            {
                yield return new ValidationResult(
                    "Cannot mark future shifts as completed",
                    new[] { nameof(Status) }
                );
            }

            // Validate overlapping shifts for the same staff member
            if (StaffMember?.Schedules != null)
            {
                var overlappingSchedule = StaffMember.Schedules
                    .Where(s => s.Id != Id) // Exclude current schedule
                    .Any(s => s.ShiftStart < ShiftEnd && s.ShiftEnd > ShiftStart);

                if (overlappingSchedule)
                {
                    yield return new ValidationResult(
                        "Staff member already has a schedule during this time period",
                        new[] { nameof(ShiftStart), nameof(ShiftEnd) }
                    );
                }
            }

            yield break;
        }
    }

    public enum ScheduleStatus
    {
        Pending,
        Approved,
        Completed,
        Cancelled,
        SickLeave,
        Vacation,
        NeedsCoverage
    }
}