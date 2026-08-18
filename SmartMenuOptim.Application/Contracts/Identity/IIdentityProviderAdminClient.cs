namespace SmartMenuOptim.Application.Contracts.Identity;

/// <summary>
/// Defines admin-level operations against the external identity provider (initially Supabase Auth).
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture:</strong></para>
/// <para>Interface defined in Application layer (port), implementation in Infrastructure layer (adapter).
/// Swapping identity providers means a new adapter only — no change here or in any caller.
/// See docs/06-Security/AUTHENTICATION_FRAMEWORK.md §1 and ADR-006.</para>
/// </remarks>
public interface IIdentityProviderAdminClient
{
    Task ForceLogoutAsync(string userId, CancellationToken ct);
    Task DisableUserAsync(string userId, CancellationToken ct);
    Task TriggerPasswordResetAsync(string email, CancellationToken ct);
}
