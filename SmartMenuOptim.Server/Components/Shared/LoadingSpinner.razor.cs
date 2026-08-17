using Microsoft.AspNetCore.Components;

namespace SmartMenuOptim.Server.Components.Shared;

/// <summary>
/// Code-behind for the LoadingSpinner component.
/// Provides a consistent loading-state indicator across the application.
/// </summary>
public partial class LoadingSpinner : ComponentBase
{
    /// <summary>
    /// Controls whether the spinner is displayed.
    /// </summary>
    [Parameter]
    public bool IsLoading { get; set; } = true;

    /// <summary>
    /// Accessibility message for screen readers.
    /// </summary>
    [Parameter]
    public string Message { get; set; } = "Loading...";

    /// <summary>
    /// CSS class for the spinner element. Empty by default so the scoped
    /// brand-amber tint applies; pass a Bootstrap text-* class to override.
    /// </summary>
    [Parameter]
    public string SpinnerClass { get; set; } = "";

    /// <summary>
    /// CSS class for the container div.
    /// </summary>
    [Parameter]
    public string ContainerClass { get; set; } = "py-5";
}
