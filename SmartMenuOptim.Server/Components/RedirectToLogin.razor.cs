using Microsoft.AspNetCore.Components;

namespace SmartMenuOptim.Server.Components;

public sealed partial class RedirectToLogin : ComponentBase
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        var relativePath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

        // Already on the login page (or already carrying a returnUrl to it) — navigating again would
        // re-encode this URL into its own returnUrl each pass, nesting forever. Stop here instead.
        if (relativePath.StartsWith("auth/login", StringComparison.OrdinalIgnoreCase))
            return;

        var returnUrl = Uri.EscapeDataString(relativePath);
        NavigationManager.NavigateTo($"/auth/login?returnUrl={returnUrl}", forceLoad: true);
    }
}
