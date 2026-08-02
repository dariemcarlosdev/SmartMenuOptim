using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SmartMenuOptim.Server.Components.Layout;

/// <summary>
/// Code-behind for the NavMenu sidebar component.
/// </summary>
/// <remarks>
/// JS interop in this component is used for:
/// <list type="bullet">
///   <item>Responsive UI: detects window width to adapt sidebar behavior for mobile/desktop.</item>
///   <item>Event bridging: adds/removes outside-click listeners in JS to close the sidebar when clicking outside.</item>
///   <item>.NET/JS communication: allows JS to invoke C# methods (e.g., <see cref="OnOutsideSidebarClick"/>).</item>
/// </list>
/// Enables a mobile-friendly sidebar that closes when the user clicks outside,
/// adapting its behavior based on the browser window size.
/// </remarks>
public partial class NavMenu : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool sidebarOpen = false;
    private DotNetObjectReference<NavMenu>? dotNetRef;
    private bool _disposed = false;

    /// <summary>
    /// Closes the sidebar if the screen width is less than 992px (Bootstrap's mobile breakpoint).
    /// Uses JS interop to get the current window width.
    /// Called when a sidebar menu item is clicked.
    /// </summary>
    private async Task CloseSidebarOnMobileAsync()
    {
        if (_disposed) return;
        var width = await JS.InvokeAsync<int>("getWindowWidth"); // JS interop: gets window width
        if (width < 992)
        {
            sidebarOpen = false;
            StateHasChanged();
            await RemoveOutsideClickListenerAsync(); // JS interop: removes outside click listener
        }
    }

    /// <summary>
    /// Called by JS when user clicks outside the sidebar.
    /// JS interop: Invoked from site.js via DotNet.invokeMethodAsync.
    /// </summary>
    [JSInvokable]
    public async Task OnOutsideSidebarClick()
    {
        if (_disposed) return;
        await CloseSidebarOnMobileAsync();
    }

    /// <summary>
    /// Adds JS listener for outside clicks when sidebar opens on mobile.
    /// Uses JS interop to register the event handler in site.js.
    /// </summary>
    private async Task OpenSidebarAsync()
    {
        if (_disposed) return;
        sidebarOpen = true;
        var width = await JS.InvokeAsync<int>("getWindowWidth"); // JS interop: gets window width
        if (width < 992)
        {
            dotNetRef ??= DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("sidebarInterop.addOutsideClickListener", dotNetRef, ".sidebar.sidebar-open"); // JS interop: adds outside click listener
        }
    }

    /// <summary>
    /// Removes JS listener for outside clicks when sidebar closes.
    /// Uses JS interop to unregister the event handler in site.js.
    /// </summary>
    private async Task RemoveOutsideClickListenerAsync()
    {
        if (_disposed) return;
        await JS.InvokeVoidAsync("sidebarInterop.removeOutsideClickListener"); // JS interop: removes outside click listener
    }

    /// <summary>
    /// Toggles sidebar (hamburger button).
    /// </summary>
    private async Task ToggleSidebarAsync()
    {
        if (_disposed) return;
        if (!sidebarOpen)
            await OpenSidebarAsync();
        else
        {
            sidebarOpen = false;
            await RemoveOutsideClickListenerAsync();
        }
    }

    /// <summary>
    /// Clean up JS listeners when component is disposed.
    /// Ensures no JS interop calls are made after disposal.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            await RemoveOutsideClickListenerAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit is disconnected, safe to ignore
        }
        dotNetRef?.Dispose();
    }
}
