using SmartMenuOptim.Shared.Data.Entities;

namespace SmartMenuOptim.API.Services.Interfaces
{
    public interface ISentimentService
    {
        /// <summary>
        /// Analyzes the sentiment of a given text and returns a score.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        Task<double?> AnalyzeSentimentAsync(string text);

        //--------Functions for analyzing sentiment of multiple texts, reviews, and sale records--------//

        /// <summary>
        /// Analyzes the sentiment of multiple texts and returns an average score.
        /// </summary>
        /// <param name="texts"></param>
        /// <returns></returns>
        Task<double> AnalyzeSentimentAsync(IEnumerable<string> texts);

        /// <summary>
        /// Analyzes the sentiment of a collection of reviews and returns an average score.
        /// </summary>
        /// <param name="reviews"></param>
        /// <returns></returns>
        Task<double> AnalyzeSentimentAsync(IEnumerable<Review> reviews);

        /// <summary>
        /// Analyzes the sentiment of a collection of sale records and returns an average score.
        /// </summary>
        /// <param name="saleRecords"></param>
        /// <returns></returns>
        Task<double> AnalyzeSentimentAsync(IEnumerable<SaleRecord> saleRecords);
    }
}