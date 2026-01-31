using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartMenuOptim.API.Controllers.v1;
using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Application.Interfaces;
using SmartMenuOptim.Domain.Repositories;

namespace SmartMenuOptim.Tests.UnitTests.Controllers
{
    // This class contains unit tests for the AiController class in the SmartMenuOptim API.
    public class AiControllerTests
    {
        // Each test method should be independent and test a specific scenario.
        // Scenarios include:
        // 1. Valid request with reviews and sale records.
        // 2. Null sale records.
        // 3. Empty sale records.
        // 4. Returning top two dishes based on sales.
        // 5. Handling ties in sales records.

        private readonly IUnityOfWork _mockUnityOfWork;
        private readonly IAImprovementStrategyService _mock; // Mocking the service if needed for further tests

        //mock constructor to initialize the IUnityOfWork
        public AiControllerTests()
        {
            _mockUnityOfWork = new Mock<IUnityOfWork>().Object; // Using Moq to create a mock of IUnityOfWork
            _mock = new Mock<IAImprovementStrategyService>().Object; // Mocking the service if needed for further tests
        }

        [Fact]
        public void Recommend_ReturnBadRequest_WhenSaleRecordsIsNull()
        {
            // Arrange: Define the controller and request with null sale records
            var controller = new AiController(_mockUnityOfWork, _mock);
            var request = new AiRecomendationRequestDTO
            {
                Reviews = new List<ReviewDTO>(),
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
            var controller = new AiController(_mockUnityOfWork, _mock);
            var request = new AiRecomendationRequestDTO
            {
                Reviews = new List<ReviewDTO>(),
                SaleRecords = new List<SaleRecordDTO>() // Simulating empty sale records
            };
            // Act: Desire to call the Recommend method
            var result = controller.Recommend(request);
            // Assert: Check if the result is a BadRequestObjectResult
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Sale records cannot be empty.", badRequestResult.Value);
        }

        /// <summary>
        /// Test to ensure that the Recommend method returns an OkObjectResult with the correct recommended dishes
        /// </summary>
        [Fact]
        public void Recommend_ReturnOk_WhithTopTwoDishes()
        {
            // Arrange: Arrange defines the context for the test.Define the controller and request with valid sale records. 
            var controller = new AiController(_mockUnityOfWork, _mock);
            var request = new AiRecomendationRequestDTO
            {
                Reviews = new List<ReviewDTO>
                {   new ReviewDTO { CustomerName = "Jonh", SentimentScore=0.7,DishName ="Pizza", Comment = "Pizza was delicious", DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 5 },
                    new ReviewDTO { CustomerName = "Kim", SentimentScore=0.6, DishName= "Burger", Comment = "Burger was great", DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 4 },
                    new ReviewDTO { CustomerName = "Dariem", SentimentScore=0.5,DishName = "Pasta", Comment = "Pasta was okay", DateCreated = DateTime.UtcNow.AddDays(-3), Rating = 3 }
                },
                SaleRecords = new List<SaleRecordDTO>
                {
                    new SaleRecordDTO { DishId = 1, QuantitySold = 10, DishName = "Pizza" },
                    new SaleRecordDTO { DishId = 2, QuantitySold = 5 , DishName = "Burger"},
                    new SaleRecordDTO { DishId = 3, QuantitySold = 8 , DishName = "Pasta"}
                }
            };
            // Act: Act define the action to be tested.Desire to call the Recommend method.
            var result = controller.Recommend(request);

            // Assert: Assert checks the outcome of the action.  Check if the result is an OkObjectResult.
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = okResult.Value as List<AiRecomendationResponseDTO>;

            // Verify the recommended dishes and strategy note
            Assert.NotNull(response);
            // Only one dish should be recommended based on the highest sales
            Assert.Single(response);
            Assert.Equal("Pizza", response[0].RecomendedDish);
        }

        /// <summary>
        ///  Test to ensure that the Recommend method returns an OkObjectResult with the correct recommended dishes
        /// </summary>
        [Fact]
        public void Recommend_ReturnOk_WhenTopDishesAreTied()
        {
            // Arrange: Define the controller and request with sale records where two dishes are tied
            var controller = new AiController(_mockUnityOfWork, _mock);
            var request = new AiRecomendationRequestDTO
            {
                Reviews = new List<ReviewDTO>
                {
                    new ReviewDTO { CustomerName = "Jonh", DishName = "Pizza", SentimentScore = 0.7, Comment = "Pizza was delicious", DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 5 },
                    new ReviewDTO { CustomerName = "Kim", DishName = "Burger", SentimentScore = 0.8, Comment = "Burger was great", DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 4 },
                    new ReviewDTO { CustomerName = "Dariem", DishName = "Pasta", SentimentScore = 0.5, Comment = "Pasta was okay", DateCreated = DateTime.UtcNow.AddDays(-3), Rating = 3 }
                },
                SaleRecords = new List<SaleRecordDTO>
                {
                    new SaleRecordDTO { DishId = 1, DishName = "Pizza", QuantitySold = 10 },
                    new SaleRecordDTO { DishId = 2, DishName = "Burger", QuantitySold = 10 }, // Tied with Pizza
                    new SaleRecordDTO { DishId = 3, DishName = "Pasta", QuantitySold = 8 }
                }
            };

            // Act: Desire to call the Recommend method.
            var result = controller.Recommend(request);

            // Assert: Check if the result is an OkObjectResult.
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = okResult.Value as List<AiRecomendationResponseDTO>;

            // Verify the recommended dishes and strategy note
            Assert.NotNull(response);
            // Both Pizza and Burger should be recommended since both have positive sentiment and are tied in sales
            Assert.Equal(2, response.Count);
            Assert.Contains(response, r => r.RecomendedDish == "Pizza");
            Assert.Contains(response, r => r.RecomendedDish == "Burger");
            // Strategy note should still be the same
            Assert.All(response, r => Assert.Equal("AI strategy to boost this item with promotions and track review sentiment to refine.", r.StrategyNote));
        }
    }
}
