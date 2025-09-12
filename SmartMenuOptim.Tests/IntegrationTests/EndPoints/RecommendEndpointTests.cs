using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
//using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
//using SmartMenuOptim.Server; // <-- Add this using statement
using SmartMenuOptim.API;
using SmartMenuOptim.Shared.Data.Entities;
using SmartMenuOptim.Shared.Data.Dtos;

namespace SmartMenuOptim.Tests.IntegrationTests.EndPoints
{
    // This class is responsible for testing the AI recommendation endpoint in the SmartMenuOptim API. Integration tests are used to verify that the endpoint behaves as expected when given valid input data.
    // Integration tests are crucial for ensuring that the API endpoints work correctly with the underlying data layer and business logic.
    // Integration tests typically involve making HTTP requests to the API and verifying the responses, ensuring that the entire stack (from the controller to the database) works as intended.
    // It differs from unit tests, which focus on testing individual components in isolation without external dependencies.
    
    public class RecommendEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        public RecommendEndpointTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }
        /// PostRecommend_WithValidRequest_Returns200Ok is a test method that verifies the behavior of the AI recommendation endpoint when provided with a valid request.
        [Fact]
        public async Task PostRecommend_WithValidRequest_Returns200Ok()
        {
            // Arrange: Define a valid request with sale records. Arrange is used to set up the test environment, including any necessary data or configurations.
            var request = new AiRecomendationRequestDTO
            {
                SaleRecords = new List<SaleRecordDTO>
                {
                    new SaleRecordDTO { DishId = 1, QuantitySold = 10, SaleDate = DateTime.UtcNow },
                    new SaleRecordDTO { DishId = 2, QuantitySold = 5, SaleDate = DateTime.UtcNow },
                    new SaleRecordDTO { DishId = 3, QuantitySold = 2, SaleDate = DateTime.UtcNow },
                    new SaleRecordDTO { DishId = 1, QuantitySold = 15, SaleDate = DateTime.UtcNow }
                },
                Reviews = new List<ReviewDTO>
                {
                    new ReviewDTO    { CustomerName="John", Comment = "Delicious pizza!", SentimentScore =  0.95}
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
