using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Server.Features.AI.Services;
using SmartMenuOptim.Server.Features.Reviews.Models;
using SmartMenuOptim.Server.Features.Reviews.Services;

namespace SmartMenuOptim.Server.Features.Reviews.Components;

/// <summary>
/// Code-behind for the SubmitReview page.
/// Renders a validated feedback form and posts a new <see cref="ReviewDTO"/> via the review client service.
/// </summary>
public partial class SubmitReview : ComponentBase
{
    [Inject] private IReviewClientService ReviewService { get; set; } = default!;
    [Inject] private IAIClientService AIService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private ReviewFormModel reviewModel = new();
    private bool isSubmitting = false;
    private bool isSubmitted = false;

    private async Task HandleValidSubmit()
    {
        isSubmitting = true;

        var reviewDto = new ReviewDTO
        {
            CustomerName = reviewModel.CustomerName,
            DishName = reviewModel.DishName,
            Rating = reviewModel.Rating,
            Comment = reviewModel.Comment,
            DateCreated = DateTime.UtcNow
        };

        try
        {
            await ReviewService.AddReviewAsync(reviewDto);
            isSubmitted = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error submitting review: {ex.Message}");
            // Optionally, display an error message to the user
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private void SubmitAnother()
    {
        reviewModel = new();
        isSubmitted = false;
    }
}
