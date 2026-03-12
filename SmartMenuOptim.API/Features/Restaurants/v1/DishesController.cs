using Microsoft.AspNetCore.Mvc;
using SmartMenuOptim.Application.Dtos.Dish;
using SmartMenuOptim.Application.Services.Restaurant;

namespace SmartMenuOptim.API.Features.Restaurants.v1;

/// <summary>
/// REST API Controller for Dish management operations.
/// </summary>
/// <remarks>
/// <para><strong>Endpoints:</strong></para>
/// <list type="bullet">
///   <item><description>GET /api/v1/dishes/{id} - Get dish by ID</description></item>
///   <item><description>GET /api/v1/restaurants/{restaurantId}/dishes - List dishes for restaurant</description></item>
///   <item><description>POST /api/v1/restaurants/{restaurantId}/dishes - Create dish</description></item>
///   <item><description>PUT /api/v1/dishes/{id} - Update dish</description></item>
///   <item><description>DELETE /api/v1/dishes/{id} - Delete dish</description></item>
/// </list>
/// </remarks>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class DishesController : ControllerBase
{
    private readonly IDishService _dishService;
    private readonly ILogger<DishesController> _logger;

    public DishesController(IDishService dishService, ILogger<DishesController> logger)
    {
        _dishService = dishService ?? throw new ArgumentNullException(nameof(dishService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET ENDPOINTS (QUERIES)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retrieves a dish by its unique identifier.
    /// </summary>
    [HttpGet("dishes/{id:int}")]
    [ProducesResponseType(typeof(DishDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DishDTO>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting dish with ID {DishId}", id);

        var result = await _dishService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(CreateProblemDetails("Dish.NotFound", result.Error, StatusCodes.Status404NotFound));
    }

    /// <summary>
    /// Retrieves all dishes for a restaurant.
    /// </summary>
    [HttpGet("restaurants/{restaurantId:int}/dishes")]
    [ProducesResponseType(typeof(IReadOnlyList<DishDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DishDTO>>> GetByRestaurantAsync(
        int restaurantId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting dishes for restaurant {RestaurantId}", restaurantId);

        var result = await _dishService.GetByRestaurantIdAsync(restaurantId, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Retrieves all dishes assigned to a specific menu.
    /// </summary>
    [HttpGet("menus/{menuId:int}/dishes")]
    [ProducesResponseType(typeof(IReadOnlyList<DishDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DishDTO>>> GetByMenuAsync(
        int menuId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting dishes for menu {MenuId}", menuId);

        var result = await _dishService.GetByMenuIdAsync(menuId, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(CreateProblemDetails("Menu.NotFound", result.Error, StatusCodes.Status404NotFound))
            : Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // POST/PUT/DELETE ENDPOINTS (COMMANDS)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new dish for a restaurant.
    /// </summary>
    [HttpPost("restaurants/{restaurantId:int}/dishes")]
    [ProducesResponseType(typeof(DishDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DishDTO>> CreateAsync(
        int restaurantId,
        [FromBody] DishCreateDTO dto,
        CancellationToken cancellationToken)
    {
        dto.RestaurantId = restaurantId;

        _logger.LogInformation("API: Creating dish '{DishName}' for restaurant {RestaurantId}", dto.Name, restaurantId);

        var result = await _dishService.CreateAsync(dto, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetByIdAsync),
                new { id = result.Value.Id },
                result.Value);
        }

        return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(CreateProblemDetails("Restaurant.NotFound", result.Error, StatusCodes.Status404NotFound))
            : BadRequest(CreateProblemDetails("Dish.ValidationError", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Updates an existing dish.
    /// </summary>
    [HttpPut("dishes/{id:int}")]
    [ProducesResponseType(typeof(DishDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DishDTO>> UpdateAsync(
        int id,
        [FromBody] DishUpdateDTO dto,
        CancellationToken cancellationToken)
    {
        if (id != dto.Id)
        {
            return BadRequest(CreateProblemDetails(
                "Dish.IdMismatch",
                "Route ID does not match body ID.",
                StatusCodes.Status400BadRequest));
        }

        _logger.LogInformation("API: Updating dish with ID {DishId}", id);

        var result = await _dishService.UpdateAsync(dto, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(CreateProblemDetails("Dish.NotFound", result.Error, StatusCodes.Status404NotFound))
            : BadRequest(CreateProblemDetails("Dish.ValidationError", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Soft-deletes a dish.
    /// </summary>
    [HttpDelete("dishes/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Deleting dish with ID {DishId}", id);

        var result = await _dishService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(CreateProblemDetails("Dish.NotFound", result.Error, StatusCodes.Status404NotFound));
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
