using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace SmartMenuOptim.Server.Components.Layout;

/// <summary>
/// Code-behind for the TopBar component.
/// The topbar title updates dynamically based on the current route by tracking
/// navigation changes via <see cref="NavigationManager"/>. <see cref="SetPageTitle"/>
/// maps known URL paths to their corresponding titles.
/// </summary>
public partial class TopBar : ComponentBase, IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    // Simulated logged-in user name
    private string LoggedInUserName { get; set; } = "Dariem C.";

    // Holds the current page title for the topbar, updated on navigation
    private string CurrentPageTitle { get; set; } = "Dashboard";

    // On component initialization, set the initial page title and subscribe to navigation changes
    protected override void OnInitialized()
    {
        SetPageTitle(NavigationManager.Uri);
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    // Event handler for navigation changes; updates the page title
    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        SetPageTitle(e.Location);
        InvokeAsync(StateHasChanged);
    }

    // Maps the first URL segment to its topbar title. Titles mirror the sidebar
    // labels so nav, header, and page title stay consistent. Unknown routes fall
    // back to the app name rather than a blank title.
    private void SetPageTitle(string uri)
    {
        var path = new Uri(uri).AbsolutePath.Trim('/').ToLowerInvariant();
        var segment = path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        CurrentPageTitle = segment switch
        {
            "" => "Home",
            "dashboard" => "Dashboard",
            "restaurants" => "Restaurants",
            "orders" => "Orders",
            "insights" => "Insights",
            "reviews" => "Customer Reviews",
            "underperforming" => "Underperforming",
            "settings" => "Settings",
            "my-plan" => "My Plan",
            "upgrade" => "Upgrade Plan",
            _ => "Smart Menu"
        };
    }

    private void LogOut()
    {
        // Implement logout logic here
        Console.WriteLine("User logged out");
    }

    // Unsubscribe from navigation events when the component is disposed
    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
