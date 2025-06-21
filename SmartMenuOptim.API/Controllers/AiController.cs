using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Shared.Data.Dtos;
using SmartMenuOptim.Shared.Data.Entities;
using SmartMenuOptim.Shared.Data.Interfaces;

namespace SmartMenuOptim.API.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly IUnityOfWork _unityOfWork;

        public AiController(IUnityOfWork unityOfWork)
        {
            _unityOfWork = unityOfWork ?? throw new ArgumentNullException(nameof(unityOfWork));
        }


        // This controller handles AI-related operations, such as generating recommendations based on sales records and reviews.

        // Add Endpoint for underperforming dishes based on reviews and sales records.
        [HttpGet("underperforming-dishes")]
        public async Task<ActionResult<IEnumerable<UnderperformingDishDTO>>> GetUnderperformingDishes()
        {
            // Only consider sales from the last 7 days
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            // // Calculate total sales for each dish in the last 7 days
            // ShowCase1: This block uses the Query() method to get an IQueryable for further LINQ operations on the repository, allowing for efficient filtering and grouping in the database.

            var saleRecords = await _unityOfWork.SaleRecords.Query() 
                .Where(sr => sr.SaleDate >= sevenDaysAgo) // for optimization, filter records from the last 7 days
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
                //.Where(r => r.Comment != null && r.CreatedDate >= sevenDaysAgo) // for optimization, filter reviews with non-null comments, add CreatedDate property to Review entity if needed
                .Select(r => new { r.Comment, r.SentimentScore }) //for optimization, only select necessary fields
                .AsNoTracking()
                .ToListAsync();
            
            var dishNames = saleRecords.Select(sr => sr.DishName.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();


                var sentimentResults = dishNames
                .AsParallel() // for the in-memory LINQ if the dataset is large processing to improve performance.
                .Select(dishName =>
                {
                    var dishWords = dishName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    
                    var matchingReviews = allReviews
                        .Where(r => r.Comment != null &&
                                    dishWords.Any(word =>
                                    r.Comment.Contains(dishName, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    return new
                    {
                        DishName = dishName,
                        AverageSentiment = matchingReviews.Any()
                            ? matchingReviews.Average(r => r.SentimentScore)
                            : (double?)null
                    };
                })
                .Where(x => x.AverageSentiment.HasValue)
                .Select(x => new
                {
                    x.DishName,
                    AverageSentiment = x.AverageSentiment.Value
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
                                              AverageSentiment = Math.Round(rev.AverageSentiment, 2)
                                          }).ToList();

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
            // Validate the request
            if (request.SaleRecords == null || !request.SaleRecords.Any())
            {
                return BadRequest("Sale records cannot be empty.");
            }

            // Simulate AI recommendation logic: recommend the top 2 dishes based on sales records
            // LINQ 
            var recommendedDishes = request.SaleRecords
                .GroupBy(sr => sr.DishName)
                .Select(g => new
                {
                    Dish = g.Key,
                    TotalSold = g.Sum(sr => sr.QuantitySold)
                })
                .OrderByDescending(g => g.TotalSold)
                .Take(2)
                .Select(g => g.Dish)
                .ToList();

            var response = new AiRecomendationResponse
            {
                RecomendedDishes = recommendedDishes,
                StrategyNote = "Boost these items with promotions and track review sentiment to refine."
            };
            return Ok(response);
        }

    }
}
