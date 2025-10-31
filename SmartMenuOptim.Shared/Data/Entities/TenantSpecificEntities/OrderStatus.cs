using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;

/// <summary>
/// Represents the status of an order in the restaurant system.
/// </summary>
/// <remarks>
/// - Validation attributes ensure Name length and ColorCode format.
/// - Indexes are centralized in `AppDbContext.OnModelCreating` so avoid adding index attributes here.
/// </remarks>
[Table("OrderStatuses")]
public class OrderStatus : TenantEntityBase, IValidatableObject
{
    /// <summary>
    /// The name/title of the order status (e.g., "Pending", "Preparing", "Ready", etc.).
    /// </summary>
    [Required(ErrorMessage = "OrderStatus name is required")]
    [MaxLength(50, ErrorMessage = "OrderStatus name cannot exceed 50 characters")]
    [MinLength(1, ErrorMessage = "OrderStatus name must contain at least 1 character")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description providing more details about what this status means.
    /// </summary>
    [MaxLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// The display order for showing statuses in UI elements. Lower numbers appear first.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "DisplayOrder must be a non-negative integer")]
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Indicates if this is a terminal status (e.g., "Completed", "Cancelled") that shouldn't transition to other statuses.
    /// </summary>
    [Required]
    public bool IsTerminal { get; set; }

    /// <summary>
    /// Color code for UI representation (e.g., "#FF0000" for red).
    /// Stored as a 7-character string including the leading '#'.

    /// <summary>
    /// Navigation property for orders with this status.
    /// </summary>
    [InverseProperty(nameof(Order.Status))]
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    
    /// <summary>
    /// Gets or sets the hexadecimal color code in the format '#RRGGBB'.
    /// </summary>
    /// <remarks>The color code must begin with a '#' character followed by six hexadecimal digits (0-9, A-F).
    /// The value is case-insensitive and must not exceed seven characters. Example: '#FFA500' for orange.</remarks>
    [MaxLength(7, ErrorMessage = "ColorCode cannot exceed 7 characters (e.g. '#FFA500')")]
    [RegularExpression("^#([0-9a-fA-F]{6})$", ErrorMessage = "ColorCode must be a valid hex color in the format '#RRGGBB'.")]
    public string? ColorCode { get; set; }

    // === Validation ===
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("OrderStatus name must not be empty or whitespace.", new[] { nameof(Name) });

        // ColorCode is validated via RegularExpression attribute above; keep additional defensive check
        if (!string.IsNullOrEmpty(ColorCode))
        {
            var regex = new Regex("^#([0-9a-fA-F]{6})$");
            if (!regex.IsMatch(ColorCode))
                yield return new ValidationResult("ColorCode must be a valid hex color in the format '#RRGGBB'.", new[] { nameof(ColorCode) });
        }

        yield break;
    }
}