using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace SmartMenuOptim.Server.Features.Auth.Services;

/// <summary>
/// Server-side <see cref="AuthenticationStateProvider"/> for the interactive Blazor circuit.
/// </summary>
/// <remarks>
/// Inherits <see cref="RevalidatingServerAuthenticationStateProvider"/>, which implements
/// <c>IHostEnvironmentAuthenticationStateProvider</c>. The Blazor Server circuit host therefore
/// injects the cookie <c>ClaimsPrincipal</c> — captured from the HTTP request that established the
/// circuit — into this provider at circuit start, and it stays available inside the WebSocket
/// circuit where there is <b>no</b> <c>HttpContext</c>. This is why the previous
/// <c>IHttpContextAccessor</c>-based implementation was wrong: <c>HttpContext</c> is null in the
/// interactive circuit, so an authenticated user was seen as anonymous and any <c>[Authorize]</c>
/// page would have bounced them to /auth/login.
///
/// Note: this provider is NOT what fixed BUG-012 — that was a missing
/// <c>@using Microsoft.AspNetCore.Components.Authorization</c> in <c>Components/_Imports.razor</c>
/// (see docs/10-ISSUES-QUICK-FIX/BUG-012). A registered provider is nonetheless required, because
/// <c>AuthorizeRouteView</c> throws without a resolvable <c>Task&lt;AuthenticationState&gt;</c>.
/// </remarks>
internal sealed class ServerAuthenticationStateProvider(ILoggerFactory loggerFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    // The local session cookie already bounds validity (8h absolute + sliding, see
    // AddCookieAuthentication). The Blazor Server project has no direct DB access — it calls the API
    // over HTTP — so there is no local security-stamp/user record to re-check here. Revalidation
    // therefore affirms the state; tighten to an API round-trip if per-interval revocation is needed.
    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
        => Task.FromResult(true);
}
