namespace SmartMenuOptim.Application.Contracts.Identity;

/// <summary>
/// Authenticates end-user credentials against the external identity provider (initially Supabase Auth)
/// using a password grant. Distinct from <see cref="IIdentityProviderAdminClient"/>, which performs
/// service-role admin operations — this port uses the provider's public anon/publishable key.
/// </summary>
/// <remarks>
/// Interface defined in Application layer (port), implementation in Infrastructure layer (adapter).
/// See docs/06-Security/AUTHENTICATION_FRAMEWORK.md §4 and ADR-006.
/// </remarks>
public interface IIdentityProviderAuthenticator
{
    Task<IdentityProviderSignInResult> SignInWithPasswordAsync(string email, string password, CancellationToken ct);
}

/// <summary>
/// Outcome of a password-grant sign-in attempt against the identity provider.
/// </summary>
public sealed record IdentityProviderSignInResult(bool Succeeded, string? Subject, string? Email, string? Error)
{
    public static IdentityProviderSignInResult Success(string subject, string email) => new(true, subject, email, null);
    public static IdentityProviderSignInResult Failure(string error) => new(false, null, null, error);
}
