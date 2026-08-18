using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using SmartMenuOptim.Application.Contracts.Identity;

namespace SmartMenuOptim.Infrastructure.Identity;

/// <summary>
/// Runs on every authenticated request (after JWT bearer validation) and JIT-provisions the local
/// <see cref="Domain.Entities.GlobalEntities.ApplicationUser"/> shadow record on first-seen IdP <c>sub</c> claim.
/// See docs/06-Security/AUTHENTICATION_FRAMEWORK.md §3 and ADR-006.
/// </summary>
public sealed class JitClaimsTransformation(IUserProvisioningService jitUserProvisioningService) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        await jitUserProvisioningService.GetOrProvisionAsync(principal, CancellationToken.None);
        return principal;
    }
}
