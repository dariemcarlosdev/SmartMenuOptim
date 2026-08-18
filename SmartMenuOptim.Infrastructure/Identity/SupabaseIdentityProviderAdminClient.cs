using System.Net.Http.Json;
using SmartMenuOptim.Application.Contracts.Identity;

namespace SmartMenuOptim.Infrastructure.Identity;

/// <summary>
/// Supabase Auth (GoTrue) implementation of <see cref="IIdentityProviderAdminClient"/>.
/// </summary>
/// <remarks>
/// Calls the GoTrue Admin API (https://{project}.supabase.co/auth/v1/admin/...), authenticated with the
/// project's secret API key (<c>sb_secret_...</c>, formerly "service_role"). The <see cref="HttpClient"/> is registered as a typed client in
/// <c>InfrastructureServiceCollectionExtensions.AddIdentityProviderAdminClient</c> with
/// <c>BaseAddress</c> and <c>Authorization</c> header sourced from config (user-secrets/env vars, never
/// <c>appsettings.json</c>). See docs/06-Security/AUTHENTICATION_FRAMEWORK.md §1.
/// </remarks>
internal sealed class SupabaseIdentityProviderAdminClient(HttpClient httpClient) : IIdentityProviderAdminClient
{
    public async Task ForceLogoutAsync(string userId, CancellationToken ct)
    {
        using var response = await httpClient.PostAsync($"admin/users/{userId}/logout", content: null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DisableUserAsync(string userId, CancellationToken ct)
    {
        // ban_duration "876000h" (~100 years) is GoTrue's documented way to disable a user indefinitely;
        // there is no separate "disabled" flag on the admin user resource.
        using var response = await httpClient.PutAsJsonAsync($"admin/users/{userId}", new { ban_duration = "876000h" }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task TriggerPasswordResetAsync(string email, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync("admin/generate_link", new { type = "recovery", email }, ct);
        response.EnsureSuccessStatusCode();
    }
}
