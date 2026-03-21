namespace SmartMenuOptim.Application.Features.Customers.DTOs;

/// <summary>
/// Lightweight DTO for customer dropdown/lookup scenarios.
/// </summary>
/// <remarks>
/// Contains only Id and Name to minimize payload for select lists.
/// </remarks>
public class CustomerLookupDTO
{
    /// <summary>
    /// Customer identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Customer display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
