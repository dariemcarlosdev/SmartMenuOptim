using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.API.Services.Interfaces;
using SmartMenuOptim.Shared.Data.Dtos;
using SmartMenuOptim.Shared.Data.Entities;
using SmartMenuOptim.Shared.Data.Interfaces;
using System.Security.Principal;

namespace SmartMenuOptim.API.Controllers
{
    /*
     The AiController.cs file defines an ASP.NET Core API controller for AI-related operations in your application. It exposes endpoints for:
     
     1.	Getting underperforming dishes based on recent sales and review sentiment.
     2.	Recommending top dishes based on sales and positive review sentiment.
     3.	Generating improvement strategies for underperforming dishes using an AI strategy service.

    Key features:
    Uses dependency injection for data access (IUnityOfWork) and AI strategy logic (IAiImprovementStrategyService).
    Optimizes data queries with AsNoTracking, AsQueryable, and LINQ grouping/filtering.
    Filters and processes data in-memory for performance, including parallelization for large datasets.
    Returns DTOs for API responses, focusing on recent and relevant data for recommendations and analysis.
    This controller is central to the AI-driven analytics and recommendation features of your application. 
     */
     
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly IUnityOfWork _unityOfWork;
        //Inject IAiImprovementService _aiImprovementService into the controller
        private readonly IAiImprovementStrategyService _aiImprovementService;

        public AiController(IUnityOfWork unityOfWork, IAiImprovementStrategyService aiImprovementService)
        {
            _unityOfWork = unityOfWork ?? throw new ArgumentNullException(nameof(unityOfWork));
            _aiImprovementService = aiImprovementService ?? throw new ArgumentNullException(nameof(aiImprovementService));
        }


        // This controller handles AI-related operations, such as generating recommendations based on sales records and reviews.
        // Set of improvements:
        // 1. Use of AsQueryable() for better LINQ operations on the repository.
        // 2. Use of AsNoTracking() for read-only queries to improve performance.
        // 3. Use of parallel processing for in-memory LINQ operations to improve performance.
        // 4. Use of StringSplitOptions to handle whitespace and trimming in dish names.
        // 5. Use of StringComparison.OrdinalIgnoreCase for case-insensitive string comparisons.
        // 6. Selecting only necessary fields in queries to optimize performance.
        // 7. Using Query() method to get an IQueryable for further LINQ operations on the repository, allowing for efficient filtering and grouping in the database.
        // 8. Using Task-based asynchronous programming for database operations to improve responsiveness and scalability.
        // 9. Filter records from the last 7 days to focus on recent data, which is more relevant for performance analysis.

        /// <summary>
        /// Endpoint to get underperforming dishes based on reviews and sales records.
        /// </summary>
        /// <returns></returns>
        [HttpGet("underperforming")]
        public async Task<ActionResult<IEnumerable<UnderperformingDishDTO>>> GetUnderperformingDishes()
        {
            // Filter by Date Reduce Data Volume Early: If you only care about recent reviews (e.g., last 7 days), filter reviews by date before loading them into memory.
            var oneYearAgo = DateTime.UtcNow.AddDays(-360); //change to -7 for last 365 days, with the current logic we are looking for last 360 days

            // // Calculate total sales for each dish in the last 7 days
            // ShowCase1: This block uses the Query() method to get an IQueryable for further LINQ operations on the repository, allowing for efficient filtering and grouping in the database.

            var saleRecords = await _unityOfWork.SaleRecords.Query() 
                .Where(sr => sr.SaleDate >= oneYearAgo) // for optimization, filter records from the last 7 days
                .AsQueryable()
                .AsNoTracking()
                .GroupBy(sr => sr.DishName)
                .Select(g => new
                {
                    DishName = g.Key,
                    TotalSales = g.Sum(sr => sr.QuantitySold)
                }).ToListAsync();

            // Fetch all reviews with non-null comments (project only needed fields
            var allReviews = await _unityOfWork.Reviews.Query()
                .Where(r => r.Comment != null)
                .Select(r => new { r.Comment, r.SentimentScore }) //for optimization, only select necessary fields
                .AsNoTracking()
                .ToListAsync();
            
            var dishNames = saleRecords.Select(sr => sr.DishName.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); // This selects distinct dish names from the sale records, trimming any whitespace and ensuring case insensitivity.


            var sentimentResults = dishNames
                .AsParallel() // for the in-memory LINQ if the dataset is large processing to improve performance.
                .Select(dishName =>
                {
                    // Dish name normalization: split by spaces and trim entries to handle cases like "Pizza Margherita" vs "Pizza  Margherita"
                    var dishWords = dishName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    // Assuming that the dish name is a key word in the review comment, we can check if any of the words in the dish name are present in the review comments.
                    var matchingReviews = allReviews
                        .Where(r => r.Comment != null &&
                                    dishWords.Any(word =>
                                    r.Comment.Contains(dishName, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                    // Return an anonymous object with the dish name and the average sentiment score for that dish
                    return new
                    {
                        DishName = dishName,
                        Comment = matchingReviews.Select(r => r.Comment).ToList(),
                        AverageSentiment = matchingReviews.Any()
                            ? matchingReviews.Average(r => r.SentimentScore)
                            : (double?)null
                    };
                })
                .Where(x => x.AverageSentiment.HasValue)
                .Select(x => new
                {
                    x.DishName,
                    AverageSentiment = x.AverageSentiment.Value,
                    x.Comment
                })
                .ToList();

            // Merge the two lists to find underperforming dishes in the last 7 days
            var underperformingDishes = ( from s in saleRecords
                                          join rev in sentimentResults on s.DishName equals rev.DishName
                                          where s.TotalSales <=35 && rev.AverageSentiment < 0.6
                                          select new UnderperformingDishDTO
                                          {
                                              DishName = s.DishName,
                                              TotalSales = s.TotalSales,
                                              AverageSentiment = Math.Round(rev.AverageSentiment, 2),
                                              Comments = rev.Comment

                                          }).OrderBy(d => d.AverageSentiment).ToList();

            return Ok(underperformingDishes);
        }

        /// <summary>
        /// Endpoint to get AI recommendations based on sales records and reviews.This endpoint simulates the logic AI recommendations based on reviews and sales records.
        /// This feature can be implemented with AI service like Azure OpenAI to generate recommendations based on sales records and reviews.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("recommend")]
        public ActionResult<AiRecomendationRequest> Recommend([FromBody] AiRecomendationRequest request)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(request));
            // Validating the request 
            if (request.SaleRecords == null || !request.SaleRecords.Any() || request.SaleRecords.Any(sr => sr == null))
            {
                return BadRequest("Sale records cannot be empty.");
            }

            if (request.Reviews == null || !request.Reviews.Any() || request.Reviews.Any(r => r == null))
            {
                return BadRequest("Reviews cannot be empty.");
            }

            // Filter reviews to get only those with positive sentiment (> 0.6), non-empty comment, and non-empty customer name.
            // Select the comment from each review, ensure uniqueness, and convert to a list.
            // Purpose:This list of positive review comments is used to identify dishes that have received positive feedback, which is then used for generating dish recommendations.
            
             var positiveSentimentDishes = request.Reviews
                .Where(r => r.SentimentScore > 0.6 && !string.IsNullOrWhiteSpace(r.Comment) && !string.IsNullOrWhiteSpace(r.CustomerName))
                .Select(r => r.Comment)
                .Distinct()
                .ToList();

            // This block generates dish recommendations based on sales records and positive review sentiment:
            // 1. Filters sale records to include only those where the dish name matches any comment from positiveSentimentDishes (i.e., dishes with positive reviews).
            // 2. Groups the filtered sale records by dish name.
            // 3. For each group, calculates the total quantity sold.
            // 4. Orders the groups by total sold in descending order.
            // 5. Takes the top 3 dishes.
            // 6. Selects only the dish names for the final recommendation list.
            var recommendedDishes = request.SaleRecords
                .Where(sr => positiveSentimentDishes.Any(dish => sr.DishName.Contains(dish, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(sr => sr.DishName)
                .Select(g => new
                {
                    Dish = g.Key,
                    TotalSold = g.Sum(sr => sr.QuantitySold)
                })
                .OrderByDescending(g => g.TotalSold)
                .Take(3)
                .Select(g => g.Dish)
                .ToList();

            var response = new AiRecomendationResponse
            {
                RecomendedDishes = recommendedDishes,
                StrategyNote = "Boost these items with promotions and track review sentiment to refine."
            };
            return Ok(response);
        }

        [HttpPost("underperforming/strategies")]
        public async Task<ActionResult<string>> GetImprovementStrategies([FromBody] List<UnderperformingDishDTO> underperformingDishes)
        {
            // Validate input
            if (underperformingDishes == null || !underperformingDishes.Any())
            {
                return BadRequest("The list of underperforming dishes cannot be null or empty.");
            }
            var strategyNote = await _aiImprovementService.GetImprovementStrategyAsync(underperformingDishes);
            return Ok(strategyNote);
        }
    }
}
