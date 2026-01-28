using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Common
{
    /// <summary>
    /// Represents a paginated response containing data and pagination metadata for API responses.
    /// </summary>
    /// <typeparam name="T">The type of data in the response (e.g., DishDto, OrderDto, MenuDto).</typeparam>
    /// <remarks>
    /// <para><strong>Purpose:</strong></para>
    /// <para>This generic DTO encapsulates paginated data along with metadata required for implementing
    /// pagination in client applications (Blazor, Web APIs, mobile apps). It provides a consistent
    /// structure for all paginated API endpoints in the SmartMenuOptimizer system.</para>
    /// 
    /// <para><strong>Design Pattern:</strong></para>
    /// <para>Follows the standard pagination response pattern used in RESTful APIs, providing both
    /// the data payload and pagination context in a single response object. This eliminates the need
    /// for separate pagination metadata headers and simplifies client-side pagination logic.</para>
    /// 
    /// <para><strong>Usage Context:</strong></para>
    /// <list type="bullet">
    ///   <item><description>API controller responses for paginated lists (dishes, menus, orders, etc.)</description></item>
    ///   <item><description>Blazor component data binding for paginated grids/tables</description></item>
    ///   <item><description>Service layer responses when querying large datasets</description></item>
    ///   <item><description>Repository pattern responses for efficient data loading</description></item>
    /// </list>
    /// 
    /// <para><strong>Helper Properties:</strong></para>
    /// <para>Includes computed properties (HasNextPage, HasPreviousPage, StartIndex, EndIndex) that
    /// simplify pagination UI logic in Blazor components and reduce client-side calculations.</para>
    /// 
    /// <para><strong>Example Usage - API Controller:</strong></para>
    /// <code>
    /// [HttpGet]
    /// public async Task&lt;ActionResult&lt;PaginatedResponseDto&lt;DishDto&gt;&gt;&gt; GetDishes(
    ///     [FromQuery] int page = 1,
    ///     [FromQuery] int pageSize = 10)
    /// {
    ///     // Validate parameters
    ///     if (page &lt; 1) page = 1;
    ///     if (pageSize &lt; 1 || pageSize &gt; 100) pageSize = 10;
    ///     
    ///     // Get data with pagination
    ///     var dishes = await _dishService.GetPaginatedDishesAsync(page, pageSize);
    ///     var totalCount = await _dishService.GetTotalCountAsync();
    ///     
    ///     // Use factory method (auto-calculates TotalPages)
    ///     var response = PaginatedResponseDto&lt;DishDto&gt;.Create(
    ///         data: dishes.ToList(),
    ///         totalCount: totalCount,
    ///         page: page,
    ///         pageSize: pageSize
    ///     );
    ///     
    ///     return Ok(response);
    /// }
    /// </code>
    /// 
    /// <para><strong>Example Usage - Blazor Component:</strong></para>
    /// <code><![CDATA[
    /// @page "/dishes"
    /// @inject HttpClient Http
    /// 
    /// <h3>Dishes (@dishes?.TotalCount ?? 0)</h3>
    /// 
    /// @if (dishes?.Data?.Any() == true)
    /// {
    ///     <table class="table">
    ///         <thead>
    ///             <tr>
    ///                 <th>Name</th>
    ///                 <th>Price</th>
    ///                 <th>Category</th>
    ///             </tr>
    ///         </thead>
    ///         <tbody>
    ///             @foreach (var dish in dishes.Data)
    ///             {
    ///                 <tr>
    ///                     <td>@dish.Name</td>
    ///                     <td>@dish.Price.ToString("C")</td>
    ///                     <td>@dish.CategoryName</td>
    ///                 </tr>
    ///             }
    ///         </tbody>
    ///     </table>
    ///     
    ///     <nav aria-label="Page navigation">
    ///         <ul class="pagination justify-content-center">
    ///             <li class="page-item @(dishes.IsFirstPage ? "disabled" : "")">
    ///                 <button class="page-link" @onclick="FirstPage" disabled="@dishes.IsFirstPage">
    ///                     First
    ///                 </button>
    ///             </li>
    ///             
    ///             <li class="page-item @(!dishes.HasPreviousPage ? "disabled" : "")">
    ///                 <button class="page-link" @onclick="PreviousPage" disabled="@!dishes.HasPreviousPage">
    ///                     Previous
    ///                 </button>
    ///             </li>
    ///             
    ///             <li class="page-item active">
    ///                 <span class="page-link">
    ///                     Page @dishes.Page of @dishes.TotalPages
    ///                 </span>
    ///             </li>
    ///             
    ///             <li class="page-item @(!dishes.HasNextPage ? "disabled" : "")">
    ///                 <button class="page-link" @onclick="NextPage" disabled="@!dishes.HasNextPage">
    ///                     Next
    ///                 </button>
    ///             </li>
    ///             
    ///             <li class="page-item @(dishes.IsLastPage ? "disabled" : "")">
    ///                 <button class="page-link" @onclick="LastPage" disabled="@dishes.IsLastPage">
    ///                     Last
    ///                 </button>
    ///             </li>
    ///         </ul>
    ///     </nav>
    ///     
    ///     <p class="text-muted text-center">
    ///         Showing @dishes.StartIndex - @dishes.EndIndex of @dishes.TotalCount items
    ///     </p>
    /// }
    /// else
    /// {
    ///     <p class="text-center">No dishes available.</p>
    /// }
    /// 
    /// @code {
    ///     private PaginatedResponseDto<DishDto>? dishes;
    ///     
    ///     protected override async Task OnInitializedAsync()
    ///     {
    ///         await LoadDishes(page: 1);
    ///     }
    ///     
    ///     private async Task LoadDishes(int page)
    ///     {
    ///         dishes = await Http.GetFromJsonAsync<PaginatedResponseDto<DishDto>>(
    ///             $"api/dishes?page={page}&pageSize=10");
    ///     }
    ///     
    ///     private Task FirstPage() => LoadDishes(1);
    ///     private Task PreviousPage() => LoadDishes(dishes!.Page - 1);
    ///     private Task NextPage() => LoadDishes(dishes!.Page + 1);
    ///     private Task LastPage() => LoadDishes(dishes!.TotalPages);
    /// }
    /// ]]></code>
    /// 
    /// <para><strong>Best Practices:</strong></para>
    /// <list type="bullet">
    ///   <item><description>Always set TotalPages after setting TotalCount, Page, and PageSize</description></item>
    ///   <item><description>Use standard page sizes (10, 20, 50, 100) for consistency</description></item>
    ///   <item><description>Page numbers are 1-based (first page is 1, not 0)</description></item>
    ///   <item><description>Validate page and pageSize parameters in controllers before querying</description></item>
    ///   <item><description>Consider caching for frequently accessed paginated data</description></item>
    /// </list>
    /// 
    /// <para><strong>Performance Considerations:</strong></para>
    /// <para>For large datasets, ensure database queries use proper pagination (SKIP/TAKE in LINQ,
    /// OFFSET/FETCH in SQL) to avoid loading all data into memory. The TotalCount should be
    /// calculated efficiently (e.g., using COUNT query) without loading actual data.</para>
    /// </remarks>
    public class PaginatedResponseDto<T>
    {
        /// <summary>
        /// Gets or sets the list of items for the current page.
        /// </summary>
        /// <remarks>
        /// <para>Contains the actual data payload for the current page. The number of items
        /// should match PageSize (except for the last page which may have fewer items).</para>
        /// 
        /// <para><strong>Expected Behavior:</strong></para>
        /// <list type="bullet">
        ///   <item><description>Empty list if no data available for the requested page</description></item>
        ///   <item><description>Count ≤ PageSize (equal except for last page or when filtered)</description></item>
        ///   <item><description>Should never be null (initialized to empty list)</description></item>
        /// </list>
        /// </remarks>
        [Required]
        public List<T> Data { get; set; } = new();

        /// <summary>
        /// Gets or sets the total number of items across all pages.
        /// </summary>
        /// <remarks>
        /// <para>Represents the complete count of items in the dataset, not just the current page.
        /// Used to calculate TotalPages and determine pagination boundaries.</para>
        /// 
        /// <para><strong>Important:</strong></para>
        /// <para>This should be the total count AFTER applying filters but BEFORE applying pagination.
        /// For example, if searching for "pizza" returns 47 dishes, TotalCount = 47 regardless
        /// of page size or current page.</para>
        /// </remarks>
        [Range(0, int.MaxValue, ErrorMessage = "TotalCount must be non-negative")]
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the current page number (1-based).
        /// </summary>
        /// <remarks>
        /// <para>Represents which page of data is currently displayed.</para>
        /// 
        /// <para><strong>Convention:</strong></para>
        /// <list type="bullet">
        ///   <item><description>First page is 1 (not 0)</description></item>
        ///   <item><description>Must be ≥ 1 and ≤ TotalPages</description></item>
        ///   <item><description>If requesting page beyond TotalPages, should return last valid page or empty</description></item>
        /// </list>
        /// </remarks>
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        /// <remarks>
        /// <para>Defines how many items should be returned in each page.</para>
        /// 
        /// <para><strong>Typical Values:</strong></para>
        /// <list type="bullet">
        ///   <item><description>10 - Default for most lists</description></item>
        ///   <item><description>20 - Medium-sized lists</description></item>
        ///   <item><description>50 - Large lists or admin interfaces</description></item>
        ///   <item><description>100 - Maximum recommended for performance</description></item>
        /// </list>
        /// 
        /// <para><strong>Performance Note:</strong></para>
        /// <para>Larger page sizes reduce the number of API calls but increase response time
        /// and memory usage. Balance based on user experience and data size.</para>
        /// </remarks>
        [Range(1, 1000, ErrorMessage = "PageSize must be between 1 and 1000")]
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages.
        /// </summary>
        /// <remarks>
        /// <para>Calculated as: Ceiling(TotalCount / PageSize)</para>
        /// <para>Represents the maximum page number that can be requested.</para>
        /// 
        /// <para><strong>Examples:</strong></para>
        /// <list type="bullet">
        ///   <item><description>TotalCount = 47, PageSize = 10 → TotalPages = 5</description></item>
        ///   <item><description>TotalCount = 100, PageSize = 10 → TotalPages = 10</description></item>
        ///   <item><description>TotalCount = 0, PageSize = 10 → TotalPages = 0</description></item>
        /// </list>
        /// 
        /// <para><strong>Note:</strong></para>
        /// <para>Should be calculated and set automatically. If manually set, ensure it matches
        /// the formula to avoid inconsistencies in pagination UI.</para>
        /// </remarks>
        [Range(0, int.MaxValue, ErrorMessage = "TotalPages must be non-negative")]
        public int TotalPages { get; set; }

        // ===================================================================
        // COMPUTED HELPER PROPERTIES
        // ===================================================================

        /// <summary>
        /// Gets a value indicating whether there is a next page available.
        /// </summary>
        /// <remarks>
        /// <para>Computed as: Page &lt; TotalPages</para>
        /// <para>Used to enable/disable "Next" button in pagination controls.</para>
        /// </remarks>
        public bool HasNextPage => Page < TotalPages;

        /// <summary>
        /// Gets a value indicating whether there is a previous page available.
        /// </summary>
        /// <remarks>
        /// <para>Computed as: Page &gt; 1</para>
        /// <para>Used to enable/disable "Previous" button in pagination controls.</para>
        /// </remarks>
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// Gets the starting index (1-based) of items on the current page.
        /// </summary>
        /// <remarks>
        /// <para>Calculated as: ((Page - 1) * PageSize) + 1</para>
        /// <para>Example: Page 3, PageSize 10 → StartIndex = 21</para>
        /// <para>Used for displaying "Showing 21-30 of 100 items"</para>
        /// </remarks>
        public int StartIndex => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

        /// <summary>
        /// Gets the ending index (1-based) of items on the current page.
        /// </summary>
        /// <remarks>
        /// <para>Calculated as: Min(Page * PageSize, TotalCount)</para>
        /// <para>Example: Page 3, PageSize 10, TotalCount 25 → EndIndex = 25 (not 30)</para>
        /// <para>Used for displaying "Showing 21-25 of 25 items"</para>
        /// </remarks>
        public int EndIndex => Math.Min(Page * PageSize, TotalCount);

        /// <summary>
        /// Gets a value indicating whether the current page is the first page.
        /// </summary>
        /// <remarks>
        /// <para>Computed as: Page == 1</para>
        /// <para>Useful for conditional rendering of "First" button.</para>
        /// </remarks>
        public bool IsFirstPage => Page == 1;

        /// <summary>
        /// Gets a value indicating whether the current page is the last page.
        /// </summary>
        /// <remarks>
        /// <para>Computed as: Page == TotalPages</para>
        /// <para>Useful for conditional rendering of "Last" button.</para>
        /// </remarks>
        public bool IsLastPage => Page == TotalPages;

        // ===================================================================
        // CONVENIENCE FACTORY METHODS
        // ===================================================================

        /// <summary>
        /// Creates a new paginated response with calculated TotalPages.
        /// </summary>
        /// <param name="data">The data items for the current page.</param>
        /// <param name="totalCount">The total count of items across all pages.</param>
        /// <param name="page">The current page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A new PaginatedResponseDto instance with calculated TotalPages.</returns>
        /// <remarks>
        /// <para>Factory method that automatically calculates TotalPages based on TotalCount and PageSize.</para>
        /// <para>Recommended over manual property assignment to ensure consistency.</para>
        /// </remarks>
        public static PaginatedResponseDto<T> Create(List<T> data, int totalCount, int page, int pageSize)
        {
            return new PaginatedResponseDto<T>
            {
                Data = data ?? new List<T>(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <summary>
        /// Creates an empty paginated response (no data, page 1).
        /// </summary>
        /// <param name="pageSize">The page size to use (default: 10).</param>
        /// <returns>An empty PaginatedResponseDto instance.</returns>
        /// <remarks>
        /// <para>Useful for initial state or when no data is available.</para>
        /// </remarks>
        public static PaginatedResponseDto<T> Empty(int pageSize = 10)
        {
            return new PaginatedResponseDto<T>
            {
                Data = new List<T>(),
                TotalCount = 0,
                Page = 1,
                PageSize = pageSize,
                TotalPages = 0
            };
        }
    }
}
