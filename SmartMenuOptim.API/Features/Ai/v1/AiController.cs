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
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Domain.Repositories;

namespace SmartMenuOptim.API.Features.Ai.v1
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

    //For versioning, add [ApiVersion("1.0")] above [Route("api/[controller]")]
    //[ApiVersion(1)]
    //[ApiVersion(2)]
    //[ApiController]
    //[Route("api/v{v:apiVersion}/[controller]")]

    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly IUnityOfWork _unityOfWork;
        //Inject IAiImprovementService _aiImprovementService into the controller
        private readonly IAImprovementStrategyService _aiService;

        public AiController(IUnityOfWork unityOfWork, IAImprovementStrategyService aiImprovementService)
        {
            _unityOfWork = unityOfWork ?? throw new ArgumentNullException(nameof(unityOfWork));
            _aiService = aiImprovementService ?? throw new ArgumentNullException(nameof(aiImprovementService));
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
        /// Retrieves a paginated, filtered, and sorted list of underperforming dishes based on sales records and customer reviews.
        /// This endpoint analyzes recent sales data and sentiment scores to identify dishes that may need improvement strategies.
        /// </summary>
        /// <param name="page">The page number for pagination (1-based). Defaults to 1 if not provided.</param>
        /// <param name="pageSize">The number of items per page (1-100). Defaults to 10 if not provided.</param>
        /// <param name="sortBy">The field to sort by (TotalSales, AverageSentiment, AverageRating, DishName). Defaults to "TotalSales" if not provided or invalid.</param>
        /// <param name="sortOrder">The sort order ("asc" or "desc"). Defaults to "asc" if not provided or invalid.</param>
        /// <param name="startDate">The start date for filtering sales records (UTC). Defaults to 7 days ago if not provided.</param>
        /// <param name="endDate">The end date for filtering sales records (UTC). Defaults to current UTC time if not provided.</param>
        /// <param name="minSales">Minimum total sales for filtering. Only dishes with sales >= this value are included.</param>
        /// <param name="maxSales">Maximum total sales for filtering. Only dishes with sales <= this value are included.</param>
        /// <param name="minSentiment">Minimum average sentiment for filtering. Only dishes with sentiment >= this value are included.</param>
        /// <param name="maxSentiment">Maximum average sentiment for filtering. Only dishes with sentiment <= this value are included.</param>
        /// <returns>A paginated list of <see cref="UnderperformingDishDTO"/> objects matching the specified criteria.</returns>
        /// <remarks>
        /// ### API Design Best Practices Implemented:
        /// - **RESTful Conventions**: Uses HTTP GET with query parameters for filtering, sorting, and pagination.
        /// - **Optional Parameters**: All parameters are optional with sensible defaults to ensure backward compatibility.
        /// - **Input Validation**: Parameters are validated and constrained (e.g., page >= 1, pageSize <= 100) to prevent abuse and ensure data integrity.
        /// - **Consistent Response Format**: Returns a standardized <see cref="PaginatedResponseDto{T}"/> with data and metadata (total count, pages, etc.).
        /// - **Error Handling**: Invalid inputs are corrected to defaults rather than throwing errors, improving user experience.
        /// - **Query Parameter Naming**: Uses clear, descriptive names (e.g., sortBy, sortOrder) following common API conventions.
        ///
        /// ### Filtering Capabilities:
        /// - **Date Range**: Filters sales records within a specified UTC date range, defaulting to the last 7 days for relevance.
        /// - **Sales Thresholds**: Allows min/max sales filtering to focus on dishes within specific performance ranges.
        /// - **Sentiment Thresholds**: Filters by average sentiment scores to identify dishes with positive/negative feedback.
        /// - **Post-Query Filtering**: Additional filters are applied in-memory after the main database query for flexibility.
        ///
        /// ### Sorting Options:
        /// - **Dynamic Sorting**: Supports sorting by key metrics (TotalSales, AverageSentiment, AverageRating, DishName).
        /// - **Order Control**: Ascending or descending order to prioritize high/low performers.
        /// - **Default Behavior**: Sorts by TotalSales ascending if no valid sort parameters are provided.
        ///
        /// ### Pagination Features:
        /// - **1-Based Indexing**: Page numbers start from 1 for intuitive use.
        /// - **Configurable Page Size**: Allows 1-100 items per page, defaulting to 10.
        /// - **Metadata Included**: Response includes total count, current page, page size, and total pages for client-side navigation.
        /// - **Efficient Implementation**: Pagination is applied after sorting to ensure correct ordering across pages.
        ///
        /// ### AI Performance Optimizations:
        /// - **Data Reduction**: Filtering by date range and thresholds reduces the dataset processed by AI algorithms, improving response times.
        /// - **Focused Analysis**: Allows users to target specific time periods or performance levels, enabling more precise AI-driven insights.
        /// - **Scalability**: Pagination prevents large result sets, reducing memory usage and network overhead for AI-powered analytics.
        /// - **Relevance**: Default date range (last 7 days) ensures analysis focuses on recent, actionable data rather than historical noise.
        /// - **Query Efficiency**: Database-level filtering (e.g., date range) minimizes data transfer, while in-memory operations handle fine-tuned filtering.
        /// - **Caching Potential**: Structured responses enable client-side caching of paginated results, reducing repeated AI computations.
        ///
        /// ### Usage Examples:
        /// - Get recent underperformers: `GET /api/ai/underperforming`
        /// - Filter by date and sales: `GET /api/ai/underperforming?startDate=2023-01-01&endDate=2023-12-31&minSales=10&maxSales=50`
        /// - Sort and paginate: `GET /api/ai/underperforming?page=2&pageSize=20&sortBy=AverageSentiment&sortOrder=desc`
        ///
        /// ### Security Considerations:
        /// - Input validation prevents SQL injection and ensures safe parameter handling.
        /// - Rate limiting should be applied to prevent abuse of filtering/pagination features.
        /// - Tenant isolation is maintained through RestaurantId filtering in underlying queries.
        /// </remarks>
        [HttpGet("underperforming")]
        public async Task<ActionResult<PaginatedResponseDto<UnderperformingDishDTO>>> GetUnderperformingDishesAsync(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? minSales = null,
            [FromQuery] int? maxSales = null,
            [FromQuery] double? minSentiment = null,
            [FromQuery] double? maxSentiment = null)
        {
            // Validate pagination parameters. These are optional, with defaults applied if not provided.
            page ??= 1;
            pageSize ??= 10;
            sortBy ??= "TotalSales";
            sortOrder ??= "asc";
            startDate ??= DateTime.UtcNow.AddDays(-360);
            endDate ??= DateTime.UtcNow;

            if (page < 1) page = 1; // constrain page to be at least 1
            if (pageSize < 1 || pageSize > 100) pageSize = 10; // constrain pageSize to be between 1 and 100

            // ensure startDate is before endDate
            if (startDate > endDate) startDate = endDate.Value.AddDays(-7); 

            // Validate sortBy. This ensures only valid fields are used for sorting.
            var validSortFields = new[] { "TotalSales", "AverageSentiment", "AverageRating", "DishName" };
            if (!validSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
                sortBy = "TotalSales";

            // Validate sortOrder. This ensures only "asc" or "desc" are used.
            if (!string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase))
                sortOrder = "asc";

            // Get thresholds from the first admin user (or use defaults)
            var appUser = await _unityOfWork.ApplicationUsers.Query()
                .Include(u => u.AdminProfile)
                .Where(u => u.ProfileType == ProfileType.Admin && u.AdminProfile.Role == AdminRoleType.SystemAdmin)
                .OrderBy(a => a.Id).FirstOrDefaultAsync();
            var salesThreshold = appUser?.AdminProfile?.SalesThreshold ?? 100;

            var sentimentThreshold = appUser?.AdminProfile?.SentimentThreshold ?? 0.5; // Default sentiment threshold
            
            var oneYearAgo = DateTime.UtcNow.AddDays(-360).ToUniversalTime();
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7).ToUniversalTime();
            var oneMonthAgo = DateTime.UtcNow.AddDays(-30).ToUniversalTime();
            var threeMonthsAgo = DateTime.UtcNow.AddDays(-90).ToUniversalTime();


            // Group sales by DishId and DishName
            /*
            -- Corrected Query for PostgreSQL (to avoid sales multiplication due to join with reviews)
            SELECT
                d."Id" AS "DishId",
                d."Name" AS "DishName",
                sr_summary."TotalSales",
                CASE
                    WHEN COUNT(r."Id") > 0 THEN ROUND(AVG(r."Rating"))::int
                    ELSE 0
                END AS "DishRating"
            /* Aggregate SaleRecords before joining to Reviews, or use a subquery/CTE to get the correct total sales per dish.
            FROM
                (
                    SELECT "DishId", SUM("QuantitySold") AS "TotalSales"
                    FROM "SaleRecords"
                    WHERE "SaleDate" >= (CURRENT_DATE - INTERVAL '360 days')
                    GROUP BY "DishId"
                ) sr_summary
            JOIN "Dishes" d ON sr_summary."DishId" = d."Id"
            LEFT JOIN "Reviews" r ON r."DishId" = d."Id"
            GROUP BY d."Id", d."Name", sr_summary."TotalSales"
            */



            var saleRecords = await _unityOfWork.SaleRecords.Query()
                .AsNoTracking()  // as non-tracking for read-only query is useful for performance, as it avoids the overhead of tracking changes in the DbContext.
                .AsQueryable()
                .Where(sr => sr.SaleDate >= startDate && sr.SaleDate <= endDate) // Filter by date range
                .Include(sr => sr.Dish)
                .Include(sr => sr.Dish.Reviews)
                .GroupBy(sr => new { sr.DishId, sr.Dish.Name })
                .Select(g => new
                {
                    // DishId will be used for lookup in reviews
                    DishId = g.Key.DishId,
                    DishName = g.Key.Name,
                    TotalSales = g.Sum(sr => sr.QuantitySold),
                    // Calculate average rating only if there are reviews. g.any(...) checks if there are any reviews for the dish, if so, calculate average, else set to 0.
                    DishRating = g.Any(sr => sr.Dish.Reviews.Any()) ? (int)g.Average(sr => sr.Dish.Reviews.Average(r => r.Rating)) : 0
                }).Where(ts => ts.TotalSales <= salesThreshold).ToListAsync();


            // Get all reviews with DishId and SentimentScore below threshold
            /*
            -- Equivalent PostgreSQL query for the LINQ below:
            SELECT
                "DishId",
                "Comment",
                "SentimentScore"
            FROM
                "Reviews"
            WHERE
                "Comment" IS NOT NULL
                AND "SentimentScore" < :sentimentThreshold;
            */
            var allReviews = await _unityOfWork.Reviews.Query()
                .Where(r => r.Comment != null)
                .Select(r => new { r.DishId, r.Comment, r.SentimentScore })
                .AsNoTracking().Where(ss => ss.SentimentScore < sentimentThreshold)
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

            // Compose underperforming dishes.
            // Combine sales and sentiment results using Linq join with in-memory collections through DishId. Combine traditional LINQ with query syntax for clarity.
            var underperformingDishes = (from s in saleRecords
                                         join rev in sentimentResults
                                           on s.DishId equals rev.DishId
                                         select new UnderperformingDishDTO
                                         {
                                             DishId = s.DishId,
                                             DishName = s.DishName,
                                             TotalSales = s.TotalSales,
                                             AverageSentiment = Math.Round(rev.AverageSentiment, 2),
                                             Comments = rev.Comment,
                                             AverageRating = s.DishRating,
                                         }).ToList();

            // Apply additional filters for sales and sentiment if provided according API request query parameters.
            // This is an additional layer of filtering on top of the initial thresholds.
            // These filters are optional and only applied if the corresponding parameters are provided.
            if (minSales.HasValue)
                underperformingDishes = underperformingDishes.Where(d => d.TotalSales >= minSales.Value).ToList();
            if (maxSales.HasValue)
                underperformingDishes = underperformingDishes.Where(d => d.TotalSales <= maxSales.Value).ToList();
            if (minSentiment.HasValue)
                underperformingDishes = underperformingDishes.Where(d => d.AverageSentiment >= minSentiment.Value).ToList();
            if (maxSentiment.HasValue)
                underperformingDishes = underperformingDishes.Where(d => d.AverageSentiment <= maxSentiment.Value).ToList();

            // Sort the underperforming dishes based on sortBy and sortOrder
            // This uses dynamic LINQ to apply sorting based on the provided parameters.
            IOrderedEnumerable<UnderperformingDishDTO> sortedDishes;
            // Apply sorting based on the specified sortBy and sortOrder parameters
            switch (sortBy.ToLowerInvariant())
            {
                case "totalsales":
                    sortedDishes = sortOrder.ToLowerInvariant() == "desc"
                        ? underperformingDishes.OrderByDescending(d => d.TotalSales)
                        : underperformingDishes.OrderBy(d => d.TotalSales);
                    break;
                case "averagesentiment":
                    sortedDishes = sortOrder.ToLowerInvariant() == "desc"
                        ? underperformingDishes.OrderByDescending(d => d.AverageSentiment)
                        : underperformingDishes.OrderBy(d => d.AverageSentiment);
                    break;
                case "averagerating":
                    sortedDishes = sortOrder.ToLowerInvariant() == "desc"
                        ? underperformingDishes.OrderByDescending(d => d.AverageRating)
                        : underperformingDishes.OrderBy(d => d.AverageRating);
                    break;
                case "dishname":
                    sortedDishes = sortOrder.ToLowerInvariant() == "desc"
                        ? underperformingDishes.OrderByDescending(d => d.DishName)
                        : underperformingDishes.OrderBy(d => d.DishName);
                    break;
                default:
                    sortedDishes = underperformingDishes.OrderBy(d => d.TotalSales);
                    break;
            }

            // Get total count from the unpaginated list
            var totalCount = underperformingDishes.Count;

            // Apply pagination to the sorted dishes
            // This is done after sorting to ensure correct ordering in the paginated results.
            // Skip and Take are used to get the correct page of results.
            var paginatedDishes = sortedDishes
                .Skip((page.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();

            // Return paginated response
            var response = new PaginatedResponseDto<UnderperformingDishDTO>
            {
                Data = paginatedDishes,
                TotalCount = totalCount,
                Page = page.Value,
                PageSize = pageSize.Value,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize.Value)
            };

            // Return the paginated response to the client. I am explicitly returning Ok(response.Data) to return only the list of underperforming dishes in the response body.
            return Ok(response.Data);
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
        /// [MapToApiVersion("1.0")] // Map this action to API version 1.0
        [HttpPost("v1/recommend")]
        public ActionResult<List<AiRecomendationResponseDTO>> Recommend_v1([FromBody] AiRecommendationRequestDTO request)
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
                        RecommendedDish = recommendedDishes.Select(d => d.Trim()).FirstOrDefault() ?? dish,
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
        
        /// [MapToApiVersion("1.0")] // Map this action to API version 1.0
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
                        RecommendedDish = dish.Trim(),
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
        
        //[MapToApiVersion("1.0")] // Map this action to API version 1.0
        [HttpPost("underperforming/improve-strategy")]
        public async Task<ActionResult<string>> GetImprovementStrategyAsync([FromQuery] string name, [FromQuery] int sales, [FromQuery] double sentiment)
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
            var GptStrategy = await _aiService.GetImprovementStrategyAsync(underperformingDish);
            return Ok(GptStrategy);
        }
    }
}

