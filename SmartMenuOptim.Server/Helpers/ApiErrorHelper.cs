/*
 * File: ApiErrorHelper.cs
 * Helper class for extracting error messages from API responses
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Provides consistent error message extraction from RFC 7807 ProblemDetails 
 * responses returned by the API when domain exceptions occur.
 */

using System.Text.Json;
using SmartMenuOptim.Server.Models.Api;

namespace SmartMenuOptim.Server.Helpers;

/// <summary>
/// Helper class for extracting error messages from API responses. Static methods to parse RFC 7807 ProblemDetails responses and return user-friendly error messages.
/// </summary>
public static class ApiErrorHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Attempts to extract an error message from an HTTP response.
    /// Supports RFC 7807 ProblemDetails format.
    /// </summary>
    /// <param name="response">The HTTP response to extract the error from.</param>
    /// <param name="fallbackMessage">The fallback message if extraction fails.</param>
    /// <returns>The extracted error message or the fallback message.</returns>
    public static async Task<string> GetErrorMessageAsync(HttpResponseMessage response, string fallbackMessage)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
                return fallbackMessage;

            // Try to parse as ProblemDetails
            var problemDetails = JsonSerializer.Deserialize<ProblemDetailsResponseDto>(content, JsonOptions);

            // Return Detail first (more specific), then Title, then fallback
            return problemDetails?.Detail ?? problemDetails?.Title ?? fallbackMessage;
        }
        catch
        {
            return fallbackMessage;
        }
    }
}
