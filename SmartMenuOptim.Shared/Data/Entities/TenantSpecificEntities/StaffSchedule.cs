using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a staff member's work schedule for a specific restaurant.
    /// Minimal tenant-scoped schedule entity with only necessary properties.
    /// Updated to allow Restaurant Admins (SystemAdmin or Owner) or Staff Managers to create/modify schedules.
    ///
    /// Authorization notes:
    /// - Admin users allowed to manage schedules: `AdminRole.SystemAdmin` and `AdminRole.Owner` only.
    /// - Staff members allowed to manage schedules: staff with `StaffRole.Manager` in the same `RestaurantId`.
    /// - To allow additional roles to manage schedules, update `CanBeManagedBy(AdminUser?)` and
    ///   optionally `AdminUser.CanManageStaffSchedules(...)` in `AdminUser.cs`.
    /// </summary>
    [Table("StaffSchedules")]
    public class StaffSchedule : TenantEntityBase, IValidatableObject
    {
        // Shift timing
        [Required]
        public DateTime ShiftStart { get; set; }

        [Required]
        public DateTime ShiftEnd { get; set; }

        // Recurrence (optional)
        public bool IsRecurring { get; set; }
        public DayOfWeek? RecurringDay { get; set; }

        // Status and notes
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Pending;

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Assignment
        [Required]
        public int StaffMemberId { get; set; }

        // Navigation (optional)
        public StaffMember? StaffMember { get; set; }

        // Audit: who created / last modified
        // Creators can be either a StaffMember (e.g., a staff manager) or an AdminUser (restaurant owner/manager)
        public int? CreatedByStaffId { get; set; }
        public StaffMember? CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedByAdminUser))]
        public int? CreatedByAdminUserId { get; set; }
        public AdminUser? CreatedByAdminUser { get; set; }

        public DateTime? LastModified { get; set; }

        public int? LastModifiedByStaffId { get; set; }
        public StaffMember? LastModifiedBy { get; set; }

        [ForeignKey(nameof(LastModifiedByAdminUser))]
        public int? LastModifiedByAdminUserId { get; set; }
        public AdminUser? LastModifiedByAdminUser { get; set; }

        // Helper methods: these do not perform DB checks. For AdminUser ownership checks, the caller
        // should ensure `AdminUser.OwnedRestaurants` is loaded or pass a predicate from service layer.

        /// <summary>
        /// Returns true if given admin user is allowed to create/modify schedules for this restaurant.
        /// Current policy: only SystemAdmin and Owner roles are allowed.
        /// - SystemAdmin: global rights
        /// - Owner: allowed for restaurants they own (if OwnedRestaurants is loaded this is enforced),
        ///   otherwise Owner role is allowed (caller should load OwnedRestaurants for strict enforcement).
        /// </summary>
        public bool CanBeManagedBy(AdminUser? admin)
        {
            if (admin == null) return false;

            // SystemAdmin has global rights
            if (admin.Role == AdminRole.SystemAdmin) return true;

            // Only Owners (not Managers) are allowed in the admin role scope per current policy
            if (admin.Role != AdminRole.Owner) return false;

            // If OwnedRestaurants navigation is populated, enforce ownership strictly
            if (admin.OwnedRestaurants != null && admin.OwnedRestaurants.Any())
                return admin.OwnedRestaurants.Any(r => r.Id == RestaurantId);

            // Fallback: allow based on Owner role only (service layer should enforce tenant scoping when possible)
            return true;
        }

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

            // Audit validation: at least one creator identity must be present (staff or admin)
            if (!((CreatedByStaffId.HasValue && CreatedByStaffId > 0) || (CreatedByAdminUserId.HasValue && CreatedByAdminUserId > 0)))
                yield return new ValidationResult("CreatedBy (either a StaffMember or an AdminUser) must be provided.", new[] { nameof(CreatedByStaffId), nameof(CreatedByAdminUserId) });

            // If last modified timestamp is set, require a last modifier identity
            if (LastModified.HasValue && !((LastModifiedByStaffId.HasValue && LastModifiedByStaffId > 0) || (LastModifiedByAdminUserId.HasValue && LastModifiedByAdminUserId > 0)))
                yield return new ValidationResult("LastModifiedBy (either a StaffMember or an AdminUser) must be provided when LastModified is set.", new[] { nameof(LastModifiedByStaffId), nameof(LastModifiedByAdminUserId), nameof(LastModified) });

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