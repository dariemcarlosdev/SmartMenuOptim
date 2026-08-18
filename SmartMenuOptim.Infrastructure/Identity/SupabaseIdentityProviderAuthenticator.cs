using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SmartMenuOptim.Application.Contracts.Identity;

namespace SmartMenuOptim.Infrastructure.Identity;

/// <summary>
/// Supabase Auth (GoTrue) implementation of <see cref="IIdentityProviderAuthenticator"/>, using the
/// password grant token endpoint (<c>POST {Authority}/token?grant_type=password</c>).
/// </summary>
/// <remarks>
/// Authenticated with the project's public anon/publishable key (<c>apikey</c> header) — distinct from
/// the secret service-role key used by <see cref="SupabaseIdentityProviderAdminClient"/>. The response
/// carries the authenticated user's <c>id</c>/<c>email</c> directly, so no JWT decoding is needed here.
/// <see cref="HttpClient"/> is registered as a typed client in
/// <c>InfrastructureServiceCollectionExtensions.AddIdentityProviderAuthenticator</c> with
/// <c>BaseAddress</c> and <c>apikey</c> header sourced from config (user-secrets/env vars, never
/// <c>appsettings.json</c>). See docs/06-Security/AUTHENTICATION_FRAMEWORK.md §4.
/// </remarks>
internal sealed class SupabaseIdentityProviderAuthenticator(HttpClient httpClient) : IIdentityProviderAuthenticator
{
    public async Task<IdentityProviderSignInResult> SignInWithPasswordAsync(string email, string password, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync("token?grant_type=password", new { email, password }, ct);

        if (!response.IsSuccessStatusCode)
            return IdentityProviderSignInResult.Failure("Invalid email or password.");

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
        if (payload?.User is null || string.IsNullOrEmpty(payload.User.Id))
            return IdentityProviderSignInResult.Failure("Identity provider returned an unexpected response.");

        return IdentityProviderSignInResult.Success(payload.User.Id, payload.User.Email ?? email);
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("user")]
        public UserPayload? User { get; init; }
    }

    private sealed class UserPayload
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }
    }
}
