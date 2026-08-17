using Microsoft.AspNetCore.Components;

namespace SmartMenuOptim.Server.Components.Shared;

/// <summary>
/// Code-behind for the ErrorAlert component.
/// Consistent error-state display supporting dismissible alerts and optional back link.
/// </summary>
public partial class ErrorAlert : ComponentBase
{
    /// <summary>
    /// The error message to display.
    /// </summary>
    [Parameter]
    public string? Message { get; set; }

    /// <summary>
    /// Optional title prefix (e.g., "Error", "Warning").
    /// </summary>
    [Parameter]
    public string? Title { get; set; } = "Error";

    /// <summary>
    /// Alert type: "danger", "warning", "info", "success".
    /// </summary>
    [Parameter]
    public string AlertType { get; set; } = "danger";

    /// <summary>
    /// Whether the alert can be dismissed.
    /// </summary>
    [Parameter]
    public bool IsDismissible { get; set; } = false;

    /// <summary>
    /// Optional URL for back navigation link.
    /// </summary>
    [Parameter]
    public string? BackLinkUrl { get; set; }

    /// <summary>
    /// Text for back navigation link.
    /// </summary>
    [Parameter]
    public string BackLinkText { get; set; } = "Go back";

    /// <summary>
    /// Callback when alert is dismissed.
    /// </summary>
    [Parameter]
    public EventCallback OnDismiss { get; set; }

    private string AlertClass => AlertType switch
    {
        "danger" => "alert-danger",
        "warning" => "alert-warning",
        "info" => "alert-info",
        "success" => "alert-success",
        _ => "alert-danger"
    };

    private string IconClass => AlertType switch
    {
        "danger" => "bi-exclamation-triangle-fill",
        "warning" => "bi-exclamation-circle-fill",
        "info" => "bi-info-circle-fill",
        "success" => "bi-check-circle-fill",
        _ => "bi-exclamation-triangle-fill"
    };
}
