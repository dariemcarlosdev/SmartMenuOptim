using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Features.AI.DTOs;

/// <summary>
/// Represents an AI-generated insight or recommendation response from the menu optimization system.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Encapsulates AI/ML-generated recommendations for menu optimization,
/// dish suggestions, pricing strategies, or operational improvements.</para>
/// 
/// <para><strong>Usage Context:</strong></para>
/// <list type="bullet">
///   <item><description>AI/ML service responses for menu analysis</description></item>
///   <item><description>Recommendation engine outputs</description></item>
///   <item><description>Data analytics insights</description></item>
///   <item><description>Dashboard insights and suggestions</description></item>
/// </list>
/// </remarks>
public class InsightResponseDTO
{
    /// <summary>
    /// Confidence score for the AI-generated insight (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    /// Higher scores indicate greater confidence in the recommendation.
    /// Consider presenting recommendations with scores >= 0.7 to users.
    /// </remarks>
    [Range(0.0, 1.0, ErrorMessage = "Confidence score must be between 0.0 and 1.0")]
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// The AI-generated recommendation or insight text.
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>
    /// Category of the insight (e.g., "MenuOptimization", "Pricing", "Staffing").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Priority level of the insight (1 = highest, 5 = lowest).
    /// </summary>
    [Range(1, 5)]
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Timestamp when the insight was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
