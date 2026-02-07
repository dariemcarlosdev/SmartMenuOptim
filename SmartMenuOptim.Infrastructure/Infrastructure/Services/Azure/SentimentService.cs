using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Domain.Services.Abstraction;

namespace SmartMenuOptim.Infrastructure.Infrastructure.Services.Azure
{
    /// <summary>
    /// Provides sentiment analysis services using an external Azure Text Analytics service.
    /// This is the ADAPTER in Hexagonal Architecture. ADAPTERs implement PORTs defined in the domain layer. This separation allows the core domain logic to remain independent of external services and technologies.
    /// </summary>
    /// <remarks>The SentimentService analyzes the sentiment of text input by leveraging Azure Text Analytics.
    /// It offers methods to evaluate the positive sentiment of individual texts or collections of texts asynchronously.
    /// This service is intended for applications that require automated sentiment scoring, such as feedback analysis or
    /// content moderation. Thread safety is ensured for concurrent calls to the service methods.</remarks>
    public class SentimentService : ISentimentAnalyzer
    {
        private readonly TextAnalyticsClient _azureAiTextAnalyticsClient;
        private readonly ILogger<SentimentService> _logger;


        public SentimentService(
            IConfiguration config,
            ILogger<SentimentService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var endpoint = config["Azure:TextAnalytics:Endpoint"]
                ?? throw new ArgumentNullException("Azure Text Analytics endpoint is not configured.");
            var key = config["Azure:TextAnalytics:Key"]
                ?? throw new ArgumentNullException("Azure Text Analytics key is not configured.");
            
            var credential = new AzureKeyCredential(key);
            
            _azureAiTextAnalyticsClient = new TextAnalyticsClient(new Uri(endpoint), credential);
        }

        /// <summary>
        /// Analyzes the sentiment of the specified text asynchronously and returns the average positive sentiment
        /// score.
        /// </summary>
        /// <remarks>This method uses an external text analytics service to determine the sentiment of the
        /// input text. If the sentiment analysis fails or the input is invalid, the method returns null. The returned
        /// score indicates the confidence that the text expresses a positive sentiment, where 1.0 represents maximum
        /// confidence.</remarks>
        /// <param name="text">The text to analyze for sentiment. Cannot be null, empty, or consist only of white-space characters.</param>
        /// <returns>A double value between 0.0 and 1.0 representing the average positive sentiment score, or null if the
        /// analysis could not be completed.</returns>
        public async Task<double?> AnalyzeAverageSentimentAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("AnalyzeSentimentAsync called with null or empty text.");
                return null;
            }
            try
            {
                var response = await _azureAiTextAnalyticsClient.AnalyzeSentimentAsync(text);
                if (response.Value == null)
                {
                    _logger.LogError("Sentiment analysis response is null for text: {Text}", text);
                    return null;
                }
                return response?.Value.ConfidenceScores.Positive;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Text Analytics request failed for text: {Text}",
                    text.Substring(0, Math.Min(50, text.Length))); // Log first 50 chars
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during sentiment analysis for text: {Text}",
                    text.Substring(0, Math.Min(50, text.Length))); // Log first 50 chars
                return null;

            }
        }

        

        public async Task<double> AnalyzePositiveSentimentAsync(IEnumerable<string> texts)
        {
            if (texts == null || !texts.Any())
            {
                _logger.LogWarning("AnalyzeAverageSentimentAsync called with null or empty texts collection.");
                return 0.0;
            }

            var textList = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (!textList.Any())
            {
                _logger.LogWarning("No valid texts to analyze in AnalyzeAverageSentimentAsync.");
                return 0.0;
            }
            try
            {
                // Azure Text Analytics supports batch processing (up to 10 documents)
                var batchSize = 10;
                var scores = new List<double>();

                for (int i = 0; i < textList.Count; i += batchSize)
                {
                    var batch = textList.Skip(i).Take(batchSize).ToList();
                    var response = await _azureAiTextAnalyticsClient.AnalyzeSentimentBatchAsync(batch);

                    foreach (var result in response.Value)
                    {
                        if (!result.HasError && result.DocumentSentiment != null)
                        {
                            scores.Add(result.DocumentSentiment.ConfidenceScores.Positive);
                        }
                    }
                }

                return scores.Any() ? scores.Average() : 0.0;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing sentiment for {Count} texts", textList.Count);
                return 0.0;
            }
        }




    }
}
