using SmartMenuOptim.Domain.Exceptions;
namespace SmartMenuOptim.Domain.Aggregates.MenuAggregate.Errors;

/// <summary>
/// Exception thrown when a menu-related business rule is violated.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Represents business rule violations specific to the Menu aggregate, such as adding
/// dishes from a different restaurant, making a menu available without dishes, duplicate
/// dish entries, or invalid availability time windows.</para>
///
/// <para><strong>Menu Aggregate Invariants:</strong></para>
/// <list type="bullet">
///   <item><description>All dishes on a menu must belong to the same restaurant as the menu</description></item>
///   <item><description>A menu cannot be made available without at least one active dish</description></item>
///   <item><description>Each dish can only appear once per menu (no duplicates)</description></item>
///   <item><description>Availability time windows must be valid (AvailableFrom &lt; AvailableTo, or both null)</description></item>
///   <item><description>Special pricing must be reasonable (not exceeding 5x base price)</description></item>
///   <item><description>Only active, non-deleted dishes can be added to a menu</description></item>
/// </list>
///
/// <para><strong>Example:</strong></para>
/// <code>
/// throw new MenuDomainException("Cannot add dish from different restaurant.");
/// throw new MenuDomainException($"Dish '{dishName}' is already on this menu.", menuId);
/// throw new MenuDomainException("Cannot make menu available without active dishes.");
/// </code>
/// </remarks>
public class MenuDomainException : DomainException
{
    /// <summary>
    /// Gets the menu identifier associated with this exception, if available.
    /// </summary>
    public int? MenuId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuDomainException"/> class.
    /// </summary>
    /// <param name="message">A message describing the menu business rule violation.</param>
    public MenuDomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuDomainException"/> class with a menu identifier.
    /// </summary>
    /// <param name="message">A message describing the menu business rule violation.</param>
    /// <param name="menuId">The identifier of the menu involved in the violation.</param>
    public MenuDomainException(string message, int menuId)
        : base(message)
    {
        MenuId = menuId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuDomainException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">A message describing the menu business rule violation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MenuDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
