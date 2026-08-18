using Microsoft.AspNetCore.Components;

namespace SmartMenuOptim.Server.Components;

public sealed partial class RedirectToLogin : ComponentBase
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        var returnUrl = Uri.EscapeDataString(NavigationManager.ToBaseRelativePath(NavigationManager.Uri));
        NavigationManager.NavigateTo($"/auth/login?returnUrl={returnUrl}", forceLoad: true);
    }
}
