using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SmartMenuOptim.Server.Components.Pages.Home;

/// <summary>
/// Landing page code-behind. Wires the zero-dependency motion module
/// (scroll reveals, pointer tilt, count-up) after first render.
/// </summary>
public sealed partial class Home : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference Root;
    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/home-motion.js");
        await _module.InvokeVoidAsync("init", Root);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("dispose");
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // Circuit already gone — nothing to clean up.
        }
        catch (OperationCanceledException)
        {
            // Render/dispose race — safe to ignore.
        }
    }
}
