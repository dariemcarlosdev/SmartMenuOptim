using Microsoft.AspNetCore.Components;

namespace SmartMenuOptim.Server.Components.Shared;

/// <summary>
/// Code-behind for the NotFoundAlert component.
/// Consistent "resource not found" display with optional back navigation.
/// </summary>
public partial class NotFoundAlert : ComponentBase
{
    /// <summary>
    /// The not found message to display.
    /// </summary>
    [Parameter]
    public string Message { get; set; } = "Resource not found.";

    /// <summary>
    /// URL for back navigation link.
    /// </summary>
    [Parameter]
    public string? BackLinkUrl { get; set; }

    /// <summary>
    /// Text for back navigation link.
    /// </summary>
    [Parameter]
    public string BackLinkText { get; set; } = "Go back";
}
