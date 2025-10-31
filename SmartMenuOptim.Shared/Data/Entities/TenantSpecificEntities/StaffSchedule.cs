using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a staff member's work schedule for a specific restaurant.
    /// Minimal tenant-scoped schedule entity with only necessary properties.
    /// Updated to allow Restaurant Admins (SystemAdmin or Owner) to create/modify schedules.
    ///
    /// Streamlined model: Only admin users manage schedules. Staff-related audit properties are commented out below.
    /// To re-enable staff audit, uncomment and update business logic accordingly.
    /// </summary>
    [Table("StaffSchedules")]
    public class StaffSchedule : TenantEntityBase, IValidatableObject
    {
        // Shift timing
        /// <summary>
        /// Gets or sets the scheduled start time of the shift.
        /// </summary>
        [Required]
        public DateTime ShiftStart { get; set; }

        /// <summary>
        /// Gets or sets the scheduled end time of the shift.
        /// </summary>
        [Required]
        public DateTime ShiftEnd { get; set; }

        // Recurrence (optional)
        /// <summary>
        /// Gets or sets a value indicating whether the event is configured to repeat on a regular schedule.
        /// </summary>
        public bool IsRecurring { get; set; }

        // Day of week for recurring shifts (if IsRecurring is true)
        /// <summary>
        /// Gets or sets the day of the week on which the recurring shift occurs.
        /// A recurring shift repeats weekly on the specified day.
        /// </summary>
        /// <remarks>This property is relevant only when the shift is configured as recurring. If <see
        /// langword="null"/>, no specific recurring day is set.</remarks>
        public DayOfWeek? RecurringDay { get; set; }

        // Status and notes

        /// <summary>
        /// Gets or sets the current status of the schedule.
        /// Status indicates whether the shift is pending, approved, completed, etc.
        /// </summary>
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Pending;

        /// <summary>
        /// Gets or sets optional notes or comments associated with the entity.
        /// It can hold additional information about the schedule.
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        // Assignment
        /// <summary>
        /// The ID of the staff member this schedule is assigned to.
        /// This property should NOT be commented out, even in a streamlined model,
        /// because every schedule must be linked to a staff member.
        /// It is essential for associating shifts with employees and for schedule management.
        /// </summary>
        [Required]
        public int StaffMemberId { get; set; }

        /// <summary>
        /// Navigation property to the assigned staff member.
        /// This property should NOT be commented out.
        /// It enables EF Core relationship mapping and allows querying schedules by staff member.
        /// Removing it would break schedule-to-employee association and related queries.
        /// </summary>
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

        // Helper methods: these do not perform DB checks. For AdminUser ownership checks, the caller
        // should ensure `AdminUser.OwnedRestaurants` is loaded or pass a predicate from service layer.
        
        /// <summary>
        /// Returns true if given admin user is allowed to create/modify schedules for this restaurant.
        /// Delegates to AdminUser.CanManageStaffSchedules to keep a single canonical implementation.
        /// This ensures all admin schedule management policy is maintained in one place (AdminUser).
        /// If you need to change business rules, update AdminUser.CanManageStaffSchedules only.
        /// </summary>
        public bool CanBeManagedBy(AdminUser? admin)
        {
            if (admin == null) return false;
            // Delegate to AdminUser method to keep single source of truth for admin schedule management policy
            return admin.CanManageStaffSchedules(RestaurantId);
        }

        /*
        /// <summary>
        /// Returns true if given staff member may manage this schedule (must be assigned to same restaurant and be a staff manager role).
        /// This is used to authorize staff members managing schedules. It is a overload of CanBeManagedBy for AdminUser.
        /// It is not necessary to be used since just AdminUser can manage schedules, but provided for symmetry and potential future use cases.
        /// </summary>
        public bool CanBeManagedBy(StaffMember? staff)
        {
            if (staff == null) return false;
            if (staff.RestaurantId != RestaurantId) return false;

            // Only staff with managerial role should be allowed to manage schedules
            return staff.Role == StaffRole.Manager;
        }
        */

        // Validation logic

        // Simple validation to ensure sensible shift times and audit presence
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ShiftEnd <= ShiftStart)
                yield return new ValidationResult("ShiftEnd must be after ShiftStart.", new[] { nameof(ShiftStart), nameof(ShiftEnd) });

            var duration = ShiftEnd - ShiftStart;
            if (duration > TimeSpan.FromHours(24))
                yield return new ValidationResult("Shift duration cannot exceed 24 hours.", new[] { nameof(ShiftStart), nameof(ShiftEnd) });

            if (IsRecurring && !RecurringDay.HasValue)
                yield return new ValidationResult("Recurring schedules must specify a RecurringDay.", new[] { nameof(RecurringDay) });

            if (StaffMemberId <= 0)
                yield return new ValidationResult("StaffMemberId must be a positive integer.", new[] { nameof(StaffMemberId) });

            // Audit validation: at least one creator identity must be present (admin only in streamlined model)
            if (!(CreatedByAdminUserId.HasValue && CreatedByAdminUserId > 0))
                yield return new ValidationResult("CreatedByAdminUser must be provided.", new[] { nameof(CreatedByAdminUserId) });

            // If last modified timestamp is set, require a last modifier identity (admin only in streamlined model)
            if (LastModified.HasValue && !(LastModifiedByAdminUserId.HasValue && LastModifiedByAdminUserId > 0))
                yield return new ValidationResult("LastModifiedByAdminUser must be provided when LastModified is set.", new[] { nameof(LastModifiedByAdminUserId), nameof(LastModified) });

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