using System.Security.Claims;
using SmartMenuOptim.Domain.Entities.GlobalEntities;

namespace SmartMenuOptim.Application.Contracts.Identity;

/// <summary>
/// Gets or JIT-provisions the local <see cref="ApplicationUser"/> shadow record for an authenticated
/// identity-provider principal. Application-layer port over <c>JitUserProvisioningService</c>
/// (Infrastructure), so Application code never references Infrastructure directly.
/// </summary>
/// <remarks>
/// See docs/06-Security/AUTHENTICATION_FRAMEWORK.md §3/§4 and ADR-006.
/// </remarks>
public interface IUserProvisioningService
{
    Task<ApplicationUser> GetOrProvisionAsync(ClaimsPrincipal principal, CancellationToken ct);
}
