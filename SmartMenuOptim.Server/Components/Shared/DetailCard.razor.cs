using Microsoft.AspNetCore.Components;

namespace SmartMenuOptim.Server.Components.Shared;

/// <summary>
/// Code-behind for the DetailCard component.
/// Provides a consistent card layout with optional header icon, actions, and footer.
/// </summary>
public partial class DetailCard : ComponentBase
{
    /// <summary>
    /// The title text displayed in the card header.
    /// </summary>
    [Parameter]
    public string? HeaderTitle { get; set; }

    /// <summary>
    /// Bootstrap icon class for the header (e.g., "bi-telephone").
    /// </summary>
    [Parameter]
    public string? HeaderIcon { get; set; }

    /// <summary>
    /// Optional custom header template (overrides HeaderTitle and HeaderIcon).
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Optional header action buttons/links.
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderActions { get; set; }

    /// <summary>
    /// The main content of the card body.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Optional footer content.
    /// </summary>
    [Parameter]
    public RenderFragment? FooterContent { get; set; }

    /// <summary>
    /// Additional CSS classes for the card element.
    /// </summary>
    [Parameter]
    public string CardClass { get; set; } = "mb-4";

    /// <summary>
    /// Additional CSS classes for the card header.
    /// </summary>
    [Parameter]
    public string HeaderClass { get; set; } = "";

    /// <summary>
    /// Additional CSS classes for the card body.
    /// </summary>
    [Parameter]
    public string BodyClass { get; set; } = "";

    /// <summary>
    /// Additional CSS classes for the card footer.
    /// </summary>
    [Parameter]
    public string FooterClass { get; set; } = "";
}
