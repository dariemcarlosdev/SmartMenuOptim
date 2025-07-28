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
            // Get thresholds from the first admin user (or use defaults)
            var admin = await _unityOfWork.AdminUsers.Query().OrderBy(a => a.Id).FirstOrDefaultAsync();
            double salesThreshold = admin?.SalesThreshold ?? 35;
            double sentimentThreshold = admin?.SentimentThreshold ?? 0.6;

            var oneYearAgo = DateTime.UtcNow.AddDays(-360);

            // Group sales by DishId and DishName
            var saleRecords = await _unityOfWork.SaleRecords.Query()
                .Where(sr => sr.SaleDate >= oneYearAgo)
                .AsQueryable()
                .AsNoTracking()
                .GroupBy(sr => new { sr.DishId, sr.Dish.Name })
                .Select(g => new
                {
                    DishId = g.Key.DishId,
                    DishName = g.Key.Name,
                    TotalSales = g.Sum(sr => sr.QuantitySold)
                }).ToListAsync();

            // Get all reviews with DishId
            var allReviews = await _unityOfWork.Reviews.Query()
                .Where(r => r.Comment != null)
                .Select(r => new { r.DishId, r.Comment, r.SentimentScore })
                .AsNoTracking()
                .ToListAsync();

            // Group reviews by DishId for only dishes in saleRecords
            var sentimentResults = saleRecords
                .Select(sale =>
                {
                    var matchingReviews = allReviews
                        .Where(r => r.DishId == sale.DishId)
                        .ToList();
                    return new
                    {
                        DishId = sale.DishId,
                        Comment = matchingReviews.Select(r => r.Comment).ToList(),
                        AverageSentiment = matchingReviews.Any()
                            ? matchingReviews.Average(r => r.SentimentScore)
                            : (double?)null
                    };
                })
                .Where(x => x.AverageSentiment.HasValue)
                .Select(x => new
                {
                    x.DishId,
                    AverageSentiment = x.AverageSentiment.Value,
                    x.Comment
                })
                .ToList();

            // Compose underperforming dishes
            var underperformingDishes = (from s in saleRecords
                                         join rev in sentimentResults
                                           on s.DishId equals rev.DishId
                                         where s.TotalSales <= salesThreshold && rev.AverageSentiment < sentimentThreshold
                                         select new UnderperformingDishDTO
                                         {
                                             DishId = s.DishId,
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
        /// // OpenAI's GPT-3.5 Turbo is used to generate these recommendations based on the provided reviews and sale records.
        /// ML models can also be used to analyze patterns in customer reviews and sales data, providing a more data-driven approach to menu optimization.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("recommend")]
        public ActionResult<AiRecomendationResponse> Recommend([FromBody] AiRecomendationRequest request)
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
            // 1. Filters sale records to include only those where the dish name appears in any positive review comment (case-insensitive).
            //    For example, if a review comment says "spaguetti carbonara was espectacular" and the dish name is "Spaguetti Carbonara",
            //    the dish will be recommended if the sentiment is positive.
            // 2. Groups the filtered sale records by dish name.
            // 3. For each group, calculates the total quantity sold.
            // 4. Orders the groups by total sold in descending order.
            // 5. Takes the top 3 dishes.
            // 6. Selects only the dish names for the final recommendation list.
            var dishNames = request.SaleRecords
                .Where(sr => sr.Dish != null)
                .Select(sr => sr.Dish.Name)
                .Distinct()
                .ToList();

            var recommendedDishes = request.SaleRecords
                .Where(sr => sr.Dish != null && positiveSentimentDishes.Any(comment =>
                    !string.IsNullOrWhiteSpace(comment) &&
                    comment.Contains(sr.Dish.Name, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(sr => sr.Dish.Name)
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

        /// <summary>
        /// Endpoint to get AI improvement strategy for underperforming dishes.
        /// </summary>
        /// <param name="underperformingDish"></param>
        /// <returns></returns>
        [HttpPost("underperforming/improve-strategy")]
        public async Task<ActionResult<string>> GetImprovementStrategy([FromQuery] string name, [FromQuery] int sales, [FromQuery] double sentiment)
        {
            // Create an instance of UnderperformingDishDTO from the query parameters
            var underperformingDish = new UnderperformingDishDTO
            {
                DishName = name,
                TotalSales = sales,
                AverageSentiment = sentiment
            };

            // Validate input
            if (underperformingDish == null)
            {
                return BadRequest("Underperforming dish cannot be null.");
            }
            var GptStrategy = await _aiImprovementService.GetImprovementStrategyAsync(underperformingDish);
            return Ok(GptStrategy);
        }
    }
}
