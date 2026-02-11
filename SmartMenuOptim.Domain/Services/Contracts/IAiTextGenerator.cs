namespace SmartMenuOptim.Domain.Services.Abstraction;

/// <summary>
/// Domain abstraction for AI text generation services.
/// This is a PORT in Hexagonal Architecture.
/// </summary>
/// <remarks>
/// This interface represents the domain's need for AI-powered text generation,
/// independent of any specific implementation (OpenAI, Azure, local models, etc.).
/// Implementations reside in the Infrastructure layer.
/// </remarks>
public interface IAiTextGenerator
{
    /// <summary>
    /// Generates text based on user and system prompts using AI.
    /// </summary>
    /// <param name="userPrompt">The main prompt describing what to generate.</param>
    /// <param name="systemPrompt">Optional system-level instructions for the AI model.</param>
    /// <param name="temperature">Controls randomness (0.0 = deterministic, 1.0 = creative). Default: 0.7</param>
    /// <param name="maxTokens">Maximum number of tokens in the response. Default: implementation-specific</param>
    /// <returns>Generated text response</returns>
    Task<string> GenerateAsync(
        string userPrompt, 
        string? systemPrompt = null, 
        float? temperature = null,
        string? deploymentName = null,
        int? maxTokens = null);
}
