using SmartMenuOptim.Domain.Entities.ProfileEntities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartMenuOptim.Domain.Entities.GlobalEntities
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
    /// 
    /// Analysis Summary:
    /// - The BusinessRule entity acts as an audit trail for business rule modifications, storing historical versions of rules.
    /// - Current active rule values are maintained as direct properties on the AdminUser entity (single source of truth).
    /// - Synchronization occurs via SynchronizeWithAdminUser() method, which maps RuleType enum values to corresponding AdminUser properties.
    /// - All required properties (SalesThreshold, SentimentThreshold, ReviewCountThreshold, WellSoldThreshold, RegularCustomerReviewCountThreshold, PremiumCustomerReviewCountThreshold) are already implemented in AdminUser.cs with proper validation ranges.
    /// - Data types match: integer thresholds are cast from double Value, SentimentThreshold remains double.
    /// - Validation ensures rule values conform to AdminUser property constraints before synchronization.
    /// - This design enables efficient querying of current rules while maintaining full change history.
    /// 
    /// Usage Example:
    /// ```csharp
    /// // Creating a new business rule
    /// var newRule = new BusinessRule
    /// {
    ///     Name = "New Sales Threshold",
    ///     Description = "Updated minimum sales threshold",
    ///     RuleType = BusinessRuleType.SalesThreshold,
    ///     Value = 50,
    ///     AdminUserId = adminId,
    ///     IsCurrentValue = true
    /// };
    /// 
    /// dbContext.BusinessRules.Add(newRule);
    /// await dbContext.SaveChangesAsync(); // This will automatically sync with AdminUser properties
    /// ```
    /// 
    /// When saved:
    /// 1. Any existing active rule of the same type will be automatically deactivated
    /// 2. The corresponding AdminUser property will be updated
    /// 3. Audit fields (CreatedAt, UpdatedAt) are automatically set
    /// </remarks>
    [Table("BusinessRules")]
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
        public AdminUser AdminUser { get; set; }

        /// <summary>
        /// Version number for this rule
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Version must be a positive number")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// Indicates if this is the current active value for this rule type.
        /// Only one rule per type per admin can be active at a time.
        /// </summary>
        [Required]
        public bool IsCurrentValue { get; set; }

        /// <summary>
        /// Optional notes about rule changes
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        // --------------------------------------------------------------

        /*
        1.	Synchronization Logic:
        •	SynchronizeWithAdminUser() method in BusinessRule to update the corresponding AdminUser property
        •	Automatic synchronization in SaveChangesAsync in the DbContext when a BusinessRule is added or updated
        •	Handling of multiple active rules by deactivating old rules when a new active rule is saved
        */

        /// <summary>
        /// Updates the corresponding property on the AdminUser based on this rule's type and value
        /// </summary>
        /// <returns>True if the property was updated, false if no matching property exists</returns>
        public bool SynchronizeWithAdminUser()
        {
            if (AdminUser == null) return false;

            switch (RuleType)
            {
                case BusinessRuleType.SalesThreshold:
                    AdminUser.SalesThreshold = (int)Value;
                    break;
                case BusinessRuleType.SentimentThreshold:
                    AdminUser.SentimentThreshold = Value;
                    break;
                case BusinessRuleType.ReviewCountThreshold:
                    AdminUser.ReviewCountThreshold = (int)Value;
                    break;
                case BusinessRuleType.WellSoldThreshold:
                    AdminUser.WellSoldThreshold = (int)Value;
                    break;
                case BusinessRuleType.RegularCustomerReviewCountThreshold:
                    AdminUser.RegularCustomerReviewCountThreshold = (int)Value;
                    break;
                case BusinessRuleType.PremiumCustomerReviewCountThreshold:
                    AdminUser.PremiumCustomerReviewCountThreshold = (int)Value;
                    break;
                default:
                    return false;
            }

            return true;
        }

        /// --------------------------------------------------------------

        /*
         2.	Data Consistency Validation:
            •	ValidateValueType() method in BusinessRule to ensure values match the expected type and range
            •	Value range validation based on rule type (e.g., SentimentThreshold between 0 and 1)
            •	Integer validation for threshold values
            •	Prevention of duplicate active rules for the same type and admin 
         
         */


        /// <summary>
        /// Validates that the rule value is compatible with the corresponding AdminUser property type.
        /// Ensures data integrity when synchronizing values.
        /// </summary>
        public IEnumerable<ValidationResult> ValidateValueType(ValidationContext validationContext)
        {
            switch (RuleType)
            {
                case BusinessRuleType.SentimentThreshold:
                    if (Value < 0 || Value > 1)
                    {
                        yield return new ValidationResult(
                            "Sentiment threshold must be between 0 and 1",
                            new[] { nameof(Value) });
                    }
                    break;

                case BusinessRuleType.SalesThreshold:
                case BusinessRuleType.ReviewCountThreshold:
                case BusinessRuleType.WellSoldThreshold:
                    if (Value % 1 != 0 || Value < 1 || Value > 1000)
                    {
                        yield return new ValidationResult(
                            $"{RuleType} must be a whole number between 1 and 1000",
                            new[] { nameof(Value) });
                    }
                    break;

                case BusinessRuleType.RegularCustomerReviewCountThreshold:
                case BusinessRuleType.PremiumCustomerReviewCountThreshold:
                    if (Value % 1 != 0 || Value < 1 || Value > 100)
                    {
                        yield return new ValidationResult(
                            $"{RuleType} must be a whole number between 1 and 100",
                            new[] { nameof(Value) });
                    }
                    break;
            }
        }
    }
}
