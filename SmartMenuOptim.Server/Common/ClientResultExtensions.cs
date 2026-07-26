/*
 * File: ClientResultExtensions.cs
 * Extension methods for client-side Result handling
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Provides extension methods for converting API responses to ClientResult,
 * mapping, and transforming client results.
 */

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartMenuOptim.Server.Models.Api;

namespace SmartMenuOptim.Server.Common;

/// <summary>
/// Extension methods for client-side Result handling.
/// </summary>
public static class ClientResultExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ═══════════════════════════════════════════════════════════════════════
    // HTTP RESPONSE TO CLIENT RESULT
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Converts an HTTP response to a ClientResult&lt;T&gt;.
    /// </summary>
    public static async Task<ClientResult<T>> ToClientResultAsync<T>(
        this HttpResponseMessage response,
        string fallbackError = "An error occurred.",
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
        {
            try
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                return data is not null
                    ? HttpClientResult.Success(data)
                    : HttpClientResult.Failure<T>(fallbackError);
            }
            catch (JsonException)
            {
                return HttpClientResult.Failure<T>("Failed to parse server response.");
            }
        }

        var (error, errorCode) = await ExtractErrorAsync(response, fallbackError);
        return HttpClientResult.Failure<T>(error, errorCode);
    }

    /// <summary>
    /// Converts an HTTP response to a ClientResult (no data).
    /// </summary>
    public static async Task<HttpClientResult> ToClientResultAsync(
        this HttpResponseMessage response,
        string fallbackError = "An error occurred.")
    {
        if (response.IsSuccessStatusCode)
            return HttpClientResult.Success();

        var (error, errorCode) = await ExtractErrorAsync(response, fallbackError);
        return HttpClientResult.Failure(error, errorCode);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MAPPING EXTENSIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps a successful result to a new value.
    /// </summary>
    public static ClientResult<TOut> Map<TIn, TOut>(
        this ClientResult<TIn> result, 
        Func<TIn, TOut> mapper)
    {
        return result.IsSuccess
            ? HttpClientResult.Success(mapper(result.Value))
            : HttpClientResult.Failure<TOut>(result.Error, result.ErrorCode);
    }

    /// <summary>
    /// Pattern matches on the result.
    /// </summary>
    public static TOut Match<TIn, TOut>(
        this ClientResult<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<string, TOut> onFailure)
    {
        return result.IsSuccess
            ? onSuccess(result.Value)
            : onFailure(result.Error);
    }

    /// <summary>
    /// Gets the value or a default if the result failed.
    /// </summary>
    public static T GetValueOrDefault<T>(this ClientResult<T> result, T defaultValue = default!)
    {
        return result.IsSuccess ? result.Value : defaultValue;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ERROR EXTRACTION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Extracts error information from an HTTP response.
    /// </summary>
    private static async Task<(string Error, string? ErrorCode)> ExtractErrorAsync(
        HttpResponseMessage response,
        string fallbackError)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
                return (GetDefaultErrorForStatus(response.StatusCode, fallbackError), null);

            var problemDetails = JsonSerializer.Deserialize<ProblemDetailsResponseDto>(content, JsonOptions);
            if (problemDetails is not null)
            {
                var message = problemDetails.Detail ?? problemDetails.Title ?? fallbackError;
                var code = problemDetails.Extensions?.TryGetValue("errorCode", out var codeValue) == true
                    ? codeValue?.ToString()
                    : null;
                return (message, code);
            }
        }
        catch
        {
            // Ignore deserialization errors
        }

        return (GetDefaultErrorForStatus(response.StatusCode, fallbackError), null);
    }

    /// <summary>
    /// Gets a user-friendly error message for a status code.
    /// </summary>
    private static string GetDefaultErrorForStatus(HttpStatusCode statusCode, string fallbackError)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "The requested resource was not found.",
            HttpStatusCode.BadRequest => "The request was invalid.",
            HttpStatusCode.Unauthorized => "You are not authorized to perform this action.",
            HttpStatusCode.Forbidden => "Access to this resource is forbidden.",
            HttpStatusCode.Conflict => "A conflict occurred with the current state.",
            HttpStatusCode.UnprocessableEntity => "The request could not be processed.",
            HttpStatusCode.InternalServerError => "An internal server error occurred.",
            HttpStatusCode.ServiceUnavailable => "The service is temporarily unavailable.",
            _ => fallbackError
        };
    }
}
