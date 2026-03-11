namespace SmartMenuOptim.Domain.Services.Abstractions;

/// <summary>
/// Domain abstraction for AI text generation services.
/// This is a PORT in Hexagonal Architecture.
/// </summary>
/// <remarks>
/// <para><strong>Hexagonal Architecture (Ports & Adapters)</strong></para>
/// 
/// This interface represents the domain's need for AI-powered text generation,
/// independent of any specific implementation (OpenAI, Azure OpenAI, local models, etc.).
/// Implementations (ADAPTERS) reside in the Infrastructure layer.
/// 
/// <para><strong>Use Cases:</strong></para>
/// <list type="bullet">
///   <item><description>Menu description generation</description></item>
///   <item><description>Review response suggestions</description></item>
///   <item><description>Marketing content creation</description></item>
///   <item><description>Menu optimization recommendations</description></item>
/// </list>
/// </remarks>
public interface IAiTextGenerator
{
    /// <summary>
    /// Generates text based on user and system prompts using AI.
    /// </summary>
    /// <param name="userPrompt">The main prompt describing what to generate.</param>
    /// <param name="systemPrompt">Optional system-level instructions for the AI model.</param>
    /// <param name="temperature">Controls randomness (0.0 = deterministic, 1.0 = creative). Default: 0.7</param>
    /// <param name="deploymentName">Optional specific model deployment to use.</param>
    /// <param name="maxTokens">Maximum number of tokens in the response.</param>
    /// <returns>Generated text response.</returns>
    Task<string> GenerateAsync(
        string userPrompt,
        string? systemPrompt = null,
        float? temperature = null,
        string? deploymentName = null,
        int? maxTokens = null);
}
