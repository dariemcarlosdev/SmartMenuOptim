/*
 * File: OrdersController.cs
 * REST API Controller for Order aggregate operations
 * Version: 1.0
 * .NET Target: .NET 8/9
 * 
 * ═══════════════════════════════════════════════════════════════════════════════
 * ARCHITECTURAL NOTE - HYBRID APPROACH (MVP)
 * ═══════════════════════════════════════════════════════════════════════════════
 * 
 * Current Implementation: Service Layer Pattern
 * - Controllers depend on IOrderService (Application Services)
 * - Suitable for MVP and simple CRUD operations
 * - Business logic encapsulated in services
 * 
 * TODO: Refactor to CQRS after MVP
 * ═══════════════════════════════════════════════════════════════════════════════
 * Post-MVP: Refactor to full CQRS + MediatR pattern
 * 
 * Target Architecture:
 * - Controllers depend ONLY on ISender (MediatR)
 * - One endpoint = One Command or Query
 * - FluentValidation via pipeline behaviors
 * - Domain events dispatched via handlers
 * ═══════════════════════════════════════════════════════════════════════════════
 * 
 * Design Patterns:
 * - Clean Architecture: Presentation → Application → Domain
 * - Result Pattern: Returns operation results with success/failure semantics
 * - RFC 7807: ProblemDetails for error responses
 * 
 * Reference: RestaurantsController.cs (canonical pattern)
 */

using Microsoft.AspNetCore.Mvc;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Customers.DTOs;
using SmartMenuOptim.Application.Features.Orders.DTOs;
using SmartMenuOptim.Application.Features.Orders.Services;

namespace SmartMenuOptim.API.Features.Orders.v1;

/// <summary>
/// REST API Controller for Order management operations.
/// </summary>
/// <remarks>
/// <para><strong>Framework:</strong> .NET 8/9 | Clean Architecture + DDD</para>
/// <para><strong>Current:</strong> Service Layer Pattern (MVP)</para>
/// <para><strong>Future:</strong> CQRS + MediatR (Post-MVP refactoring)</para>
/// 
/// <para><strong>Endpoints:</strong></para>
/// <list type="bullet">
///   <item><description>GET /api/v1/orders?restaurantId={id} - List orders by restaurant</description></item>
///   <item><description>GET /api/v1/orders/{id} - Get order detail</description></item>
///   <item><description>POST /api/v1/orders - Place new order</description></item>
///   <item><description>PATCH /api/v1/orders/{id}/status - Change order status</description></item>
///   <item><description>POST /api/v1/orders/{id}/cancel - Cancel order with reason</description></item>
///   <item><description>DELETE /api/v1/orders/{id} - Soft-delete order</description></item>
///   <item><description>GET /api/v1/orders/statuses?restaurantId={id} - List available statuses</description></item>
/// </list>
/// </remarks>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrdersController"/> class.
    /// </summary>
    /// <param name="orderService">The order service.</param>
    /// <param name="logger">The logger.</param>
    /// <remarks>
    /// TODO (Post-MVP): Replace IOrderService with ISender (MediatR)
    /// </remarks>
    public OrdersController(
        IOrderService orderService,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET ENDPOINTS (QUERIES)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retrieves all orders for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of orders for the restaurant.</returns>
    /// <response code="200">Returns the list of orders.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<OrderDTO>>> GetByRestaurantAsync(
        [FromQuery] int restaurantId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting orders for restaurant {RestaurantId}", restaurantId);

        var result = await _orderService.GetAllByRestaurantAsync(restaurantId, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Retrieves a paginated list of orders for a restaurant with sorting and optional status filtering.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier (required).</param>
    /// <param name="request">Pagination and sorting parameters (page, pageSize, sortBy, sortDirection).</param>
    /// <param name="status">Optional status name filter (exact match).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of orders with metadata.</returns>
    /// <response code="200">Returns the paginated list of orders.</response>
    /// <response code="400">Invalid pagination parameters.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("paginated")]
    [ProducesResponseType(typeof(PaginatedResponse<OrderDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedResponse<OrderDTO>>> GetByRestaurantPaginatedAsync(
        [FromQuery] int restaurantId,
        [FromQuery] PaginatedRequest request,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "API: Getting paginated orders for restaurant {RestaurantId} — page {Page}, size {PageSize}, sort {SortBy} {SortDir}, status {Status}",
            restaurantId, request.Page, request.PageSize, request.SortBy, request.SortDirection, status ?? "(none)");

        var result = await _orderService.GetAllByRestaurantPaginatedAsync(
            restaurantId, request, status, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Retrieves an order with full details including items.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order detail if found.</returns>
    /// <response code="200">Returns the order with details.</response>
    /// <response code="404">Order not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDetailDTO>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting order with ID {OrderId}", id);

        var result = await _orderService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(CreateProblemDetails("Order.NotFound", result.Error, StatusCodes.Status404NotFound));
    }

    /// <summary>
    /// Retrieves available order statuses for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available order statuses.</returns>
    /// <response code="200">Returns the list of order statuses.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("statuses")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderStatusDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<OrderStatusDTO>>> GetStatusesAsync(
        [FromQuery] int restaurantId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting order statuses for restaurant {RestaurantId}", restaurantId);

        var result = await _orderService.GetStatusesAsync(restaurantId, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Retrieves a lightweight list of all customers for dropdown/lookup scenarios.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of customer lookups (Id + Name).</returns>
    /// <response code="200">Returns the list of customer lookups.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("customers/lookup")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerLookupDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<CustomerLookupDTO>>> GetCustomerLookupsAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("API: Getting customer lookups for order form");

        var result = await _orderService.GetCustomerLookupsAsync(cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // POST/PATCH/DELETE ENDPOINTS (COMMANDS)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Places a new order.
    /// </summary>
    /// <param name="dto">The order creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created order.</returns>
    /// <response code="201">Order created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDTO>> CreateAsync(
        [FromBody] OrderCreateDTO dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Creating new order for restaurant {RestaurantId}, customer {CustomerId}",
            dto.RestaurantId, dto.CustomerId);

        var result = await _orderService.CreateAsync(dto, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetByIdAsync),
                new { id = result.Value.Id },
                result.Value);
        }

        return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(CreateProblemDetails("Order.NotFound", result.Error, StatusCodes.Status404NotFound))
            : BadRequest(CreateProblemDetails("Order.ValidationError", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Changes the status of an existing order.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="request">The status update request containing the new status ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated order.</returns>
    /// <response code="200">Order status updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="404">Order not found.</response>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(OrderDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDTO>> UpdateStatusAsync(
        int id,
        [FromBody] OrderStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Updating status of order {OrderId} to status {StatusId}", id, request.NewStatusId);

        var result = await _orderService.UpdateStatusAsync(id, request.NewStatusId, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(CreateProblemDetails("Order.NotFound", result.Error, StatusCodes.Status404NotFound))
            : BadRequest(CreateProblemDetails("Order.StatusError", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Cancels an order with a specified reason.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="request">The cancellation request containing the reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Order cancelled successfully.</response>
    /// <response code="400">Invalid request or order cannot be cancelled.</response>
    /// <response code="404">Order not found.</response>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelAsync(
        int id,
        [FromBody] OrderCancelRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Cancelling order {OrderId} with reason: {Reason}", id, request.Reason);

        var result = await _orderService.CancelAsync(id, request.Reason, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(CreateProblemDetails("Order.NotFound", result.Error, StatusCodes.Status404NotFound))
            : BadRequest(CreateProblemDetails("Order.CancelError", result.Error, StatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Soft-deletes an order.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Order deleted successfully.</response>
    /// <response code="404">Order not found.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("API: Deleting order with ID {OrderId}", id);

        var result = await _orderService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(CreateProblemDetails("Order.NotFound", result.Error, StatusCodes.Status404NotFound));
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

// ═══════════════════════════════════════════════════════════════════════
// REQUEST MODELS (API-layer only — not application DTOs)
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Request model for updating an order's status.
/// </summary>
public class OrderStatusUpdateRequest
{
    /// <summary>
    /// The new order status identifier.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "New status ID is required")]
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Status ID must be a positive number")]
    public int NewStatusId { get; set; }
}

/// <summary>
/// Request model for cancelling an order.
/// </summary>
public class OrderCancelRequest
{
    /// <summary>
    /// The reason for cancelling the order.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Cancellation reason is required")]
    [System.ComponentModel.DataAnnotations.StringLength(500, MinimumLength = 3, ErrorMessage = "Reason must be between 3 and 500 characters")]
    public string Reason { get; set; } = string.Empty;
}
