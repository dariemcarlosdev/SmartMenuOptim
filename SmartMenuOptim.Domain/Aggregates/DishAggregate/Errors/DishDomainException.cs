using SmartMenuOptim.Domain.Exceptions;
namespace SmartMenuOptim.Domain.Aggregates.DishAggregate.Errors;

/// <summary>
/// Exception thrown when a dish-related business rule is violated.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Represents business rule violations specific to the Dish aggregate root,
/// such as tenant consistency failures across menu assignments, reviews, sales records,
/// or order items, and category-tenant boundary violations.</para>
///
/// <para><strong>Dish Aggregate Invariants:</strong></para>
/// <list type="bullet">
///   <item><description>All menu assignments (MenuDishes) must belong to the same restaurant as the dish</description></item>
///   <item><description>Category must belong to the same restaurant as the dish</description></item>
///   <item><description>All reviews must belong to the same restaurant as the dish</description></item>
///   <item><description>All sale records must belong to the same restaurant as the dish</description></item>
///   <item><description>All order items must belong to the same restaurant as the dish</description></item>
///   <item><description>Restaurant navigation property must match RestaurantId</description></item>
/// </list>
///
/// <para><strong>Example:</strong></para>
/// <code>
/// throw new DishDomainException("Dish category must belong to the same restaurant.");
/// throw new DishDomainException("Dish contains menu assignments from different restaurants.", dishId);
/// throw new DishDomainException("Dish contains reviews from different restaurants.", dishId);
/// </code>
/// </remarks>
public class DishDomainException : DomainException
{
    /// <summary>
    /// Gets the dish identifier associated with this exception, if available.
    /// </summary>
    public int? DishId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DishDomainException"/> class.
    /// </summary>
    /// <param name="message">A message describing the dish business rule violation.</param>
    public DishDomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DishDomainException"/> class with a dish identifier.
    /// </summary>
    /// <param name="message">A message describing the dish business rule violation.</param>
    /// <param name="dishId">The identifier of the dish involved in the violation.</param>
    public DishDomainException(string message, int dishId)
        : base(message)
    {
        DishId = dishId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DishDomainException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">A message describing the dish business rule violation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DishDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
