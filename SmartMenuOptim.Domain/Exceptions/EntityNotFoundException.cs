namespace SmartMenuOptim.Domain.Exceptions;

/// <summary>
/// Exception thrown when a domain entity cannot be found.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Represents a domain-level "not found" condition, indicating that a requested aggregate
/// or entity does not exist within the expected bounded context. This is distinct from
/// <see cref="KeyNotFoundException"/> in that it carries domain-specific context such as the
/// entity name and identifier.</para>
///
/// <para><strong>HTTP Mapping:</strong></para>
/// <para>The Infrastructure layer maps this exception to HTTP 404 (Not Found) responses.</para>
///
/// <para><strong>Example:</strong></para>
/// <code>
/// throw new EntityNotFoundException("Order", orderId);
/// // Message: "Entity 'Order' with identifier '42' was not found."
///
/// throw new EntityNotFoundException("Dish", dishId, "The dish may have been removed from the menu.");
/// // Message: "Entity 'Dish' with identifier '7' was not found. The dish may have been removed from the menu."
/// </code>
/// </remarks>
public class EntityNotFoundException : DomainException
{
    /// <summary>
    /// Gets the name of the entity that was not found.
    /// </summary>
    public string EntityName { get; }

    /// <summary>
    /// Gets the identifier of the entity that was not found.
    /// </summary>
    public object EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="entityName">The name of the entity that was not found.</param>
    /// <param name="entityId">The identifier of the entity that was not found.</param>
    public EntityNotFoundException(string entityName, object entityId)
        : base($"Entity '{entityName}' with identifier '{entityId}' was not found.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with additional details.
    /// </summary>
    /// <param name="entityName">The name of the entity that was not found.</param>
    /// <param name="entityId">The identifier of the entity that was not found.</param>
    /// <param name="additionalDetails">Additional context about why the entity was not found.</param>
    public EntityNotFoundException(string entityName, object entityId, string additionalDetails)
        : base($"Entity '{entityName}' with identifier '{entityId}' was not found. {additionalDetails}")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}
