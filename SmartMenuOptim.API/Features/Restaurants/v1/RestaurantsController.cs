/*
 * File: RestaurantController.cs
 * REST API Controller for Restaurant aggregate operations (renamed from RestaurantsController)
 * Version: 1.0
 * .NET Target: .NET 8/9
 * 
 * ═══════════════════════════════════════════════════════════════════════════════
 * ARCHITECTURAL NOTE - HYBRID APPROACH (MVP)
 * ═══════════════════════════════════════════════════════════════════════════════
 * 
 * Current Implementation: Service Layer Pattern
 * - Controllers depend on IRestaurantService (Application Services)
 * - Suitable for MVP and simple CRUD operations
 * - Business logic encapsulated in services
 * 
 * TODO: Refactor to CQRS after MVP
 * ═══════════════════════════════════════════════════════════════════════════════
 * Post-MVP: Refactor to full CQRS + MediatR pattern per REST-API-QUICK-PROMPT.md
 * 
 * Target Architecture:
 * - Controllers depend ONLY on ISender (MediatR)
 * - One endpoint = One Command or Query
 * - FluentValidation via pipeline behaviors
 * - Domain events dispatched via handlers
 * 
 * Structure: Feature-Slice (Vertical Slice) Modularity
 * - Application/Features/Restaurant/Commands/CreateRestaurant/
 * - Application/Features/Restaurant/Queries/GetRestaurantById/
 * 
 * Example refactored endpoint:
 * [HttpPost]
 * public async Task<ActionResult<RestaurantDTO>> Create(
 *     [FromBody] CreateRestaurantRequest req, CancellationToken ct)
 * {
 *     var result = await _sender.Send(new CreateRestaurantCommand(req), ct);
 *     return result.Match(
 *         restaurant => CreatedAtAction(nameof(GetById), new { id = restaurant.Id }, restaurant),
 *         error => BadRequest(CreateProblem(error)));
 * }
 * ═══════════════════════════════════════════════════════════════════════════════
 * 
 * Design Patterns:
 * - Clean Architecture: Presentation → Application → Domain
 * - Result Pattern: Returns operation results with success/failure semantics
 * - RFC 7807: ProblemDetails for error responses
 */

using Microsoft.AspNetCore.Mvc;
using SmartMenuOptim.Application.Features.Restaurants.Services;

namespace SmartMenuOptim.API.Features.Restaurants.v1;

/// <summary>
/// REST API Controller for Restaurant management operations.
/// </summary>
/// <remarks>
/// <para><strong>Framework:</strong> .NET 8/9 | Clean Architecture + DDD</para>
/// <para><strong>Current:</strong> Service Layer Pattern (MVP)</para>
/// <para><strong>Future:</strong> CQRS + MediatR (Post-MVP refactoring)</para>
/// 
/// <para><strong>Endpoints:</strong></para>
/// <list type="bullet">
///   <item><description>GET /api/v1/restaurants - List all restaurants</description></item>
///   <item><description>GET /api/v1/restaurants/{id} - Get restaurant by ID</description></item>
///   <item><description>GET /api/v1/restaurants/{id}/detail - Get restaurant with full details</description></item>
///   <item><description>POST /api/v1/restaurants - Create new restaurant</description></item>
///   <item><description>PUT /api/v1/restaurants/{id} - Update restaurant</description></item>
///   <item><description>DELETE /api/v1/restaurants/{id} - Soft-delete restaurant</description></item>
///   <item><description>PATCH /api/v1/restaurants/{id}/status - Toggle accepting orders</description></item>
/// </list>
/// </remarks>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class RestaurantsController : ControllerBase
{
    private readonly IRestaurantService _restaurantService;
    private readonly ILogger<RestaurantsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestaurantsController"/> class.
    /// </summary>
    /// <param name="restaurantService">The restaurant service.</param>
    /// <param name="logger">The logger.</param>
    /// <remarks>
    /// TODO (Post-MVP): Replace IRestaurantService with ISender (MediatR)
    /// </remarks>
    public RestaurantsController(
        IRestaurantService restaurantService,
        ILogger<RestaurantsController> logger)
    {
        _restaurantService = restaurantService ?? throw new ArgumentNullException(nameof(restaurantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET ENDPOINTS (QUERIES)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retrieves all restaurants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of restaurants.</returns>
    /// <response code="200">Returns the list of restaurants.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RestaurantDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<RestaurantDTO>>> GetAllAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting all restaurants");

        var result = await _restaurantService.GetAllAsync(cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Retrieves a restaurant by its unique identifier.
    /// </summary>
    /// <param name="id">The restaurant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The restaurant if found.</returns>
    /// <response code="200">Returns the restaurant.</response>
    /// <response code="404">Restaurant not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RestaurantDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantDTO>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting restaurant with ID {RestaurantId}", id);

        var result = await _restaurantService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(CreateProblemDetails("Restaurant.NotFound", result.Error, StatusCodes.Status404NotFound));
    }

    /// <summary>
    /// Retrieves a restaurant with full details including menus, dishes, and categories.
    /// </summary>
    /// <param name="id">The restaurant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The restaurant with full details if found.</returns>
    /// <response code="200">Returns the restaurant with details.</response>
    /// <response code="404">Restaurant not found.</response>
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType(typeof(RestaurantDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantDetailDTO>> GetDetailByIdAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting detailed restaurant with ID {RestaurantId}", id);

        var result = await _restaurantService.GetDetailByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(CreateProblemDetails("Restaurant.NotFound", result.Error, StatusCodes.Status404NotFound));
    }

    /// <summary>
    /// Retrieves all restaurants owned by a specific user.
    /// </summary>
    /// <param name="ownerId">The owner's user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of restaurants owned by the user.</returns>
    /// <response code="200">Returns the list of restaurants.</response>
    [HttpGet("owner/{ownerId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<RestaurantDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RestaurantDTO>>> GetByOwnerAsync(int ownerId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting restaurants for owner {OwnerId}", ownerId);

        var result = await _restaurantService.GetByOwnerAsync(ownerId, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // POST/PUT/DELETE ENDPOINTS (COMMANDS)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new restaurant.
    /// </summary>
    /// <param name="dto">The restaurant creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created restaurant.</returns>
    /// <response code="201">Restaurant created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="409">Conflict (e.g., duplicate).</response>
    [HttpPost]
    [ProducesResponseType(typeof(RestaurantDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RestaurantDTO>> CreateAsync(
        [FromBody] RestaurantCreateDTO dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Creating new restaurant: {RestaurantName}", dto.Name);

        var result = await _restaurantService.CreateAsync(dto, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetByIdAsync),
                new { id = result.Value.Id },
                result.Value);
        }

        // Determine appropriate error response
        return result.Error.Contains("already exists", StringComparison.OrdinalIgnoreCase)
            ? Conflict(CreateProblemDetails("Restaurant.Conflict", result.Error, StatusCodes.Status409Conflict))
            : BadRequest(CreateProblemDetails("Restaurant.ValidationError", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Updates an existing restaurant.
    /// </summary>
    /// <param name="id">The restaurant ID.</param>
    /// <param name="dto">The restaurant update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated restaurant.</returns>
    /// <response code="200">Restaurant updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="404">Restaurant not found.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RestaurantDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantDTO>> UpdateAsync(
        int id,
        [FromBody] RestaurantUpdateDTO dto,
        CancellationToken cancellationToken)
    {
        if (id != dto.Id)
        {
            return BadRequest(CreateProblemDetails(
                "Restaurant.IdMismatch",
                "Route ID does not match body ID.",
                StatusCodes.Status400BadRequest));
        }

        _logger.LogInformation("API: Updating restaurant with ID {RestaurantId}", id);

        var result = await _restaurantService.UpdateAsync(dto, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(CreateProblemDetails("Restaurant.NotFound", result.Error, StatusCodes.Status404NotFound))
            : BadRequest(CreateProblemDetails("Restaurant.ValidationError", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Soft-deletes a restaurant.
    /// </summary>
    /// <param name="id">The restaurant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Restaurant deleted successfully.</response>
    /// <response code="404">Restaurant not found.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Deleting restaurant with ID {RestaurantId}", id);

        var result = await _restaurantService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(CreateProblemDetails("Restaurant.NotFound", result.Error, StatusCodes.Status404NotFound));
    }

    /// <summary>
    /// Toggles whether a restaurant is accepting orders.
    /// </summary>
    /// <param name="id">The restaurant ID.</param>
    /// <param name="isAccepting">Whether the restaurant should accept orders.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Status updated successfully.</response>
    /// <response code="404">Restaurant not found.</response>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleAcceptingOrdersAsync(
        int id,
        [FromQuery] bool isAccepting,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Setting restaurant {RestaurantId} accepting orders to {IsAccepting}", id, isAccepting);

        var result = await _restaurantService.ToggleAcceptingOrdersAsync(id, isAccepting, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(CreateProblemDetails("Restaurant.NotFound", result.Error, StatusCodes.Status404NotFound));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BUSINESS HOURS ENDPOINTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sets the business hours for a restaurant.
    /// </summary>
    /// <param name="id">The restaurant ID.</param>
    /// <param name="businessHours">The list of business hours.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Business hours updated successfully.</response>
    /// <response code="400">Invalid business hours data.</response>
    /// <response code="404">Restaurant not found.</response>
    [HttpPut("{id:int}/business-hours")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetBusinessHoursAsync(
        int id,
        [FromBody] List<BusinessHoursDTO> businessHours,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Setting business hours for restaurant {RestaurantId}", id);

        var result = await _restaurantService.SetBusinessHoursAsync(id, businessHours, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(CreateProblemDetails("Restaurant.NotFound", result.Error, StatusCodes.Status404NotFound))
            : BadRequest(CreateProblemDetails("Restaurant.BusinessHoursError", result.Error, StatusCodes.Status400BadRequest));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a ProblemDetails response following RFC 7807.
    /// </summary>
    /// <param name="title">Error title/code.</param>
    /// <param name="detail">Error detail message.</param>
    /// <param name="status">HTTP status code.</param>
    /// <returns>ProblemDetails object.</returns>
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
