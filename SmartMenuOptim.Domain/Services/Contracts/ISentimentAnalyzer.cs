using SmartMenuOptim.Domain.Entities.RestaurantEntities;

namespace SmartMenuOptim.Domain.Services.Abstraction
{
    /// <summary>
    /// Domain abstraction for sentiment analysis services.Defines methods for analyzing the sentiment of text data, providing sentiment scores and metrics for individual exts or collections of texts.
    /// This interface is the PORT for the Sentiment Analysis Service in the application architecture.PORTs define the contract for services that can be implemented by various adapters.
    /// </summary>
    /// <remarks>Implementations of this interface can be used to assess the sentiment of user input, reviews,
    /// or other textual data. Sentiment scores typically range from 0.0 (most negative) to 1.0 (most positive). Methods
    /// are asynchronous to support analysis that may involve external services or computationally intensive
    /// operations.</remarks>
    public interface ISentimentAnalyzer
    {
        
        /// <summary>
        /// Analyzes the sentiment of the specified text and returns the average sentiment score as an asynchronous
        /// operation.
        /// </summary>
        /// <param name="text">The text to analyze for sentiment. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the average sentiment score as a
        /// value between 0.0 (most negative) and 1.0 (most positive), or null if the sentiment could not be determined.</returns>
        Task<double?> AnalyzeAverageSentimentAsync(string text);

        //--------Functions for analyzing sentiment of multiple texts, reviews, and sale records--------//

        /// <summary>
        /// Asynchronously analyzes the specified collection of texts and calculates the overall positive sentiment
        /// score.
        /// </summary>
        /// <param name="texts">A collection of text strings to analyze for positive sentiment. Each string represents a separate text or
        /// review to be evaluated.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a double value between 0.0 and
        /// 1.0 indicating the proportion of texts with positive sentiment. Returns 0.0 if the collection is empty or no
        /// texts are positive.</returns>
        Task<double> AnalyzePositiveSentimentAsync(IEnumerable<string> texts);

        //-----Additional methods for analyzing sentiment of reviews and sales records can be defined here-----//

        // E.g., AnalyzeSentimentAsync for Review entities, SalesRecord entities, etc.

        // Task<double> AnalyzePositiveSentimentAsync(IEnumerable<Review> reviews);

        // Task<double> AnalyzePositiveSentimentAsync(IEnumerable<SalesRecord> salesRecords);

        // ------------------------------------------------------------------------------------------------//

    }
}