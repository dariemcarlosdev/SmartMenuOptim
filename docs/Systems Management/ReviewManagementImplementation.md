# Review Management System Implementation Guide

## Overview
This guide outlines the implementation of the Smart Menu Optimization Review Management System, integrating with Profile, Restaurant, and Analytics systems in a Blazor-based architecture.

## Project Structure Integration
```
SmartMenuOptim/
├── SmartMenuOptim.API/
│   └── Controllers/
│       ├── ReviewController.cs
│       └── ReviewAnalyticsController.cs
├── SmartMenuOptim.Server/
│   └── Components/
│       └── Review/
│           ├── ReviewDashboard.razor
│           ├── ReviewList.razor
│           ├── ReviewEditor.razor
│           └── ReviewAnalytics.razor
├── SmartMenuOptim.Shared/
│   └── Models/
│       └── Review/
│           ├── Review.cs
│           ├── ReviewMetrics.cs
│           └── ReviewAnalytics.cs
└── SmartMenuOptim.Tests/
    └── Review/
        └── ReviewTests.cs
```

## 1. Entity Definitions

### 1.1 Review Models
```csharp
// ReviewMetrics.cs
public class ReviewMetrics : EntityBase
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public int DishId { get; set; }
    public DateTime Date { get; set; }
    
    // Aggregated metrics
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public double AverageSentiment { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = [];
    
    // AI-enhanced metrics
    public List<string> CommonPhrases { get; set; } = [];
    public Dictionary<string, double> SentimentByAspect { get; set; } = [];
    public List<string> ImprovementSuggestions { get; set; } = [];
    
    // Navigation properties
    public virtual Restaurant Restaurant { get; set; } = null!;
    public virtual Dish Dish { get; set; } = null!;
}

// ReviewAnalytics.cs
public class ReviewAnalytics : EntityBase
{
    public int RestaurantId { get; set; }
    public DateTime Date { get; set; }
    
    // Overall metrics
    public double OverallSentiment { get; set; }
    public double ResponseRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    
    // Trend analysis
    public Dictionary<DateTime, double> SentimentTrend { get; set; } = [];
    public Dictionary<string, double> TopicAnalysis { get; set; } = [];
    
    // Customer segments
    public Dictionary<string, ReviewSegment> CustomerSegments { get; set; } = [];
}
```

## 2. Service Layer Implementation

### 2.1 Review Service Interface
```csharp
public interface IReviewService
{
    Task<Result<Review>> CreateReview(ReviewCreateDto dto);
    Task<Result<Review>> UpdateReview(ReviewUpdateDto dto);
    Task<Result<bool>> DeleteReview(int reviewId);
    Task<Result<ReviewMetrics>> GetReviewMetrics(int dishId);
    Task<Result<List<Review>>> GetDishReviews(
        int dishId, 
        ReviewFilterOptions filter);
    Task<Result<ReviewAnalytics>> GetRestaurantAnalytics(
        int restaurantId,
        DateTime startDate,
        DateTime endDate);
}

public class ReviewService : IReviewService
{
    private readonly AppDbContext _context;
    private readonly ITextAnalyticsClient _textAnalytics;
    private readonly ILogger<ReviewService> _logger;

    public async Task<Result<Review>> CreateReview(ReviewCreateDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Validate customer and dish existence
            var dish = await _context.Dishes
                .FirstOrDefaultAsync(d => d.Id == dto.DishId);
            if (dish == null)
                return Result<Review>.Failure("Dish not found");

            // Create review
            var review = new Review
            {
                DishId = dto.DishId,
                RestaurantId = dish.RestaurantId,
                CustomerId = dto.CustomerId,
                CustomerName = dto.CustomerName,
                Rating = dto.Rating,
                Comment = dto.Comment,
                DateCreated = DateTime.UtcNow
            };

            // Analyze sentiment
            review.SentimentScore = await AnalyzeSentiment(dto.Comment);

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Update analytics
            await UpdateReviewMetrics(review.DishId);

            return Result<Review>.Success(review);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating review");
            return Result<Review>.Failure("Failed to create review");
        }
    }

    private async Task<double> AnalyzeSentiment(string text)
    {
        try
        {
            var response = await _textAnalytics.AnalyzeSentimentAsync(text);
            return response.Value.Sentiment == TextSentiment.Positive ? 1.0 :
                   response.Value.Sentiment == TextSentiment.Negative ? 0.0 : 0.5;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing sentiment");
            return 0.5; // Neutral fallback
        }
    }
}
```

## 3. Blazor Components

### 3.1 Review List Component
```razor
@* Components/Review/ReviewList.razor *@
@inject IReviewService ReviewService
@inject ITextAnalyticsService TextAnalytics

<div class="review-list">
    <div class="filters">
        <div class="filter-group">
            <label>Rating:</label>
            <select @bind="filter.MinRating">
                <option value="0">All</option>
                @for (int i = 1; i <= 5; i++)
                {
                    <option value="@i">@i+ Stars</option>
                }
            </select>
        </div>

        <div class="filter-group">
            <label>Sort By:</label>
            <select @bind="filter.SortBy">
                <option value="Date">Latest</option>
                <option value="Rating">Highest Rated</option>
                <option value="Helpful">Most Helpful</option>
            </select>
        </div>
    </div>

    <div class="reviews-container">
        @foreach (var review in filteredReviews)
        {
            <ReviewCard Review="@review"
                       ShowResponse="@(review.Response != null)"
                       OnHelpful="@(() => MarkHelpful(review))" />
        }
    </div>

    @if (showLoadMore)
    {
        <button class="load-more" @onclick="LoadMore">
            Load More Reviews
        </button>
    }
</div>

@code {
    [Parameter]
    public int DishId { get; set; }

    private List<Review> reviews = [];
    private ReviewFilterOptions filter = new();
    private bool showLoadMore = true;
    private int pageSize = 10;
    private int currentPage = 1;

    protected override async Task OnInitializedAsync()
    {
        await LoadReviews();
    }

    private async Task LoadReviews()
    {
        var result = await ReviewService.GetDishReviews(
            DishId,
            new ReviewFilterOptions
            {
                Page = currentPage,
                PageSize = pageSize,
                MinRating = filter.MinRating,
                SortBy = filter.SortBy
            });

        if (result.IsSuccess)
        {
            reviews.AddRange(result.Value);
            showLoadMore = result.Value.Count == pageSize;
        }
    }
}
```

### 3.2 Review Analytics Component
```razor
@* Components/Review/ReviewAnalytics.razor *@
@inject IReviewService ReviewService

<div class="review-analytics">
    <div class="metrics-header">
        <h3>Review Insights</h3>
        <div class="date-range">
            <DateRangePicker @bind-StartDate="startDate"
                            @bind-EndDate="endDate"
                            OnRangeSelected="LoadAnalytics" />
        </div>
    </div>

    <div class="metrics-grid">
        <MetricCard Title="Overall Rating"
                   Value="@($"{metrics.AverageRating:F1}")"
                   Icon="star"
                   Trend="@ratingTrend" />
                   
        <MetricCard Title="Review Count"
                   Value="@metrics.TotalReviews.ToString()"
                   Icon="comment"
                   Trend="@reviewsTrend" />
                   
        <MetricCard Title="Sentiment Score"
                   Value="@($"{metrics.AverageSentiment:P0}")"
                   Icon="heart"
                   Trend="@sentimentTrend" />
    </div>

    <div class="charts-section">
        <div class="chart rating-distribution">
            <h4>Rating Distribution</h4>
            <BarChart Data="@ratingDistribution"
                     Options="@chartOptions" />
        </div>

        <div class="chart sentiment-trend">
            <h4>Sentiment Trend</h4>
            <LineChart Data="@sentimentData"
                      Options="@trendOptions" />
        </div>
    </div>

    <div class="insights-section">
        @foreach (var insight in aiInsights)
        {
            <InsightCard Insight="@insight"
                        OnActionClick="@(() => HandleInsightAction(insight))" />
        }
    </div>
</div>

@code {
    [Parameter]
    public int RestaurantId { get; set; }

    private ReviewMetrics metrics = new();
    private DateTime startDate = DateTime.Today.AddMonths(-1);
    private DateTime endDate = DateTime.Today;

    protected override async Task OnInitializedAsync()
    {
        await LoadAnalytics();
    }

    private async Task LoadAnalytics()
    {
        var result = await ReviewService.GetRestaurantAnalytics(
            RestaurantId, startDate, endDate);
            
        if (result.IsSuccess)
        {
            metrics = result.Value;
            UpdateCharts();
            GenerateInsights();
        }
    }
}
```

## 4. AI Integration

### 4.1 Review Analysis Service
```csharp
public class ReviewAnalysisService
{
    private readonly TextAnalyticsClient _textAnalytics;
    private readonly ILogger<ReviewAnalysisService> _logger;

    public async Task<ReviewAnalysis> AnalyzeReview(string reviewText)
    {
        try
        {
            // Run parallel analysis
            var tasks = new[]
            {
                AnalyzeSentiment(reviewText),
                ExtractKeyPhrases(reviewText),
                DetectLanguage(reviewText)
            };

            await Task.WhenAll(tasks);

            return new ReviewAnalysis
            {
                Sentiment = await tasks[0],
                KeyPhrases = await tasks[1],
                Language = await tasks[2],
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing review");
            throw;
        }
    }

    private async Task<double> AnalyzeSentiment(string text)
    {
        var response = await _textAnalytics.AnalyzeSentimentAsync(text);
        return response.Value.ConfidenceScores.Positive;
    }
}
```

### 4.2 Review Insights Service
```csharp
public class ReviewInsightsService
{
    private readonly AppDbContext _context;
    private readonly IReviewAnalysisService _analysisService;

    public async Task<List<ReviewInsight>> GenerateInsights(
        int restaurantId, 
        DateTime startDate,
        DateTime endDate)
    {
        var reviews = await _context.Reviews
            .Where(r => r.RestaurantId == restaurantId &&
                       r.DateCreated >= startDate &&
                       r.DateCreated <= endDate)
            .ToListAsync();

        var insights = new List<ReviewInsight>();

        // Analyze sentiment trends
        var sentimentTrend = AnalyzeSentimentTrend(reviews);
        if (sentimentTrend.IsSignificant)
        {
            insights.Add(new ReviewInsight
            {
                Type = InsightType.SentimentTrend,
                Title = "Sentiment Trend Alert",
                Description = sentimentTrend.Description,
                Severity = sentimentTrend.Severity,
                RecommendedActions = sentimentTrend.Actions
            });
        }

        // Analyze common themes
        var themes = await AnalyzeCommonThemes(reviews);
        foreach (var theme in themes)
        {
            insights.Add(new ReviewInsight
            {
                Type = InsightType.ThemeDetection,
                Title = $"Common Theme: {theme.Name}",
                Description = theme.Description,
                Severity = theme.Sentiment < 0.4 ? 
                    InsightSeverity.High : 
                    InsightSeverity.Low
            });
        }

        return insights;
    }
}
```

## 5. Background Services

### 5.1 Review Processing Service
```csharp
public class ReviewProcessingService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ReviewProcessingService> _logger;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var reviewService = scope.ServiceProvider
                    .GetRequiredService<IReviewService>();
                var analysisService = scope.ServiceProvider
                    .GetRequiredService<IReviewAnalysisService>();

                // Process unanalyzed reviews
                await ProcessPendingReviews(
                    reviewService, 
                    analysisService);

                // Generate insights
                await GenerateInsights(reviewService);

                // Wait for next processing window
                await Task.Delay(TimeSpan.FromMinutes(15), 
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error processing reviews");
                await Task.Delay(TimeSpan.FromMinutes(1), 
                    stoppingToken);
            }
        }
    }
}
```

## Implementation Checklist

### Phase 1: Core Features
- [ ] Review creation/management
- [ ] Basic analytics
- [ ] Sentiment analysis
- [ ] Response handling

### Phase 2: Advanced Features
- [ ] AI-powered insights
- [ ] Theme detection
- [ ] Trend analysis
- [ ] Automated responses

### Phase 3: Integration
- [ ] Customer notifications
- [ ] Restaurant alerts
- [ ] Analytics integration
- [ ] Mobile support

### Phase 4: Enhancement
- [ ] Review moderation
- [ ] Spam detection
- [ ] Language translation
- [ ] Review rewards

## Monitoring and Maintenance

### Key Metrics
1. Review volume
2. Response times
3. Sentiment trends
4. AI accuracy

### Regular Tasks
1. Content moderation
2. AI model updates
3. Analytics processing
4. Data cleanup

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0   | TBD  | Initial implementation |
| 1.1.0   | TBD  | AI integration |
| 1.2.0   | TBD  | Advanced analytics |