/*
 * File: ApiControllerBase.cs
 * Base controller with standardized response handling
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Provides common response handling methods for all API controllers,
 * ensuring consistent ProblemDetails responses and Result pattern handling.
 */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartMenuOptim.Application.Common;

namespace SmartMenuOptim.API.Common;

/// <summary>
/// Base controller providing standardized response handling.
/// </summary>
/// <remarks>
/// <para><strong>Features:</strong></para>
/// <list type="bullet">
///   <item><description>RFC 7807 ProblemDetails for error responses</description></item>
///   <item><description>Result pattern to ActionResult conversion</description></item>
///   <item><description>Consistent error code handling</description></item>
/// </list>
/// </remarks>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    // ═══════════════════════════════════════════════════════════════════════
    // PROBLEM DETAILS HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a standardized ProblemDetails response.
    /// </summary>
    /// <param name="errorCode">The error code (e.g., "Menu.NotFound").</param>
    /// <param name="detail">The detailed error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>A ProblemDetails object for error responses.</returns>
    protected static ProblemDetails CreateProblemDetails(
        string errorCode, 
        string detail, 
        int statusCode)
    {
        return new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = GetTitleForStatusCode(statusCode),
            Detail = detail,
            Status = statusCode,
            Extensions = { ["errorCode"] = errorCode }
        };
    }

    /// <summary>
    /// Creates a Not Found (404) ProblemDetails response.
    /// </summary>
    protected ProblemDetails NotFoundProblem(string errorCode, string message)
        => CreateProblemDetails(errorCode, message, StatusCodes.Status404NotFound);

    /// <summary>
    /// Creates a Bad Request (400) ProblemDetails response.
    /// </summary>
    protected ProblemDetails BadRequestProblem(string errorCode, string message)
        => CreateProblemDetails(errorCode, message, StatusCodes.Status400BadRequest);

    /// <summary>
    /// Creates an Unprocessable Entity (422) ProblemDetails response for business rule violations.
    /// </summary>
    protected ProblemDetails BusinessRuleProblem(string errorCode, string message)
        => CreateProblemDetails(errorCode, message, StatusCodes.Status422UnprocessableEntity);

    /// <summary>
    /// Creates a Conflict (409) ProblemDetails response.
    /// </summary>
    protected ProblemDetails ConflictProblem(string errorCode, string message)
        => CreateProblemDetails(errorCode, message, StatusCodes.Status409Conflict);

    // ═══════════════════════════════════════════════════════════════════════
    // RESULT TO ACTION RESULT CONVERSIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Converts a Result&lt;T&gt; to an ActionResult&lt;T&gt;.
    /// Returns Ok(200) on success, appropriate error response on failure.
    /// </summary>
    protected ActionResult<T> ToActionResult<T>(
        Result<T> result, 
        string entityName = "Resource")
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return ToErrorActionResult<T>(result.Error, entityName);
    }

    /// <summary>
    /// Converts a Result&lt;T&gt; to a Created (201) ActionResult on success.
    /// </summary>
    protected ActionResult<T> ToCreatedResult<T>(
        Result<T> result,
        string actionName,
        Func<T, object> routeValuesFunc,
        string entityName = "Resource")
    {
        if (result.IsSuccess)
            return CreatedAtAction(actionName, routeValuesFunc(result.Value), result.Value);

        return ToErrorActionResult<T>(result.Error, entityName);
    }

    /// <summary>
    /// Converts a Result to a NoContent (204) ActionResult on success.
    /// </summary>
    protected IActionResult ToNoContentResult(
        Result result, 
        string entityName = "Resource")
    {
        if (result.IsSuccess)
            return NoContent();

        return ToErrorActionResult(result.Error, entityName);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ERROR HANDLING HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Converts an error message to an appropriate error ActionResult.
    /// </summary>
    protected ActionResult<T> ToErrorActionResult<T>(string error, string entityName)
    {
        // Determine error type from message content
        if (ContainsIgnoreCase(error, "not found"))
        {
            return NotFound(NotFoundProblem($"{entityName}.NotFound", error));
        }

        if (ContainsIgnoreCase(error, "already exists") || ContainsIgnoreCase(error, "duplicate"))
        {
            return Conflict(ConflictProblem($"{entityName}.Conflict", error));
        }

        if (ContainsIgnoreCase(error, "cannot") || ContainsIgnoreCase(error, "invalid"))
        {
            return UnprocessableEntity(BusinessRuleProblem($"{entityName}.BusinessRuleViolation", error));
        }

        return BadRequest(BadRequestProblem($"{entityName}.ValidationError", error));
    }

    /// <summary>
    /// Converts an error message to an appropriate IActionResult.
    /// </summary>
    protected IActionResult ToErrorActionResult(string error, string entityName)
    {
        if (ContainsIgnoreCase(error, "not found"))
        {
            return NotFound(NotFoundProblem($"{entityName}.NotFound", error));
        }

        if (ContainsIgnoreCase(error, "already exists") || ContainsIgnoreCase(error, "duplicate"))
        {
            return Conflict(ConflictProblem($"{entityName}.Conflict", error));
        }

        if (ContainsIgnoreCase(error, "cannot") || ContainsIgnoreCase(error, "invalid"))
        {
            return UnprocessableEntity(BusinessRuleProblem($"{entityName}.BusinessRuleViolation", error));
        }

        return BadRequest(BadRequestProblem($"{entityName}.ValidationError", error));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    private static string GetTitleForStatusCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Business Rule Violation",
        StatusCodes.Status500InternalServerError => "Internal Server Error",
        _ => "Error"
    };

    private static bool ContainsIgnoreCase(string source, string value)
        => source.Contains(value, StringComparison.OrdinalIgnoreCase);
}
