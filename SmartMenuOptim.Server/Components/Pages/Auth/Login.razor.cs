using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Server.Features.Auth.Services;

namespace SmartMenuOptim.Server.Components.Pages.Auth;

/// <summary>
/// Statically rendered (no interactive render mode) so <see cref="HttpContext"/> is available via the
/// framework's cascading parameter and <c>HttpContext.SignInAsync</c> can write the auth cookie before
/// the response starts — this doesn't work over an already-established interactive Server circuit.
/// See docs/06-Security/AUTHENTICATION_FRAMEWORK.md §4.
/// </summary>
public sealed partial class Login : ComponentBase
{
    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;
    [Inject] private IAuthClientService AuthClientService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [SupplyParameterFromForm] private InputModel Input { get; set; } = new();
    [SupplyParameterFromQuery] private string? ReturnUrl { get; set; }

    private string? ErrorMessage;
    private bool IsSubmitting;

    private async Task HandleLoginAsync()
    {
        IsSubmitting = true;

        var result = await AuthClientService.SignInAsync(Input.Email, Input.Password);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            IsSubmitting = false;
            return;
        }

        var user = result.Value;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId),
            new(ClaimTypes.Email, user.Email),
            new("ProfileType", user.ProfileType.ToString()),
        };
        if (user.RestaurantTenantId.HasValue)
            claims.Add(new Claim("RestaurantTenantId", user.RestaurantTenantId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        NavigationManager.NavigateTo(GetSafeReturnUrl(), forceLoad: true);
    }

    // ReturnUrl is attacker-controlled (query string) — only allow a local, relative path.
    // Rejects absolute/scheme-qualified URLs and protocol-relative ("//evil.com") to prevent open redirect.
    private string GetSafeReturnUrl()
    {
        if (string.IsNullOrEmpty(ReturnUrl))
            return "/";

        if (!Uri.IsWellFormedUriString(ReturnUrl, UriKind.Relative))
            return "/";

        if (!ReturnUrl.StartsWith('/') || ReturnUrl.StartsWith("//") || ReturnUrl.StartsWith("/\\"))
            return "/";

        return ReturnUrl;
    }

    private sealed class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }
}
