using Markdig;
using Microsoft.AspNetCore.Components;

namespace SmartMenuOptim.Server.Features.AI.Components;

/// <summary>
/// Code-behind for the AiSuggestionModal component.
/// Reusable Bootstrap modal that renders AI-generated markdown safely.
/// </summary>
public partial class AiSuggestionModal : ComponentBase
{
    // State for the modal parameters
    // Parameters are used to pass data to the modal component from the parent component
    [Parameter] public bool Show { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; } // Content to be displayed inside the modal body
    [Parameter] public EventCallback OnClose { get; set; } // Callback to close the modal
    [Parameter] public string TypingText { get; set; } = string.Empty;
    [Parameter] public bool Typing { get; set; } = false;
    [Parameter] public int TypingDelay { get; set; } = 25; // retained for caller compatibility; no longer used

    private MarkupString _renderedHtml;

    // Markdown pipeline. DisableHtml() strips raw HTML from AI output (OWASP A03 — no MarkupString injection).
    private static readonly MarkdownPipeline _pipeline =
        new MarkdownPipelineBuilder().DisableHtml().UseAdvancedExtensions().Build();

    protected override void OnParametersSet()
    {
        TypingText ??= string.Empty;
        _renderedHtml = Typing && !string.IsNullOrEmpty(TypingText)
            ? (MarkupString)Markdown.ToHtml(TypingText, _pipeline)
            : default;
    }
}
