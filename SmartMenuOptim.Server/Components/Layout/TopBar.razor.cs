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

    // Maps known URL paths to their corresponding topbar titles
    private void SetPageTitle(string uri)
    {
        var path = new Uri(uri).AbsolutePath.ToLowerInvariant();
        if (path.Contains("/dashboard"))
            CurrentPageTitle = "Dashboard";
        else if (path.Contains("/insight"))
            CurrentPageTitle = "Insight";
        else if (path.Contains("/reviews"))
            CurrentPageTitle = "Reviews";
        else if (path.Contains("/settings"))
            CurrentPageTitle = "Settings";
        else if (path.Contains("/home"))
            CurrentPageTitle = "Home";
        else if (path.Contains("/underperforming"))
            CurrentPageTitle = "Performance";
        else
            CurrentPageTitle = "";
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
