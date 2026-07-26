# Issue#: 007_AI-SUGGESTION-MODAL-MARKDOWN-FORMAT__UI

| Field | Value |
|-------|-------|
| **Date** | 2026-07-17 |
| **Severity** | 🟡 Medium |
| **Status** | ✅ Resolved |
| **Layer** | UI (Blazor Server) |
| **Feature** | AI Menu Insights — Underperforming Dishes |

**Description**: On the Underperforming Dishes view, clicking the 💡 **Improve** button on a dish opens the AI Suggestion modal. The AI improvement strategy was displayed as **raw markdown** inside a monospace gray terminal-style box. Markdown syntax (`**bold**`, numbered lists, headings) rendered as literal characters, and during the "generating" reveal the raw markdown characters flashed on screen — unreadable and unpolished.

**Root Cause**: `AiImprovementService.GetImprovementStrategyAsync` returns markdown (numbered list of 3–5 actionable improvements, `**bold**`, headings). The `AiSuggestionModal` rendered that text verbatim through a char-by-char typing animation in a `.typing-effect` monospace box — no markdown parsing, so all syntax showed literal.

**Resolution**:

1. Render the AI markdown to HTML with **Markdig** (already referenced, v0.41.3) and display it via `MarkupString` inside a styled `.ai-suggestion-content` card.
2. Markdig pipeline uses `.DisableHtml()` — strips any raw HTML from AI output to prevent `MarkupString` injection (OWASP A03).
3. Dropped the char-by-char typing animation; the formatted card now fades in directly (`ai-fade-in` keyframe), eliminating the raw-markdown flash during streaming.
4. Restyled the suggestion card: system font, 1.65 line-height, white background, amber (`#f59e0b`) left accent matching the landing design system; amber `li::marker`, styled headings/lists/`strong`/`code`.

**Markdown pipeline** (safe render):

```csharp
private static readonly MarkdownPipeline _pipeline =
    new MarkdownPipelineBuilder().DisableHtml().UseAdvancedExtensions().Build();

protected override void OnParametersSet()
{
    TypingText ??= string.Empty;
    _renderedHtml = Typing && !string.IsNullOrEmpty(TypingText)
        ? (MarkupString)Markdown.ToHtml(TypingText, _pipeline)
        : default;
}
```

**References**:

| File | Change Reason |
|------|---------------|
| `SmartMenuOptim.Server/Features/AI/Components/AiSuggestionModal.razor` | Added `@using Markdig`; render `_renderedHtml` (MarkupString) in `.ai-suggestion-content` instead of raw typing text; removed char-by-char typing loop and `_typingDone`/`CurrentText` state; `OnParametersSet` computes rendered HTML via `.DisableHtml()` pipeline |
| `SmartMenuOptim.Server/wwwroot/css/AiSuggestionModal.css` | Added `.ai-suggestion-content` prose styles + `ai-fade-in` keyframe (amber accent, headings/lists/strong/code) |
| `SmartMenuOptim.Server/Features/AI/Components/Underperforming.razor` | Caller — unchanged; still passes `TypingText`/`Typing` to modal |

**Notes**:

- `TypingDelay` parameter retained for caller compatibility but no longer used.
- `.typing-effect` / `.caret` CSS now dead (render branch removed) — left in place, not deleted.
- Pattern reusable: any `MarkupString` render of AI/user-derived text should use the `.DisableHtml()` Markdig pipeline.
- Component logic lives in the `.razor` `@code` block (violates project code-behind rule); `AiSuggestionModal.razor.cs` holds a stray unused `class AiSuggestionModal` in the wrong namespace — pre-existing, left as-is.
