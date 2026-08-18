using System.Net;
using System.Net.Http.Json;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Auth.DTOs;

namespace SmartMenuOptim.Server.Features.Auth.Services;

/// <summary>
/// HTTP client-based implementation for the credential exchange. Talks to the backend API's
/// <c>POST api/auth/sign-in</c> endpoint (AllowAnonymous — it's the credential exchange itself) —
/// the Blazor Server login page never touches Infrastructure directly.
/// </summary>
public class AuthClientService(IHttpClientFactory httpClientFactory, ILogger<AuthClientService> logger) : IAuthClientService
{
    private const string ApiBasePath = "api/auth";

    public async Task<Result<SignInResultDTO>> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PostAsJsonAsync($"{ApiBasePath}/sign-in", new { email, password }, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return Result.Failure<SignInResultDTO>("Invalid email or password.");

            if (!response.IsSuccessStatusCode)
                return Result.Failure<SignInResultDTO>("Sign-in failed. Please try again.");

            var dto = await response.Content.ReadFromJsonAsync<SignInResultDTO>(cancellationToken);
            return dto is null
                ? Result.Failure<SignInResultDTO>("Sign-in failed. Please try again.")
                : Result.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during sign-in.");
            return Result.Failure<SignInResultDTO>("Sign-in failed. Please try again.");
        }
    }
}
