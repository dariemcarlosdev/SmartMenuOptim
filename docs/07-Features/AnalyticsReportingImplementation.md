# Analytics & Reporting System Implementation Guide

## Overview
This guide outlines the implementation of the Smart Menu Optimization Analytics & Reporting System, leveraging Azure services and AI capabilities while integrating with Profile, Restaurant, Order, Loyalty, and Reservation systems in a Blazor-based architecture.

## Azure Services Integration
```json
{
    "azure": {
        "synapse": {
            "name": "smo-analytics-synapse",
            "configuration": {
                "serverless": true,
                "dataLake": "smoanalyticsdl",
                "sqlPool": "smoreportingpool"
            }
        },
        "applicationInsights": {
            "name": "smo-insights",
            "features": [
                "userBehaviorAnalytics",
                "performanceMonitoring",
                "customMetrics"
            ]
        },
        "cognitiveServices": {
            "name": "smo-cognitive",
            "services": [
                {
                    "type": "anomalyDetector",
                    "sku": "S0"
                },
                {
                    "type": "textAnalytics",
                    "sku": "S0"
                }
            ]
        },
        "powerBI": {
            "workspaceName": "smo-analytics",
            "embeddedCapacity": "A1",
            "datasets": [
                "restaurantAnalytics",
                "customerInsights",
                "salesPerformance"
            ]
        }
    }
}
```

## Project Structure Integration
```
SmartMenuOptim/
├── SmartMenuOptim.API/
│   └── Controllers/
│       ├── AnalyticsController.cs
│       └── ReportController.cs
├── SmartMenuOptim.Server/
│   └── Components/
│       └── Analytics/
│           ├── DashboardHub.razor
│           ├── SalesAnalytics.razor
│           ├── CustomerInsights.razor
│           └── ReportViewer.razor
├── SmartMenuOptim.Shared/
│   └── Models/
│       └── Analytics/
│           ├── SalesMetrics.cs
│           ├── CustomerMetrics.cs
│           └── PerformanceMetrics.cs
└── SmartMenuOptim.Tests/
    └── Analytics/
        └── AnalyticsTests.cs
```

## 1. Analytics Models

### 1.1 Core Metrics Models
```csharp
// SalesMetrics.cs
public class SalesMetrics : EntityBase
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal PeakHourSales { get; set; }
    public TimeSpan PeakHourTime { get; set; }
    public Dictionary<string, decimal> CategorySales { get; set; } = [];
    public Dictionary<int, decimal> HourlySales { get; set; } = [];
    
    // Navigation properties
    public virtual Restaurant Restaurant { get; set; } = null!;
}

// CustomerMetrics.cs
public class CustomerMetrics : EntityBase
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public DateTime Date { get; set; }
    public int NewCustomers { get; set; }
    public int RepeatCustomers { get; set; }
    public decimal CustomerRetentionRate { get; set; }
    public decimal AverageCustomerSpending { get; set; }
    public int LoyaltyProgramEnrollments { get; set; }
    public Dictionary<LoyaltyTier, int> CustomersByTier { get; set; } = [];
    
    // Navigation properties
    public virtual Restaurant Restaurant { get; set; } = null!;
}

// PerformanceMetrics.cs
public class PerformanceMetrics : EntityBase
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public DateTime Date { get; set; }
    public decimal TableTurnoverRate { get; set; }
    public TimeSpan AverageServiceTime { get; set; }
    public int PeakHourCapacity { get; set; }
    public decimal TableUtilizationRate { get; set; }
    public int NoShowCount { get; set; }
    public Dictionary<string, int> StaffProductivity { get; set; } = [];
    
    // Navigation properties
    public virtual Restaurant Restaurant { get; set; } = null!;
}
```

### 1.2 Report Models
```csharp
// ReportDefinition.cs
public class ReportDefinition : EntityBase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public ReportSchedule? Schedule { get; set; }
    public List<ReportParameter> Parameters { get; set; } = [];
    public string Query { get; set; } = string.Empty;
    public string? EmailRecipients { get; set; }
}

public enum ReportType
{
    Sales,
    Customer,
    Inventory,
    Staff,
    Financial,
    Custom
}

public class ReportParameter
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ParameterType Type { get; set; }
    public string? DefaultValue { get; set; }
    public bool Required { get; set; }
}
```

## 2. Analytics Services

### 2.1 Sales Analytics Service
```csharp
public interface ISalesAnalyticsService
{
    Task<Result<SalesMetrics>> GetDailySalesMetrics(int restaurantId, DateTime date);
    Task<Result<List<SalesMetrics>>> GetSalesHistory(
        int restaurantId, 
        DateTime startDate, 
        DateTime endDate);
    Task<Result<Dictionary<string, decimal>>> GetTopSellingCategories(
        int restaurantId, 
        DateTime date);
    Task<Result<Dictionary<string, decimal>>> GetRevenueByHour(
        int restaurantId, 
        DateTime date);
}

public class SalesAnalyticsService : ISalesAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SalesAnalyticsService> _logger;
    
    public async Task<Result<SalesMetrics>> GetDailySalesMetrics(
        int restaurantId, 
        DateTime date)
    {
        try
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.RestaurantId == restaurantId &&
                           o.OrderDate.Date == date.Date)
                .ToListAsync();

            var metrics = new SalesMetrics
            {
                RestaurantId = restaurantId,
                Date = date,
                TotalSales = orders.Sum(o => o.TotalAmount),
                OrderCount = orders.Count,
                AverageOrderValue = orders.Any() 
                    ? orders.Average(o => o.TotalAmount) 
                    : 0
            };

            // Calculate hourly sales
            metrics.HourlySales = orders
                .GroupBy(o => o.OrderDate.Hour)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(o => o.TotalAmount)
                );

            // Find peak hour
            if (metrics.HourlySales.Any())
            {
                var peakHour = metrics.HourlySales
                    .OrderByDescending(kv => kv.Value)
                    .First();
                    
                metrics.PeakHourTime = new TimeSpan(peakHour.Key, 0, 0);
                metrics.PeakHourSales = peakHour.Value;
            }

            return Result<SalesMetrics>.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating sales metrics");
            return Result<SalesMetrics>.Failure(
                "Failed to calculate sales metrics");
        }
    }
}
```

### 2.2 Customer Analytics Service
```csharp
public interface ICustomerAnalyticsService
{
    Task<Result<CustomerMetrics>> GetCustomerMetrics(
        int restaurantId, 
        DateTime date);
    Task<Result<Dictionary<string, int>>> GetCustomerSegmentation(
        int restaurantId);
    Task<Result<List<Customer>>> GetTopCustomers(
        int restaurantId, 
        int count = 10);
}

public class CustomerAnalyticsService : ICustomerAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly ILoyaltyService _loyaltyService;
    
    public async Task<Result<CustomerMetrics>> GetCustomerMetrics(
        int restaurantId, 
        DateTime date)
    {
        try
        {
            var previousDate = date.AddDays(-1);
            
            var todayOrders = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.RestaurantId == restaurantId && 
                           o.OrderDate.Date == date.Date)
                .ToListAsync();

            var previousCustomers = await _context.Orders
                .Where(o => o.RestaurantId == restaurantId && 
                           o.OrderDate.Date < date.Date)
                .Select(o => o.CustomerId)
                .Distinct()
                .ToListAsync();

            var metrics = new CustomerMetrics
            {
                RestaurantId = restaurantId,
                Date = date,
                NewCustomers = todayOrders
                    .Where(o => o.CustomerId.HasValue && 
                               !previousCustomers.Contains(o.CustomerId.Value))
                    .Select(o => o.CustomerId)
                    .Distinct()
                    .Count(),
                RepeatCustomers = todayOrders
                    .Where(o => o.CustomerId.HasValue && 
                               previousCustomers.Contains(o.CustomerId.Value))
                    .Select(o => o.CustomerId)
                    .Distinct()
                    .Count()
            };

            // Calculate retention rate
            metrics.CustomerRetentionRate = previousCustomers.Any()
                ? (decimal)metrics.RepeatCustomers / previousCustomers.Count
                : 0;

            return Result<CustomerMetrics>.Success(metrics);
        }
        catch (Exception ex)
        {
            return Result<CustomerMetrics>.Failure(
                "Failed to calculate customer metrics");
        }
    }
}
```

## 3. Blazor Components

### 3.1 Analytics Dashboard Component
```razor
@* Components/Analytics/DashboardHub.razor *@
@inject ISalesAnalyticsService SalesAnalytics
@inject ICustomerAnalyticsService CustomerAnalytics
@implements IDisposable

<div class="analytics-dashboard">
    <div class="metrics-header">
        <h2>Restaurant Analytics</h2>
        <div class="date-range">
            <DateRangePicker @bind-StartDate="startDate"
                            @bind-EndDate="endDate"
                            OnRangeSelected="LoadMetrics" />
        </div>
    </div>

    <div class="metrics-grid">
        <div class="metric-card sales">
            <h3>Sales Overview</h3>
            <MetricChart Data="@salesData"
                        Type="ChartType.Line"
                        Options="@salesChartOptions" />
            <div class="key-metrics">
                <MetricDisplay Label="Total Sales"
                             Value="@totalSales.ToString("C")"
                             Trend="@salesTrend" />
                <MetricDisplay Label="Average Order"
                             Value="@averageOrder.ToString("C")"
                             Trend="@orderTrend" />
            </div>
        </div>

        <div class="metric-card customers">
            <h3>Customer Insights</h3>
            <MetricChart Data="@customerData"
                        Type="ChartType.Doughnut"
                        Options="@customerChartOptions" />
            <div class="key-metrics">
                <MetricDisplay Label="New Customers"
                             Value="@newCustomers.ToString()"
                             Trend="@customerTrend" />
                <MetricDisplay Label="Retention Rate"
                             Value="@(retentionRate.ToString("P"))"
                             Trend="@retentionTrend" />
            </div>
        </div>
    </div>
</div>

@code {
    [Parameter]
    public int RestaurantId { get; set; }

    private DateTime startDate = DateTime.Today.AddDays(-30);
    private DateTime endDate = DateTime.Today;
    private HubConnection? hubConnection;

    protected override async Task OnInitializedAsync()
    {
        await LoadMetrics();
        await SetupRealTimeUpdates();
    }

    private async Task LoadMetrics()
    {
        var salesTask = SalesAnalytics.GetSalesHistory(
            RestaurantId, startDate, endDate);
        var customerTask = CustomerAnalytics.GetCustomerMetrics(
            RestaurantId, endDate);

        await Task.WhenAll(salesTask, customerTask);
        
        if (salesTask.Result.IsSuccess)
        {
            UpdateSalesMetrics(salesTask.Result.Value);
        }
        
        if (customerTask.Result.IsSuccess)
        {
            UpdateCustomerMetrics(customerTask.Result.Value);
        }
    }
}
```

### 3.2 Report Viewer Component
```razor
@* Components/Analytics/ReportViewer.razor *@
@inject IReportService ReportService

<div class="report-viewer">
    <div class="report-header">
        <h2>@Report?.Name</h2>
        <div class="report-controls">
            <button @onclick="ExportToPdf">
                Export PDF
            </button>
            <button @onclick="ExportToExcel">
                Export Excel
            </button>
        </div>
    </div>

    <div class="report-parameters">
        @if (Report?.Parameters != null)
        {
            <EditForm Model="@parameterValues" OnValidSubmit="@RunReport">
                @foreach (var param in Report.Parameters)
                {
                    <div class="parameter-input">
                        <label>@param.DisplayName</label>
                        @switch (param.Type)
                        {
                            case ParameterType.Date:
                                <InputDate @bind-Value="parameterValues[param.Name]" />
                                break;
                            case ParameterType.Number:
                                <InputNumber @bind-Value="parameterValues[param.Name]" />
                                break;
                            default:
                                <InputText @bind-Value="parameterValues[param.Name]" />
                                break;
                        }
                    </div>
                }
                <button type="submit">Run Report</button>
            </EditForm>
        }
    </div>

    <div class="report-content">
        @if (loading)
        {
            <LoadingSpinner />
        }
        else if (reportData != null)
        {
            <DynamicTable Data="@reportData"
                         Columns="@reportColumns" />
        }
    </div>
</div>

@code {
    [Parameter]
    public ReportDefinition? Report { get; set; }

    private Dictionary<string, object> parameterValues = new();
    private bool loading;
    private object? reportData;
    private List<ColumnDefinition> reportColumns = [];


    private async Task RunReport()
    {
        if (Report == null) return;
        
        loading = true;
        try
        {
            var result = await ReportService.RunReport(
                Report.Id, 
                parameterValues);
                
            if (result.IsSuccess)
            {
                reportData = result.Value;
                reportColumns = await GetColumnsFromData(result.Value);
            }
        }
        finally
        {
            loading = false;
        }
    }
}
```

## 4. Data Processing

### 4.1 Analytics Processing Service
```csharp
public class AnalyticsProcessingService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AnalyticsProcessingService> _logger;
    
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var analyticsService = scope.ServiceProvider
                    .GetRequiredService<IAnalyticsService>();
                    
                await analyticsService.ProcessDailyMetrics();
                await analyticsService.UpdateTrends();
                await analyticsService.CleanupOldMetrics();
                
                // Run daily at 3 AM
                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddDays(1).AddHours(3);
                var delay = nextRun - now;
                
                await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error processing analytics");
                await Task.Delay(TimeSpan.FromMinutes(15), 
                    stoppingToken);
            }
        }
    }
}
```

### 4.2 Report Generation Service
```csharp
public class ReportGenerationService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    
    public async Task GenerateScheduledReports()
    {
        var reports = await _context.ReportDefinitions
            .Where(r => r.Schedule != null && 
                       r.Schedule.NextRunTime <= DateTime.UtcNow)
            .ToListAsync();
            
        foreach (var report in reports)
        {
            var result = await GenerateReport(report);
            if (result.IsSuccess && !string.IsNullOrEmpty(report.EmailRecipients))
            {
                await _emailService.SendReportEmail(
                    report.EmailRecipients,
                    report.Name,
                    result.Value);
            }
            
            // Update next run time
            UpdateNextRunTime(report);
        }
        
        await _context.SaveChangesAsync();
    }
}
```

## 5. Real-time Analytics

### 5.1 Analytics Hub
```csharp
public class AnalyticsHub : Hub
{
    public async Task JoinRestaurantGroup(int restaurantId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId, 
            $"restaurant_{restaurantId}");
    }

    public async Task UpdateMetrics(int restaurantId, MetricsUpdate update)
    {
        await Clients.Group($"restaurant_{restaurantId}")
            .SendAsync("MetricsUpdated", update);
    }
}
```

## AI Integration

### 6.1 Anomaly Detection
```csharp
public class AnomalyDetectionService
{
    private readonly CognitiveServicesClient _cognitiveClient;
    private readonly ILogger<AnomalyDetectionService> _logger;
    
    public async Task<Result<bool>> DetectSalesAnomalies(
        int restaurantId, 
        DateTime date)
    {
        try
        {
            // Fetch sales data
            var salesMetrics = await _context.SalesMetrics
                .Where(sm => sm.RestaurantId == restaurantId && 
                            sm.Date == date)
                .ToListAsync();

            // Call Anomaly Detector API
            var response = await _cognitiveClient.AnomalyDetector
                .DetectChangesAsync("SalesData", salesMetrics);

            // Analyze response
            var isAnomalous = response.IsAnomaly;
            
            return Result<bool>.Success(isAnomalous);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting anomalies");
            return Result<bool>.Failure("Anomaly detection failed");
        }
    }
}
```

### 6.2 Customer Sentiment Analysis
```csharp
public class SentimentAnalysisService
{
    private readonly CognitiveServicesClient _cognitiveClient;
    private readonly ILogger<SentimentAnalysisService> _logger;
    
    public async Task<Result<decimal>> AnalyzeCustomerSentiment(
        int restaurantId, 
        DateTime date)
    {
        try
        {
            // Fetch customer feedback
            var feedbacks = await _context.CustomerFeedbacks
                .Where(cf => cf.RestaurantId == restaurantId && 
                            cf.FeedbackDate.Date == date.Date)
                .ToListAsync();

            var sentiments = new List<decimal>();
            
            foreach (var feedback in feedbacks)
            {
                // Call Text Analytics API
                var response = await _cognitiveClient.TextAnalytics
                    .AnalyzeSentimentAsync(feedback.Comment);

                sentiments.Add(response.ConfidenceScores.Positive);
            }

            // Calculate average sentiment
            var averageSentiment = sentiments.Any()
                ? sentiments.Average()
                : 0;

            return Result<decimal>.Success(averageSentiment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sentiment");
            return Result<decimal>.Failure("Sentiment analysis failed");
        }
    }
}
```

## Power BI Integration
```csharp
public class PowerBIIntegrationService
{
    private readonly PowerBIClient _powerBIClient;
    private readonly ILogger<PowerBIIntegrationService> _logger;
    
    public async Task<Result<string>> EmbedPowerBIReport(
        int restaurantId,
        string reportName)
    {
        try
        {
            // Fetch report details
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.RestaurantId == restaurantId && 
                                         r.Name == reportName);

            if (report == null)
            {
                return Result<string>.Failure("Report not found");
            }

            // Generate embed token
            var token = await _powerBIClient.Reports
                .GetReportEmbedTokenAsync(reportId);

            return Result<string>.Success(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error embedding Power BI report");
            return Result<string>.Failure("Power BI integration failed");
        }
    }
}
```

## Implementation Checklist

### Phase 1: Core Analytics
- [ ] Sales metrics implementation
- [ ] Customer analytics
- [ ] Performance tracking
- [ ] Basic reporting

### Phase 2: Advanced Features
- [ ] Real-time analytics
- [ ] Custom reports
- [ ] Trend analysis
- [ ] Predictive analytics

### Phase 3: Integration
- [ ] Order system integration
- [ ] Loyalty system integration
- [ ] Inventory tracking
- [ ] Staff performance

### Phase 4: Optimization
- [ ] Data aggregation
- [ ] Query optimization
- [ ] Cache implementation
- [ ] Archive strategy

## Monitoring and Maintenance

### Key Metrics
1. Data processing time
2. Report generation speed
3. Query performance
4. Storage utilization

### Regular Tasks
1. Data aggregation
2. Performance monitoring
3. Report scheduling
4. Data cleanup

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0   | TBD  | Initial implementation |
| 1.1.0   | TBD  | Real-time analytics |
| 1.2.0   | TBD  | Advanced reporting |