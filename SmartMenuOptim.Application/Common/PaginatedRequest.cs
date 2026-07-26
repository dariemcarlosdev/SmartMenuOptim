using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Common;

/// <summary>
/// Shared request model for paginated, sortable, and filterable collection endpoints.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Provides a consistent pagination contract across all API collection endpoints.
/// Controllers accept this model (or a subclass) via <c>[FromQuery]</c> binding,
/// and services use it to apply server-side pagination, sorting, and filtering.</para>
///
/// <para><strong>Conventions (per §5.4.6):</strong></para>
/// <list type="bullet">
///   <item><description>Pages are 1-based (first page is 1, not 0)</description></item>
///   <item><description>PageSize ceiling of 100 enforced server-side to prevent full-table scans</description></item>
///   <item><description>SortBy is validated against an allowlist per endpoint — never passed raw to OrderBy</description></item>
///   <item><description>SortDirection accepts "asc" or "desc" (case-insensitive)</description></item>
/// </list>
///
/// <para><strong>Usage — API Controller:</strong></para>
/// <code>
/// [HttpGet]
/// public async Task&lt;ActionResult&lt;PaginatedResponseDto&lt;OrderDTO&gt;&gt;&gt; GetPaginated(
///     [FromQuery] PaginatedRequest request, CancellationToken ct)
/// </code>
/// </remarks>
public class PaginatedRequest
{
    /// <summary>
    /// The current page number (1-based). Defaults to 1.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// The number of items per page. Defaults to 20. Maximum is 100.
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// The field to sort by. Validated against an endpoint-specific allowlist.
    /// Defaults to "createdAt".
    /// </summary>
    [StringLength(50)]
    public string SortBy { get; set; } = "createdAt";

    /// <summary>
    /// Sort direction: "asc" or "desc". Defaults to "desc".
    /// </summary>
    [RegularExpression("^(asc|desc)$", ErrorMessage = "SortDirection must be 'asc' or 'desc'.")]
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Gets whether the sort direction is descending.
    /// </summary>
    public bool IsDescending => SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
}
