using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SmartMenuOptim.Domain.Entities.GlobalEntities;

namespace SmartMenuOptim.Infrastructure.Identity;

/// <summary>
/// Just-in-time provisions a local <see cref="ApplicationUser"/> shadow record for a first-seen
/// identity-provider <c>sub</c> claim. No local password is ever created — <see cref="ApplicationUser.Id"/>
/// *is* the IdP's <c>sub</c>. See docs/06-Security/AUTHENTICATION_FRAMEWORK.md §3 and ADR-006.
/// </summary>
public sealed class JitUserProvisioningService(UserManager<ApplicationUser> userManager)
{
    public async Task<ApplicationUser> GetOrProvisionAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var subjectId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Token missing 'sub' claim.");

        var user = await userManager.FindByIdAsync(subjectId);
        if (user is not null)
            return user;

        var email = principal.FindFirstValue(ClaimTypes.Email);

        user = new ApplicationUser
        {
            Id = subjectId,
            Email = email,
            UserName = email,
            ProfileType = ProfileType.None, // no permissions until an admin assigns a profile/tenant
            RestaurantTenantId = null,
        };

        var result = await userManager.CreateAsync(user); // no password — external credential owner
        if (!result.Succeeded)
            throw new InvalidOperationException($"JIT user provisioning failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        return user;
    }
}
