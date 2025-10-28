using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;
using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a staff member's work schedule for a specific restaurant.
    /// </summary>
    /// <remarks>
    /// Multi-Tenant Support: This entity is tenant-specific. Each StaffSchedule is linked to a Restaurant,
    /// enabling the application to support multiple restaurants (tenants), each managing their own staff schedules.
    /// </remarks>
    public class StaffSchedule : TenantEntityBase
    {
        // === Schedule Properties ===

        /// <summary>
        /// Start date and time of the shift (UTC).
        /// </summary>
        [Required]
        public DateTime ShiftStart { get; set; }

        /// <summary>
        /// End date and time of the shift (UTC).
        /// </summary>
        [Required]
        public DateTime ShiftEnd { get; set; }

        /// <summary>
        /// Indicates if this is a recurring schedule.
        /// </summary>
        public bool IsRecurring { get; set; }

        /// <summary>
        /// Day of week for recurring schedules.
        /// </summary>
        public DayOfWeek? RecurringDay { get; set; }

        /// <summary>
        /// Status of the schedule (e.g., Pending, Approved, Completed).
        /// </summary>
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Pending;

        /// <summary>
        /// Notes about the schedule (e.g., special instructions, coverage details).
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        // === Staff Assignment ===

        /// <summary>
        /// Foreign key to the StaffMember entity.
        /// </summary>
        public int StaffMemberId { get; set; }

        /// <summary>
        /// Navigation property to the assigned staff member.
        /// </summary>
        public StaffMember? StaffMember { get; set; }

        // === Schedule Management ===

        /// <summary>
        /// The staff member who created/assigned this schedule.
        /// </summary>
        public int CreatedByStaffId { get; set; }

        /// <summary>
        /// Navigation property to the staff member who created the schedule.
        /// </summary>
        public StaffMember? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when this schedule was last modified (UTC).
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// ID of the staff member who last modified this schedule.
        /// </summary>
        public int? LastModifiedByStaffId { get; set; }

        /// <summary>
        /// Navigation property to the staff member who last modified the schedule.
        /// </summary>
        public StaffMember? LastModifiedBy { get; set; }

        // === Validation Methods ===

        /// <summary>
        /// Validates that the shift end time is after the start time.
        /// </summary>
        public bool IsValidShiftTime()
        {
            return ShiftEnd > ShiftStart;
        }

        /// <summary>
        /// Checks if this schedule overlaps with another schedule.
        /// </summary>
        public bool OverlapsWith(StaffSchedule other)
        {
            return StaffMemberId == other.StaffMemberId &&
                   ShiftStart < other.ShiftEnd &&
                   ShiftEnd > other.ShiftStart;
        }
    }

    /// <summary>
    /// Represents the status of a staff schedule.
    /// </summary>
    public enum ScheduleStatus
    {
        /// <summary>
        /// Schedule is waiting for approval.
        /// </summary>
        Pending,

        /// <summary>
        /// Schedule has been approved.
        /// </summary>
        Approved,

        /// <summary>
        /// Schedule has been completed.
        /// </summary>
        Completed,

        /// <summary>
        /// Schedule was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Staff member called in sick.
        /// </summary>
        SickLeave,

        /// <summary>
        /// Staff member is on vacation.
        /// </summary>
        Vacation,

        /// <summary>
        /// Schedule needs coverage.
        /// </summary>
        NeedsCoverage
    }
}