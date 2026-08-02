using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Server.Features.AI.Services;
using SmartMenuOptim.Server.Features.Reviews.Models;
using SmartMenuOptim.Server.Features.Reviews.Services;

namespace SmartMenuOptim.Server.Features.Reviews.Components;

/// <summary>
/// Code-behind for the Reviews page.
/// Loads customer reviews (with optional dish/sentiment query filters), applies the shared
/// <see cref="ReviewFilterState"/> for sort/rating/search, and paginates the result.
/// </summary>
public partial class Reviews : ComponentBase
{
    [Inject] private IAIClientService AIService { get; set; } = default!;
    [Inject] private IReviewClientService ReviewService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private ReviewFilterState filterState = new ReviewFilterState(); // Shared filter state
    private List<ReviewDTO>? reviews;
    private bool isloading = true;
    private int CurrentPage = 1;
    private int PageSize = 10;

    // Computed properties for filtering and sorting
    private IEnumerable<ReviewDTO> FilteredReviews
    {
        get
        {
            if (reviews == null)
                return Enumerable.Empty<ReviewDTO>();

            var query = reviews.AsEnumerable();

            // Apply rating filter
            if (filterState.MinRating > 0)
            {
                query = query.Where(r => r.Rating >= filterState.MinRating);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(filterState.SearchTerm))
            {
                var term = filterState.SearchTerm.Trim().ToLowerInvariant();
                query = query.Where(r =>
                    (r.Comment?.ToLowerInvariant().Contains(term) ?? false) ||
                    (r.DishName?.ToLowerInvariant().Contains(term) ?? false) ||
                    (r.CustomerName?.ToLowerInvariant().Contains(term) ?? false));
            }

            // Apply sorting
            return filterState.SortBy switch
            {
                "rating" => query.OrderByDescending(r => r.Rating),
                "sentiment" => query.OrderByDescending(r => r.SentimentScore),
                _ => query.OrderByDescending(r => r.DateCreated)
            };
        }
    }

    // Pagination logic for filtered reviews
    private IEnumerable<ReviewDTO> PagedAndFilteredReviews =>
        FilteredReviews
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

    // Calculate total pages based on filtered reviews
    private int TotalPages =>
        (int)Math.Ceiling(FilteredReviews.Count() / (double)PageSize);

    protected override async Task OnInitializedAsync()
    {
        await LoadReviews();
    }

    private async Task LoadReviews()
    {
        isloading = true;

        try
        {
            string? dishNameFilter = null;
            double? sentimentFilter = null;

            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var query = QueryHelpers.ParseQuery(uri.Query); // Parse query parameters

            if (query.TryGetValue("dishname", out var dishNameParam))
            {
                dishNameFilter = dishNameParam.ToString();
            }
            if (query.TryGetValue("sentiment", out var sentimentParam) &&
                double.TryParse(sentimentParam, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var sentimentValue))
            {
                sentimentFilter = sentimentValue;
            }

            reviews = await ReviewService.GetReviewsAsync(dishNameFilter, sentimentFilter);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading reviews: {ex.Message}");
        }
        finally
        {
            isloading = false;
            CurrentPage = 1;
            StateHasChanged(); // Refresh UI after loading
        }
    }

    private void ChangePage(int page)
    {
        if (page < 1 || page > TotalPages) return;
        CurrentPage = page;
        StateHasChanged();
    }

    private void HandleFiltersChanged()
    {
        CurrentPage = 1;
        StateHasChanged();
    }
}
