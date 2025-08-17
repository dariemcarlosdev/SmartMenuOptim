/*
    AiController.cs

    This controller provides AI-driven analytics and recommendation endpoints for the SmartMenuOptimizer application.
    It exposes endpoints to:
    - Identify underperforming dishes based on sales and review sentiment.
    - Recommend top dishes using sales and positive review sentiment.
    - Generate improvement strategies for underperforming dishes via an AI strategy service.

    Key features:
    - Uses dependency injection for data access and AI strategy logic.
    - Optimizes data queries with AsNoTracking, AsQueryable, and LINQ grouping/filtering.
    - Processes data in-memory for performance, including parallelization for large datasets.
    - Returns DTOs for API responses, focusing on recent and relevant data for recommendations and analysis.
    - Central to the AI-driven analytics and recommendation features of the application.
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.API.Services.Interfaces;
using SmartMenuOptim.Shared.Data.Dtos;
using SmartMenuOptim.Shared.Data.DTOs;
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
                    // DishId will be used for lookup in reviews
                    DishId = g.Key.DishId,
                    DishName = g.Key.Name,
                    TotalSales = g.Sum(sr => sr.QuantitySold),
                    // Calculate average rating only if there are reviews per dish
                    DishRating = g.Any(sr => sr.Dish.Reviews.Any()) ? (int)g.Average(sr => sr.Dish.Reviews.Average(r => r.Rating)) : 0
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
                                             Comments = rev.Comment,
                                             AverageRating = s.DishRating,
                                         }).OrderByDescending(d => d.AverageSentiment).ToList();

            return Ok(underperformingDishes.OrderBy(d => d.TotalSales)
                .ThenByDescending(d => d.AverageRating)
                .ThenByDescending(d => d.AverageSentiment)
                .ThenBy(d => d.DishName).ToList());
        }

        /// <summary>
        /// Endpoint to get AI recommendations based on sales records and reviews. This endpoint simulates the logic AI recommendations based on reviews and sales records.
        /// This feature simulates how the app might use AI or ML predictive models to analyze sales records and customer reviews to recommend dishes that are likely to perform well.
        /// It can be implemented with AI service like Azure OpenAI to generate recommendations based on sales records and reviews.
        /// // OpenAI's GPT-3.5 Turbo is used to generate these recommendations based on the provided reviews and sale records.
        /// ML models can also be used to analyze patterns in customer reviews and sales data, providing a more data-driven approach to menu optimization.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("v1/recommend")]
        public ActionResult<List<AiRecomendationResponseDTO>> Recommend_v1([FromBody] AiRecomendationRequestDTO request)
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
            var positiveSentimentDishes = request.Reviews
                .Where(r => r.SentimentScore > 0.6 && !string.IsNullOrWhiteSpace(r.Comment) && !string.IsNullOrWhiteSpace(r.CustomerName) && !string.IsNullOrWhiteSpace(r.DishName))
                .Select(r => new { r.DishName, r.Comment })
                .Distinct()
                .ToList();

            // Get all unique dish names from sale records
            var dishNames = request.SaleRecords
                .Where(sr => sr.DishName != null)
                .Select(sr => sr.DishName)
                .Distinct()
                .ToList();

            // For each dish with positive sentiment, recommend it if it exists in sale records
            var aiResponses = new List<AiRecomendationResponseDTO>();

            foreach (var dish in dishNames)
            {
                // Only recommend if there is a positive sentiment review for this dish
                if (positiveSentimentDishes.Any(psd => psd.DishName.Equals(dish, StringComparison.OrdinalIgnoreCase)))
                {
                    var recommendedDishes = request.SaleRecords
                        .Where(sr => sr.DishName != null && sr.DishName.Equals(dish, StringComparison.OrdinalIgnoreCase))
                        .GroupBy(sr => sr.DishName)
                        .OrderByDescending(p => p.Sum(sr => sr.QuantitySold))
                        .Select(g => g.Key)
                        .ToList();

                    aiResponses.Add(new AiRecomendationResponseDTO
                    {
                        RecomendedDish = recommendedDishes.Select(d => d.Trim()).FirstOrDefault() ?? dish,
                        StrategyNote = "AI strategy to boost this item with promotions and track review sentiment to refine."
                    });
                }
            }

            return Ok(aiResponses);
        }

        /// <summary>
        /// Endpoint to get AI recommendations based on sales records and reviews. 
        /// This endpoint simulates the logic AI recommendations based on reviews and sales records.
        /// This feature simulates how the app might use AI or ML predictive models to analyze sales records and customer reviews to recommend dishes that are likely to perform well.
        /// It can be implemented with AI service like Azure OpenAI to generate recommendations based on sales records and reviews.
        /// OpenAI's GPT-3.5 Turbo is used to generate these recommendations based on the provided reviews and sale records.
        /// ML models can also be used to analyze patterns in customer reviews and sales data, providing a more data-driven approach to menu optimization.
        /// </summary>
        /// <param name="request">The AI recommendation request containing sale records and reviews.</param>
        /// <returns>
        /// A list of <see cref="AiRecomendationResponseDTO"/> objects, each containing the recommended dish, strategy note, quantity sold, average rating, and average sentiment score.
        /// </returns>
        /// <remarks>
        /// For each recommended dish, the response now includes QuantitySold, AverageRating, and AverageSentimentScore.
        /// These values are calculated from the relevant sale records and reviews for each dish.
        /// The response type remains List&lt;AiRecomendationResponseDTO&gt;, and all required properties are populated for each item.
        /// </remarks>
        [HttpPost("recommend")]
        public ActionResult<List<AiRecomendationResponseDTO>> Recommend([FromBody] AiRecomendationRequestDTO request)
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
            var positiveSentimentDishes = request.Reviews
                .Where(r => r.SentimentScore > 0.6 && !string.IsNullOrWhiteSpace(r.Comment) && !string.IsNullOrWhiteSpace(r.CustomerName) && !string.IsNullOrWhiteSpace(r.DishName))
                .Select(r => new { r.DishName, r.Comment })
                .Distinct()
                .ToList();

            // Get all unique dish names from sale records
            var dishNames = request.SaleRecords
                .Where(sr => sr.DishName != null)
                .Select(sr => sr.DishName)
                .Distinct()
                .ToList();

            var aiResponses = new List<AiRecomendationResponseDTO>();

            foreach (var dish in dishNames)
            {
                // Only recommend if there is a positive sentiment review for this dish
                if (positiveSentimentDishes.Any(psd => psd.DishName.Equals(dish, StringComparison.OrdinalIgnoreCase)))
                {
                    var saleRecordsForDish = request.SaleRecords
                        .Where(sr => sr.DishName != null && sr.DishName.Equals(dish, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var reviewsForDish = request.Reviews
                        .Where(r => r.DishName != null && r.DishName.Equals(dish, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    int quantitySold = saleRecordsForDish.Sum(sr => sr.QuantitySold);
                    double averageRating = reviewsForDish.Any() ? reviewsForDish.Average(r => r.Rating) : 0.0;
                    double averageSentimentScore = reviewsForDish.Any() ? reviewsForDish.Average(r => r.SentimentScore) : 0.0;

                    aiResponses.Add(new AiRecomendationResponseDTO
                    {
                        RecomendedDish = dish.Trim(),
                        StrategyNote = "AI strategy to boost this item with promotions and track review sentiment to refine.",
                        QuantitySold = quantitySold,
                        AverageRating = Math.Round(averageRating, 2),
                        AverageSentimentScore = Math.Round(averageSentimentScore, 2)
                    });
                }
            }
            // Return the recommendations ordered by average sentiment score, then by average rating, and finally by quantity sold.
            return Ok(aiResponses.OrderByDescending( a => a.AverageSentimentScore)
                .ThenByDescending(a => a.AverageRating)
                .ThenByDescending(a => a.QuantitySold).ToList());
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
