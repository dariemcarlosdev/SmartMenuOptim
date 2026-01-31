using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Configuration;
using SmartMenuOptim.Application.Interfaces;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;

namespace SmartMenuOptim.Infrastructure.Infrastructure.Services.Azure
{
    /// <summary>
    /// Provides sentiment analysis services using Azure Text Analytics.
    /// </summary>
    /// <remarks>The SentimentService class enables analysis of text sentiment by leveraging Azure Cognitive
    /// Services. It requires configuration settings for the Azure endpoint and API key. This class is intended to be
    /// used as an implementation of the ISentimentService interface.</remarks>
    public class SentimentService : ISentimentService
    {
        private readonly TextAnalyticsClient _textAnalyticsClient;
        
   
        public SentimentService(IConfiguration config)
        {
            var endpoint = config["Azure:TextAnalytics:Endpoint"];
            var key = config["Azure:TextAnalytics:Key"];
            var credential = new AzureKeyCredential(key);
            _textAnalyticsClient = new TextAnalyticsClient(new Uri(endpoint), credential);
        }


        public async Task<double?> AnalyzeSentimentAsync(string text)
        {
            if(string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be null or empty.", nameof(text));
            }
            var response = await _textAnalyticsClient.AnalyzeSentimentAsync(text);
            if (response.Value == null)
            {
                throw new InvalidOperationException("Sentiment analysis response is null.");
            }
            return response?.Value.ConfidenceScores.Positive;
        }

        //Future implementations for analyzing sentiment of multiple texts, reviews, and sale records

        public Task<double> AnalyzeSentimentAsync(IEnumerable<string> texts)
        {
            throw new NotImplementedException();
        }

        public Task<double> AnalyzeSentimentAsync(IEnumerable<Review> reviews)
        {
            throw new NotImplementedException();
        }

        public Task<double> AnalyzeSentimentAsync(IEnumerable<SaleRecord> saleRecords)
        {
            throw new NotImplementedException();
        }

    }
}
