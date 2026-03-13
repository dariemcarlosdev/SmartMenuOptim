/*
 * File: CategoriesController.cs
 * REST API Controller for DishCategory entity operations
 * Version: 1.0
 * .NET Target: .NET 8/9
 * 
 * ═══════════════════════════════════════════════════════════════════════════════
 * ARCHITECTURAL NOTE - HYBRID APPROACH (MVP)
 * ═══════════════════════════════════════════════════════════════════════════════
 * 
 * Current Implementation: Service Layer Pattern
 * Future (Post-MVP): Refactor to CQRS + MediatR pattern
 * 
 * Framework: .NET 8/9 | Clean Architecture + DDD + CQRS + MediatR
 * Structure: Feature-Slice (Vertical Slice) Modularity
 * 
 * TODO: Refactor to ISender (MediatR) after MVP
 * - FluentValidation via pipeline behaviors
 * - Domain events dispatched via handlers
 * ═══════════════════════════════════════════════════════════════════════════════
 */

using Microsoft.AspNetCore.Mvc;
using SmartMenuOptim.Application.Features.Restaurants.Services;

namespace SmartMenuOptim.API.Features.Restaurants.v1;

/// <summary>
/// REST API Controller for Category management operations.
/// </summary>
/// <remarks>
/// <para><strong>Endpoints:</strong></para>
/// <list type="bullet">
///   <item><description>GET /api/v1/restaurants/{restaurantId}/categories - List categories for restaurant</description></item>
///   <item><description>GET /api/v1/categories/{id} - Get category by ID</description></item>
///   <item><description>POST /api/v1/restaurants/{restaurantId}/categories - Create category</description></item>
///   <item><description>PUT /api/v1/categories/{id} - Update category</description></item>
///   <item><description>DELETE /api/v1/categories/{id} - Delete category</description></item>
///   <item><description>PUT /api/v1/restaurants/{restaurantId}/categories/reorder - Reorder categories</description></item>
/// </list>
/// </remarks>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET ENDPOINTS (QUERIES)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retrieves a category by its unique identifier.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category if found.</returns>
    [HttpGet("categories/{id:int}")]
    [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDTO>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting category with ID {CategoryId}", id);

        var result = await _categoryService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(CreateProblemDetails("Category.NotFound", result.Error, StatusCodes.Status404NotFound));
    }

    /// <summary>
    /// Retrieves all categories for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of categories ordered by display order.</returns>
    [HttpGet("restaurants/{restaurantId:int}/categories")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDTO>>> GetByRestaurantAsync(
        int restaurantId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting categories for restaurant {RestaurantId}", restaurantId);

        var result = await _categoryService.GetByRestaurantIdAsync(restaurantId, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // POST/PUT/DELETE ENDPOINTS (COMMANDS)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new category for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant ID.</param>
    /// <param name="dto">The category creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created category.</returns>
    [HttpPost("restaurants/{restaurantId:int}/categories")]
    [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDTO>> CreateAsync(
        int restaurantId,
        [FromBody] CategoryCreateDTO dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Creating category '{CategoryName}' for restaurant {RestaurantId}", dto.Name, restaurantId);

        var result = await _categoryService.CreateAsync(restaurantId, dto, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetByIdAsync),
                new { id = result.Value.Id },
                result.Value);
        }

        if (result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(CreateProblemDetails("Restaurant.NotFound", result.Error, StatusCodes.Status404NotFound));
        }

        if (result.Error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(CreateProblemDetails("Category.Duplicate", result.Error, StatusCodes.Status409Conflict));
        }

        return BadRequest(CreateProblemDetails("Category.ValidationError", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="dto">The category update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated category.</returns>
    [HttpPut("categories/{id:int}")]
    [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDTO>> UpdateAsync(
        int id,
        [FromBody] CategoryUpdateDTO dto,
        CancellationToken cancellationToken)
    {
        if (id != dto.Id)
        {
            return BadRequest(CreateProblemDetails(
                "Category.IdMismatch",
                "Route ID does not match body ID.",
                StatusCodes.Status400BadRequest));
        }

        _logger.LogInformation("API: Updating category with ID {CategoryId}", id);

        var result = await _categoryService.UpdateAsync(dto, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(CreateProblemDetails("Category.NotFound", result.Error, StatusCodes.Status404NotFound));
        }

        if (result.Error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(CreateProblemDetails("Category.Duplicate", result.Error, StatusCodes.Status409Conflict));
        }

        return BadRequest(CreateProblemDetails("Category.ValidationError", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Soft-deletes a category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Category deleted successfully.</response>
    /// <response code="400">Cannot delete category with active dishes.</response>
    /// <response code="404">Category not found.</response>
    [HttpDelete("categories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Deleting category with ID {CategoryId}", id);

        var result = await _categoryService.DeleteAsync(id, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        if (result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(CreateProblemDetails("Category.NotFound", result.Error, StatusCodes.Status404NotFound));
        }

        // Category has active dishes - cannot delete
        return BadRequest(CreateProblemDetails("Category.HasDishes", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Reorders categories within a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant ID.</param>
    /// <param name="categoryOrders">Dictionary mapping category ID to new display order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("restaurants/{restaurantId:int}/categories/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReorderCategoriesAsync(
        int restaurantId,
        [FromBody] Dictionary<int, int> categoryOrders,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Reordering {Count} categories for restaurant {RestaurantId}", 
            categoryOrders.Count, restaurantId);

        var result = await _categoryService.ReorderCategoriesAsync(restaurantId, categoryOrders, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(CreateProblemDetails("Category.ReorderError", result.Error, StatusCodes.Status400BadRequest));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════════

    private ProblemDetails CreateProblemDetails(string title, string detail, int status)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status,
            Instance = HttpContext.Request.Path,
            Extensions = { ["traceId"] = HttpContext.TraceIdentifier }
        };
    }
}
