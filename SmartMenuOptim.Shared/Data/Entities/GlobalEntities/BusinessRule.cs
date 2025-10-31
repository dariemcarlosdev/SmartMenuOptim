using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartMenuOptim.Shared.Data.Entities.GlobalEntities
{
    /// <summary>
    /// Defines the types of business rules that can be tracked.
    /// Maps directly to properties on AdminUser.
    /// </summary>
    /// <remarks>
    /// Using an enum for RuleType provides several benefits:
    /// 1. Type Safety: Can't accidentally use an invalid rule type
    /// 2. IntelliSense Support: IDE shows available rule types
    /// 3. Clear Mapping: Direct correspondence to AdminUser properties
    /// 4. Maintainability: Changes to rule types must be explicitly added to enum
    /// 5. Documentation: Each enum value can be documented with XML comments
    /// 6. Performance: Enums are more efficient than strings
    /// 7. Validation: Easier to validate rule types at compile time
    /// 
    /// This design makes it clear which business rules can be tracked and ensures they correspond 
    /// directly to the properties in AdminUser. It also makes it easier to:
    /// - Query historical rules by type
    /// - Validate rule changes
    /// - Generate UI components for rule management
    /// - Maintain consistency in rule types across the application
    /// 
    /// The enum values exactly match the property names in AdminUser, making it easy to 
    /// programmatically update the corresponding properties when rules change.
    /// </remarks>
    public enum BusinessRuleType
    {
        /// <summary>
        /// Minimum number of sales for a dish to be considered popular
        /// </summary>
        SalesThreshold = 0,

        /// <summary>
        /// Minimum sentiment score for a review to be considered positive
        /// </summary>
        SentimentThreshold = 1,

        /// <summary>
        /// Minimum number of reviews required for a dish to be considered well-reviewed
        /// </summary>
        ReviewCountThreshold = 2,

        /// <summary>
        /// Minimum number of sales required for a dish to be considered well-sold
        /// </summary>
        WellSoldThreshold = 3,

        /// <summary>
        /// Minimum number of reviews for regular customer status
        /// </summary>
        RegularCustomerReviewCountThreshold = 4,

        /// <summary>
        /// Minimum number of reviews for premium customer status
        /// </summary>
        PremiumCustomerReviewCountThreshold = 5
    }

    /// <summary>
    /// Represents a business rule managed by an admin user (e.g., thresholds, analytics settings).
    /// </summary>
    /// <remarks>
    /// While most business rules are now properties on AdminUser, this entity is maintained for historical tracking
    /// and audit purposes of rule changes over time.
    /// </remarks>
    [Table("BusinessRules")]
    /// <summary>
    /// Composite index on (RuleType, AdminUserId, CreatedAt):
    /// - Ensures one active rule per type per admin (unique constraint enforced separately)
    /// - Optimizes historical queries for rule changes over time (filter by rule type, admin, and date)
    /// - Useful for audit/reporting queries that need the latest or historical values
    /// </summary>
    [Index(nameof(RuleType), nameof(AdminUserId), nameof(CreatedAt), Name = "IX_BusinessRules_RuleType_AdminUser_Date")]
    public class BusinessRule : EntityBase
    {
        /// <summary>
        /// Name of the business rule
        /// </summary>
        [Required(ErrorMessage = "Rule name is required")]
        [MaxLength(100, ErrorMessage = "Rule name cannot exceed 100 characters")]
        [MinLength(3, ErrorMessage = "Rule name must be at least 3 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the rule's purpose and impact
        /// </summary>
        [Required(ErrorMessage = "Rule description is required")]
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The numerical value/threshold for this rule
        /// </summary>
        [Required(ErrorMessage = "Rule value is required")]
        [Range(0.0, double.MaxValue, ErrorMessage = "Value must be a positive number")]
        [Column(TypeName = "decimal(18,2)")]
        public double Value { get; set; }
        
        /// <summary>
        /// The specific type of business rule this record represents
        /// </summary>
        [Required(ErrorMessage = "Rule type is required")]
        [EnumDataType(typeof(BusinessRuleType))]
        public BusinessRuleType RuleType { get; set; }

        
        /// <summary>
        /// Foreign key to the AdminUser who created/manages this business rule
        /// </summary>
        [Required(ErrorMessage = "Admin user reference is required")]
        [ForeignKey(nameof(AdminUser))]
        public int AdminUserId { get; set; }
        
        /// <summary>
        /// Navigation property to the AdminUser who created/manages this business rule
        /// </summary>
        [Required]
        public required AdminUser AdminUser { get; set; }

        /// <summary>
        /// Version number for this rule
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Version must be a positive number")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// Optional notes about rule changes
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

    }
}
