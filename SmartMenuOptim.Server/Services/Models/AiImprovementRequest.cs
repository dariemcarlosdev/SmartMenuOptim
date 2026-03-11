/*
 * File: AiImprovementRequest.cs
 * Request model for AI improvement suggestions
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Encapsulates data needed for AI-powered dish improvement requests.
 */

using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Server.Services.Models;

/// <summary>
/// Request model for AI improvement strategy generation.
/// </summary>
public class AiImprovementRequest
{
    /// <summary>
    /// The name of the dish requiring improvement.
    /// </summary>
    [Required]
    public required string DishName { get; set; }

    /// <summary>
    /// Total sales count for the dish.
    /// </summary>
    [Required]
    public int TotalSales { get; set; }

    /// <summary>
    /// Average sentiment score from reviews.
    /// </summary>
    [Required]
    public double AverageSentiment { get; set; }

    /// <summary>
    /// List of review comments for the dish.
    /// </summary>
    public List<string>? Comments { get; set; }
}
