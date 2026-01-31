using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Application.Dtos
{
    public class AiRecomendationResponseDTO
    {
        public string RecomendedDish { get; set; }
        // StrategyNote provides additional context or explanation for the recommendations. It can be used to guide the restaurant on how to implement the recommendations effectively.
        // For example, it might suggest boosting certain items with promotions or tracking review sentiment to refine the recommendations.
        // Empowering the restaurant with actionable insights to improve their menu offerings and customer satisfaction using AI-driven recommendations with Optimiation.
        // OpenAI's GPT-3.5 Turbo is used to generate these recommendations based on the provided reviews and sale records.
        // ML models can also be used to analyze patterns in customer reviews and sales data, providing a more data-driven approach to menu optimization.
        public string StrategyNote { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public double AverageRating { get; set; }
        public double AverageSentimentScore { get; set; }
    }
}
