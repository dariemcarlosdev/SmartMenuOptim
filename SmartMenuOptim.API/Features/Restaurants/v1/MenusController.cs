/*
 * File: MenusController.cs
 * REST API Controller for Menu aggregate operations
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
using SmartMenuOptim.Application.Dtos.Restaurant;
using SmartMenuOptim.Application.Services.Restaurant;

namespace SmartMenuOptim.API.Features.Restaurants.v1;

/// <summary>
/// REST API Controller for Menu management operations.
/// </summary>
/// <remarks>
/// <para><strong>Endpoints:</strong></para>
/// <list type="bullet">
///   <item><description>GET /api/v1/restaurants/{restaurantId}/menus - List menus for restaurant</description></item>
///   <item><description>GET /api/v1/menus/{id} - Get menu by ID</description></item>
///   <item><description>POST /api/v1/restaurants/{restaurantId}/menus - Create menu</description></item>
///   <item><description>PUT /api/v1/menus/{id} - Update menu</description></item>
///   <item><description>DELETE /api/v1/menus/{id} - Delete menu</description></item>
///   <item><description>POST /api/v1/menus/{id}/dishes/{dishId} - Add dish to menu</description></item>
///   <item><description>DELETE /api/v1/menus/{id}/dishes/{dishId} - Remove dish from menu</description></item>
/// </list>
/// </remarks>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ILogger<MenusController> _logger;

    public MenusController(IMenuService menuService, ILogger<MenusController> logger)
    {
        _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET ENDPOINTS (QUERIES)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retrieves a menu by its unique identifier.
    /// </summary>
    /// <param name="id">The menu ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The menu if found.</returns>
    [HttpGet("menus/{id:int}")]
    [ProducesResponseType(typeof(MenuDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuDTO>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting menu with ID {MenuId}", id);

        var result = await _menuService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(CreateProblemDetails("Menu.NotFound", result.Error, StatusCodes.Status404NotFound));
    }

    /// <summary>
    /// Retrieves all menus for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant ID.</param>
    /// <param name="activeOnly">If true, returns only active menus.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of menus.</returns>
    [HttpGet("restaurants/{restaurantId:int}/menus")]
    [ProducesResponseType(typeof(IReadOnlyList<MenuDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MenuDTO>>> GetByRestaurantAsync(
        int restaurantId,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("API: Getting menus for restaurant {RestaurantId}, activeOnly: {ActiveOnly}", restaurantId, activeOnly);

        var result = activeOnly
            ? await _menuService.GetActiveByRestaurantIdAsync(restaurantId, cancellationToken)
            : await _menuService.GetByRestaurantIdAsync(restaurantId, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // POST/PUT/DELETE ENDPOINTS (COMMANDS)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new menu for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant ID.</param>
    /// <param name="dto">The menu creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created menu.</returns>
    [HttpPost("restaurants/{restaurantId:int}/menus")]
    [ProducesResponseType(typeof(MenuDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuDTO>> CreateAsync(
        int restaurantId,
        [FromBody] MenuCreateDTO dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Creating menu '{MenuName}' for restaurant {RestaurantId}", dto.Name, restaurantId);

        var result = await _menuService.CreateAsync(restaurantId, dto, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetByIdAsync),
                new { id = result.Value.Id },
                result.Value);
        }

        return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(CreateProblemDetails("Restaurant.NotFound", result.Error, StatusCodes.Status404NotFound))
            : BadRequest(CreateProblemDetails("Menu.ValidationError", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Updates an existing menu.
    /// </summary>
    /// <param name="id">The menu ID.</param>
    /// <param name="dto">The menu update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated menu.</returns>
    [HttpPut("menus/{id:int}")]
    [ProducesResponseType(typeof(MenuDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuDTO>> UpdateAsync(
        int id,
        [FromBody] MenuUpdateDTO dto,
        CancellationToken cancellationToken)
    {
        if (id != dto.Id)
        {
            return BadRequest(CreateProblemDetails(
                "Menu.IdMismatch",
                "Route ID does not match body ID.",
                StatusCodes.Status400BadRequest));
        }

        _logger.LogInformation("API: Updating menu with ID {MenuId}", id);

        var result = await _menuService.UpdateAsync(dto, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(CreateProblemDetails("Menu.NotFound", result.Error, StatusCodes.Status404NotFound))
            : BadRequest(CreateProblemDetails("Menu.ValidationError", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Soft-deletes a menu.
    /// </summary>
    /// <param name="id">The menu ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("menus/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Deleting menu with ID {MenuId}", id);

        var result = await _menuService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(CreateProblemDetails("Menu.NotFound", result.Error, StatusCodes.Status404NotFound));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MENU AVAILABILITY ENDPOINTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Makes a menu available/active.
    /// </summary>
    /// <param name="id">The menu ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("menus/{id:int}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MakeAvailableAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Making menu {MenuId} available", id);

        var result = await _menuService.MakeAvailableAsync(id, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(CreateProblemDetails("Menu.NotFound", result.Error, StatusCodes.Status404NotFound))
            : BadRequest(CreateProblemDetails("Menu.CannotActivate", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Makes a menu unavailable/inactive.
    /// </summary>
    /// <param name="id">The menu ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("menus/{id:int}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MakeUnavailableAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Making menu {MenuId} unavailable", id);

        var result = await _menuService.MakeUnavailableAsync(id, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(CreateProblemDetails("Menu.NotFound", result.Error, StatusCodes.Status404NotFound));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DISH MANAGEMENT ENDPOINTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds a dish to a menu.
    /// </summary>
    /// <param name="menuId">The menu ID.</param>
    /// <param name="dishId">The dish ID to add.</param>
    /// <param name="displayOrder">Optional display order.</param>
    /// <param name="specialPrice">Optional special price for this menu.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("menus/{menuId:int}/dishes/{dishId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddDishAsync(
        int menuId,
        int dishId,
        [FromQuery] int displayOrder = 0,
        [FromQuery] decimal? specialPrice = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("API: Adding dish {DishId} to menu {MenuId}", dishId, menuId);

        var result = await _menuService.AddDishToMenuAsync(menuId, dishId, displayOrder, specialPrice, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(CreateProblemDetails("Menu.DishNotFound", result.Error, StatusCodes.Status404NotFound))
            : BadRequest(CreateProblemDetails("Menu.CannotAddDish", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Removes a dish from a menu.
    /// </summary>
    /// <param name="menuId">The menu ID.</param>
    /// <param name="dishId">The dish ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("menus/{menuId:int}/dishes/{dishId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveDishAsync(
        int menuId,
        int dishId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Removing dish {DishId} from menu {MenuId}", dishId, menuId);

        var result = await _menuService.RemoveDishFromMenuAsync(menuId, dishId, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(CreateProblemDetails("Menu.NotFound", result.Error, StatusCodes.Status404NotFound));
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
