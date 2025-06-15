using Microsoft.AspNetCore.Mvc;
using SmartMenuOptim.API.Controllers;
using SmartMenuOptim.Shared.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Tests.UnitTests.Controllers
{
    public class AiControllerTests
    {
        // Each test method should be independent and test a specific scenario.
        // Scenarios include:
        // 1. Valid request with reviews and sale records.
        // 2. Null sale records.
        // 3. Empty sale records.
        // 4. Returning top two dishes based on sales.
        // 5. Handling ties in sales records.


        [Fact]
        public void Recommend_ReturnBadRequest_WhenSaleRecordsIsNull()
        {
            // Arrange: Define the controller and request with null sale records
            var controller = new AiController();
            var request = new AiRecomendationRequest
            {
                Reviews = new List<Review>(),
                SaleRecords = [null] // Simulating null sale records
            };

            // Act: Desire to call the Recommend method
            var result = controller.Recommend(request);

            // Assert: Check if the result is a BadRequestObjectResult

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Sale records cannot be empty.", badRequestResult.Value);
        }

        [Fact]
        public void Recommend_ReturnBadRequest_WhenSaleRecordsIsEmpty()
        {
            // Arrange: Define the controller and request with empty sale records
            var controller = new AiController();
            var request = new AiRecomendationRequest
            {
                Reviews = new List<Review>(),
                SaleRecords = new List<SaleRecord>() // Simulating empty sale records
            };
            // Act: Desire to call the Recommend method
            var result = controller.Recommend(request);
            // Assert: Check if the result is a BadRequestObjectResult
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Sale records cannot be empty.", badRequestResult.Value);
        }

        [Fact]
        public void Recommend_ReturnOk_WhithTopTwoDishes()
        {
            // Arrange: Arrange defines the context for the test.Define the controller and request with valid sale records. 
            var controller = new AiController();
            var request = new AiRecomendationRequest
            {
                Reviews = new List<Review>(),
                SaleRecords = new List<SaleRecord>
                {
                    new SaleRecord { DishName = "Pizza", QuantitySold = 10 },
                    new SaleRecord { DishName = "Burger", QuantitySold = 5 },
                    new SaleRecord { DishName = "Pasta", QuantitySold = 8 }
                }
            };
            // Act: Act define the action to be tested.Desire to call the Recommend method.
            var result = controller.Recommend(request);

            // Assert: Assert checks the outcome of the action.  Check if the result is an OkObjectResult.
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = okResult.Value as AiRecomendationResponse;

            // Verify the recommended dishes and strategy note
            Assert.NotNull(response);
            Assert.Equal(2, response.RecomendedDishes.Count);
            Assert.Contains("Pizza", response.RecomendedDishes);
            Assert.Contains("Pasta", response.RecomendedDishes);
            Assert.Equal("Boost these items with promotions and track review sentiment to refine.", response.StrategyNote);
        }

        [Fact]
        public void Recommend_ReturnOk_WhenTopDishesAreTied()
        {
            // Arrange: Define the controller and request with sale records where two dishes are tied
            var controller = new AiController();
            var request = new AiRecomendationRequest
            {
                Reviews = new List<Review>(),
                SaleRecords = new List<SaleRecord>
                {
                    new SaleRecord { DishName = "Pizza", QuantitySold = 10 },
                    new SaleRecord { DishName = "Burger", QuantitySold = 10 }, // Tied with Pizza
                    new SaleRecord { DishName = "Pasta", QuantitySold = 8 }
                }
            };
            // Act: Act defines the action to be tested. Desire to call the Recommend method.
            var result = controller.Recommend(request);

            // Assert: Assert checks the outcome of the action. Check if the result is an OkObjectResult.
            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = okResult.Value as AiRecomendationResponse;

            // Verify the recommended dishes and strategy note
            Assert.NotNull(response);
            Assert.Equal(2, response.RecomendedDishes.Count);
            Assert.Contains("Pizza", response.RecomendedDishes);
            Assert.Contains("Burger", response.RecomendedDishes); // Both Pizza and Burger should be included
        }
    }
}
