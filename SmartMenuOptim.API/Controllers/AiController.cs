using Microsoft.AspNetCore.Mvc;
using SmartMenuOptim.Shared.Data.Entities;
using SmartMenuOptim.Shared.Data.Entities;

namespace SmartMenuOptim.API.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {


        // This endpoint simulates AI recommendations based on reviews and sales records.
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
