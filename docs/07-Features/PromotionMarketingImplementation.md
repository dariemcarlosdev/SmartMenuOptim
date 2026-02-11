# Promotion & Marketing System Implementation Guide

## Overview
This guide outlines the implementation of the Smart Menu Optimization Promotion & Marketing System, integrating with Customer, Restaurant, and Analytics systems in a Blazor-based architecture.

## Project Structure Integration
```
SmartMenuOptim/
├── SmartMenuOptim.API/
│   └── Controllers/
│       ├── CampaignController.cs
│       ├── PromotionController.cs
│       └── MarketingAnalyticsController.cs
├── SmartMenuOptim.Server/
│   └── Components/
│       └── Marketing/
│           ├── CampaignDashboard.razor
│           ├── PromotionEditor.razor
│           ├── MarketingCalendar.razor
│           └── CampaignAnalytics.razor
├── SmartMenuOptim.Shared/
│   └── Models/
│       └── Marketing/
│           ├── Campaign.cs
│           ├── Promotion.cs
│           ├── MarketingMetrics.cs
│           └── CampaignTarget.cs
└── SmartMenuOptim.Tests/
    └── Marketing/
        └── MarketingTests.cs
```

## 1. Entity Definitions

### 1.1 Marketing Models
```csharp
// Campaign.cs
public class Campaign : TenantEntityBase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public CampaignStatus Status { get; set; }
    public CampaignType Type { get; set; }
    public string? TargetAudience { get; set; }
    public decimal Budget { get; set; }
    public decimal ActualSpend { get; set; }
    
    // Campaign metrics
    public int Impressions { get; set; }
    public int Engagements { get; set; }
    public int Conversions { get; set; }
    public decimal ROI { get; set; }
    
    // Navigation properties
    public virtual ICollection<Promotion> Promotions { get; set; } = [];
    public virtual ICollection<CampaignTarget> Targets { get; set; } = [];
}

// Promotion.cs
public class Promotion : TenantEntityBase
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PromotionType Type { get; set; }
    public decimal DiscountValue { get; set; }
    public bool IsPercentage { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int? MaxUses { get; set; }
    public int UsageCount { get; set; }
    public decimal MinimumOrderValue { get; set; }
    public bool IsActive { get; set; }
    
    // Targeting
    public LoyaltyTier? MinimumTier { get; set; }
    public string? TargetSegment { get; set; }
    
    // Navigation properties
    public virtual Campaign Campaign { get; set; } = null!;
    public virtual ICollection<Order> Orders { get; set; } = [];
}

// MarketingMetrics.cs
public class MarketingMetrics : TenantEntityBase
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    
    // Campaign performance
    public Dictionary<int, CampaignMetrics> CampaignMetrics { get; set; } = [];
    
    // Customer engagement
    public int NewCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public double EngagementRate { get; set; }
    
    // Promotion metrics
    public int PromotionRedemptions { get; set; }
    public decimal TotalDiscountValue { get; set; }
    public double ConversionRate { get; set; }
    
    // ROI metrics
    public decimal MarketingSpend { get; set; }
    public decimal AttributedRevenue { get; set; }
    public decimal ROI { get; set; }
}
```

## 2. Service Layer Implementation

### 2.1 Campaign Service
```csharp
public interface ICampaignService
{
    Task<Result<Campaign>> CreateCampaign(CampaignCreateDto dto);
    Task<Result<Campaign>> UpdateCampaign(CampaignUpdateDto dto);
    Task<Result<List<Campaign>>> GetActiveCampaigns(int restaurantId);
    Task<Result<CampaignMetrics>> GetCampaignMetrics(int campaignId);
    Task<Result<bool>> ActivateCampaign(int campaignId);
    Task<Result<bool>> DeactivateCampaign(int campaignId);
}

public class CampaignService : ICampaignService
{
    private readonly AppDbContext _context;
    private readonly IMarketingAnalyticsService _analyticsService;
    private readonly ILogger<CampaignService> _logger;

    public async Task<Result<Campaign>> CreateCampaign(
        CampaignCreateDto dto)
    {
        using var transaction = 
            await _context.Database.BeginTransactionAsync();
        try
        {
            var campaign = new Campaign
            {
                Name = dto.Name,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Type = dto.Type,
                TargetAudience = dto.TargetAudience,
                Budget = dto.Budget,
                RestaurantId = dto.RestaurantId,
                Status = CampaignStatus.Draft
            };

            // Create associated promotions
            foreach (var promoDto in dto.Promotions)
            {
                campaign.Promotions.Add(new Promotion
                {
                    Code = GeneratePromoCode(),
                    Description = promoDto.Description,
                    Type = promoDto.Type,
                    DiscountValue = promoDto.DiscountValue,
                    IsPercentage = promoDto.IsPercentage,
                    ValidFrom = campaign.StartDate,
                    ValidTo = campaign.EndDate,
                    MinimumOrderValue = promoDto.MinimumOrderValue,
                    RestaurantId = dto.RestaurantId
                });
            }

            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result<Campaign>.Success(campaign);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating campaign");
            return Result<Campaign>.Failure(
                "Failed to create campaign");
        }
    }
}
```

### 2.2 Promotion Service
```csharp
public interface IPromotionService
{
    Task<Result<bool>> ValidatePromotion(
        string code, 
        int restaurantId, 
        int? customerId, 
        decimal orderValue);
    Task<Result<decimal>> CalculateDiscount(
        string code, 
        decimal orderValue);
    Task<Result<List<Promotion>>> GetActivePromotions(
        int restaurantId, 
        int? customerId);
}

public class PromotionService : IPromotionService
{
    private readonly AppDbContext _context;
    private readonly ILoyaltyService _loyaltyService;
    private readonly ILogger<PromotionService> _logger;

    public async Task<Result<bool>> ValidatePromotion(
        string code, 
        int restaurantId, 
        int? customerId, 
        decimal orderValue)
    {
        try
        {
            var promotion = await _context.Promotions
                .Include(p => p.Campaign)
                .FirstOrDefaultAsync(p => 
                    p.Code == code && 
                    p.RestaurantId == restaurantId &&
                    p.IsActive);

            if (promotion == null)
                return Result<bool>.Failure("Invalid promotion code");

            // Validate dates
            if (DateTime.UtcNow < promotion.ValidFrom || 
                DateTime.UtcNow > promotion.ValidTo)
                return Result<bool>.Failure("Promotion has expired");

            // Validate usage limit
            if (promotion.MaxUses.HasValue && 
                promotion.UsageCount >= promotion.MaxUses.Value)
                return Result<bool>.Failure("Promotion limit reached");

            // Validate minimum order value
            if (orderValue < promotion.MinimumOrderValue)
                return Result<bool>.Failure(
                    "Order value too low for this promotion");

            // Validate customer eligibility
            if (customerId.HasValue && 
                promotion.MinimumTier.HasValue)
            {
                var loyalty = await _loyaltyService
                    .GetCustomerLoyalty(customerId.Value, restaurantId);
                if (!loyalty.IsSuccess || 
                    loyalty.Value.Tier < promotion.MinimumTier.Value)
                    return Result<bool>.Failure(
                        "Customer not eligible for this promotion");
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promotion");
            return Result<bool>.Failure(
                "Failed to validate promotion");
        }
    }
}
```

## 3. Blazor Components

### 3.1 Campaign Dashboard Component
```razor
@* Components/Marketing/CampaignDashboard.razor *@
@inject ICampaignService CampaignService
@inject IMarketingAnalyticsService AnalyticsService

<div class="campaign-dashboard">
    <div class="dashboard-header">
        <h2>Marketing Campaigns</h2>
        <button class="btn btn-primary" @onclick="CreateCampaign">
            New Campaign
        </button>
    </div>

    <div class="campaign-stats">
        <MetricCard Title="Active Campaigns"
                   Value="@activeCampaigns.Count"
                   Icon="bullhorn" />
        <MetricCard Title="Total Revenue"
                   Value="@totalRevenue.ToString("C")"
                   Icon="dollar-sign" />
        <MetricCard Title="Average ROI"
                   Value="@($"{averageROI:P2}")"
                   Icon="chart-line" />
    </div>

    <div class="campaigns-grid">
        @foreach (var campaign in activeCampaigns)
        {
            <CampaignCard Campaign="@campaign"
                         OnEdit="@(() => EditCampaign(campaign))"
                         OnDeactivate="@(() => DeactivateCampaign(campaign))" />
        }
    </div>

    @if (showCampaignEditor)
    {
        <CampaignEditor Campaign="@selectedCampaign"
                        OnSave="@HandleCampaignSave"
                        OnCancel="@(() => showCampaignEditor = false)" />
    }
</div>

@code {
    [Parameter]
    public int RestaurantId { get; set; }

    private List<Campaign> activeCampaigns = [];
    private decimal totalRevenue;
    private double averageROI;
    private bool showCampaignEditor;
    private Campaign? selectedCampaign;

    protected override async Task OnInitializedAsync()
    {
        await LoadCampaigns();
        await LoadMetrics();
    }

    private async Task LoadCampaigns()
    {
        var result = await CampaignService
            .GetActiveCampaigns(RestaurantId);
        if (result.IsSuccess)
        {
            activeCampaigns = result.Value;
        }
    }
}
```

### 3.2 Marketing Calendar Component
```razor
@* Components/Marketing/MarketingCalendar.razor *@
@inject ICampaignService CampaignService
@inject IPromotionService PromotionService

<div class="marketing-calendar">
    <div class="calendar-header">
        <div class="month-navigation">
            <button @onclick="PreviousMonth">&lt;</button>
            <h3>@currentDate.ToString("MMMM yyyy")</h3>
            <button @onclick="NextMonth">&gt;</button>
        </div>
    </div>

    <div class="calendar-grid">
        @foreach (var week in GetWeeks())
        {
            <div class="calendar-week">
                @foreach (var day in week)
                {
                    <div class="calendar-day @GetDayClass(day)">
                        <div class="day-header">
                            @day.Day
                        </div>
                        <div class="day-content">
                            @foreach (var campaign in GetCampaignsForDay(day))
                            {
                                <div class="calendar-event"
                                     @onclick="() => ShowCampaignDetails(campaign)">
                                    @campaign.Name
                                </div>
                            }
                        </div>
                    </div>
                }
            </div>
        }
    </div>

    @if (selectedCampaign != null)
    {
        <CampaignDetailsDialog Campaign="@selectedCampaign"
                              OnClose="@(() => selectedCampaign = null)" />
    }
</div>

@code {
    [Parameter]
    public int RestaurantId { get; set; }

    private DateTime currentDate = DateTime.Today;
    private List<Campaign> campaigns = [];
    private Campaign? selectedCampaign;

    protected override async Task OnInitializedAsync()
    {
        await LoadCampaigns();
    }

    private async Task LoadCampaigns()
    {
        var startDate = new DateTime(
            currentDate.Year, 
            currentDate.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var result = await CampaignService.GetCampaigns(
            RestaurantId, startDate, endDate);
        if (result.IsSuccess)
        {
            campaigns = result.Value;
        }
    }
}
```

## 4. Background Services

### 4.1 Marketing Automation Service
```csharp
public class MarketingAutomationService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MarketingAutomationService> _logger;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var campaignService = scope.ServiceProvider
                    .GetRequiredService<ICampaignService>();
                var analyticsService = scope.ServiceProvider
                    .GetRequiredService<IMarketingAnalyticsService>();

                // Process scheduled campaigns
                await ProcessScheduledCampaigns(campaignService);

                // Update campaign metrics
                await UpdateCampaignMetrics(
                    campaignService, 
                    analyticsService);

                // Generate reports
                await GenerateMarketingReports(analyticsService);

                await Task.Delay(TimeSpan.FromMinutes(15), 
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error in marketing automation");
                await Task.Delay(TimeSpan.FromMinutes(1), 
                    stoppingToken);
            }
        }
    }
}
```

## Implementation Checklist

### Phase 1: Core Features
- [ ] Campaign management
- [ ] Promotion system
- [ ] Basic analytics
- [ ] Calendar planning

### Phase 2: Advanced Features
- [ ] Automated campaigns
- [ ] Customer targeting
- [ ] A/B testing
- [ ] ROI tracking

### Phase 3: Integration
- [ ] Customer profiles
- [ ] Order system
- [ ] Loyalty program
- [ ] Analytics platform

### Phase 4: Enhancement
- [ ] AI recommendations
- [ ] Predictive analytics
- [ ] Multi-channel campaigns
- [ ] Advanced reporting

## Monitoring and Maintenance

### Key Metrics
1. Campaign performance
2. Promotion usage
3. Customer engagement
4. Marketing ROI

### Regular Tasks
1. Campaign updates
2. Performance analysis
3. Customer segmentation
4. Data cleanup

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0   | TBD  | Initial implementation |
| 1.1.0   | TBD  | Advanced targeting |
| 1.2.0   | TBD  | AI integration |