using SmartMenuOptim.Domain.Entities.RestaurantEntities;

namespace SmartMenuOptim.Domain.Services.Abstractions;

/// <summary>
/// Domain abstraction for sentiment analysis services.
/// This interface is the PORT for the Sentiment Analysis Service in Hexagonal Architecture.
/// </summary>
/// <remarks>
/// <para><strong>Hexagonal Architecture (Ports & Adapters)</strong></para>
/// 
/// PORTs define the contract for services that can be implemented by various adapters.
/// Implementations of this interface can use Azure Cognitive Services, AWS Comprehend,
/// local ML models, or any other sentiment analysis provider.
/// 
/// <para><strong>Sentiment Scoring:</strong></para>
/// Sentiment scores typically range from 0.0 (most negative) to 1.0 (most positive).
/// Methods are asynchronous to support analysis that may involve external services
/// or computationally intensive operations.
/// </remarks>
public interface ISentimentAnalyzer
{
    /// <summary>
    /// Analyzes the sentiment of the specified text and returns the average sentiment score.
    /// </summary>
    /// <param name="text">The text to analyze for sentiment. Cannot be null or empty.</param>
    /// <returns>
    /// A task containing the average sentiment score as a value between 0.0 (most negative) 
    /// and 1.0 (most positive), or null if the sentiment could not be determined.
    /// </returns>
    Task<double?> AnalyzeAverageSentimentAsync(string text);

    /// <summary>
    /// Analyzes the specified collection of texts and calculates the overall positive sentiment score.
    /// </summary>
    /// <param name="texts">A collection of text strings to analyze for positive sentiment.</param>
    /// <returns>
    /// A task containing a double value between 0.0 and 1.0 indicating the proportion 
    /// of texts with positive sentiment. Returns 0.0 if the collection is empty.
    /// </returns>
    Task<double> AnalyzePositiveSentimentAsync(IEnumerable<string> texts);
}
