using Microsoft.AspNetCore.Components;

namespace SmartMenuOptim.Server.Components.Shared;

/// <summary>
/// Code-behind for the StatItem component.
/// Reusable key-value stat display used in Quick Stats cards and similar contexts.
/// </summary>
public partial class StatItem : ComponentBase
{
    /// <summary>
    /// The label/key text.
    /// </summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The value to display.
    /// </summary>
    [Parameter, EditorRequired]
    public string? Value { get; set; }

    /// <summary>
    /// Whether to display the value as a badge.
    /// </summary>
    [Parameter]
    public bool UseBadge { get; set; } = false;

    /// <summary>
    /// CSS class for the badge (when UseBadge is true). Empty by default so the
    /// scoped brand-slate badge applies; pass a Bootstrap bg-* class to override.
    /// </summary>
    [Parameter]
    public string BadgeClass { get; set; } = "";

    /// <summary>
    /// CSS class for the value span (when UseBadge is false).
    /// </summary>
    [Parameter]
    public string ValueClass { get; set; } = "";

    /// <summary>
    /// Whether to show border-bottom separator.
    /// </summary>
    [Parameter]
    public bool ShowBorder { get; set; } = true;
}
