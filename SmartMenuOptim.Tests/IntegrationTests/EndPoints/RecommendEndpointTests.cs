using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
//using Microsoft.VisualStudio.TestPlatform.TestHost;
using SmartMenuOptim.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
//using SmartMenuOptim.Server; // <-- Add this using statement
using SmartMenuOptim.API;

namespace SmartMenuOptim.Tests.IntegrationTests.EndPoints
{
    public class RecommendEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        public RecommendEndpointTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostRecommend_WithValidRequest_Returns200Ok()
        {
            // Arrange: Define a valid request with sale records. Arrange is used to set up the test environment, including any necessary data or configurations.
            var request = new AiRecomendationRequest
            {
                SaleRecords = new List<SaleRecord>
                {
                    new SaleRecord { DishName = "Pizza", QuantitySold = 10, SaleDate = DateTime.UtcNow },
                    new SaleRecord { DishName = "Pasta", QuantitySold = 5, SaleDate = DateTime.UtcNow },
                    new SaleRecord { DishName = "Salad", QuantitySold = 2, SaleDate = DateTime.UtcNow },
                    new SaleRecord { DishName = "Pizza", QuantitySold = 15, SaleDate = DateTime.UtcNow }
                },
                Reviews = new List<Review>
                {
                    new Review { CustomerName="John", Comment = "Delicious pizza!", SentimentScore =  0.95}
                }
            };


            // Act: Act is the step where the actual operation is performed, such as sending a request to an API or calling a method.
            var response = await _client.PostAsJsonAsync("/api/ai/recommend", request);

            // Assert: Assert is the step where the results of the operation are verified against expected outcomes.
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<object>();// here we use dynamic to avoid creating a specific response model for this test
            content.Should().NotBeNull();
        }
    }
}
