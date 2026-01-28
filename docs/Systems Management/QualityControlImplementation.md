# Quality Control System Implementation Guide

## Overview
This guide outlines the implementation of the Smart Menu Optimization Quality Control System, integrating with Restaurant, Order, Inventory, and Analytics systems in a Blazor-based architecture.

## Project Structure Integration
```
SmartMenuOptim/
??? SmartMenuOptim.API/
?   ??? Controllers/
?       ??? QualityController.cs
?       ??? InspectionController.cs
?       ??? SafetyController.cs
??? SmartMenuOptim.Server/
?   ??? Components/
?       ??? Quality/
?           ??? QualityDashboard.razor
?           ??? InspectionManager.razor
?           ??? SafetyChecklist.razor
?           ??? QualityMetrics.razor
??? SmartMenuOptim.Shared/
?   ??? Models/
?       ??? Quality/
?           ??? Inspection.cs
?           ??? QualityCheck.cs
?           ??? SafetyReport.cs
?           ??? ComplianceMetric.cs
??? SmartMenuOptim.Tests/
    ??? Quality/
        ??? QualityTests.cs
```

## 1. Entity Definitions

### 1.1 Quality Control Models
```csharp
// Inspection.cs
public class Inspection : TenantEntityBase
{
    public int Id { get; set; }
    public DateTime InspectionDate { get; set; }
    public InspectionType Type { get; set; }
    public InspectionStatus Status { get; set; }
    public int InspectorId { get; set; }
    public string? Notes { get; set; }
    public bool IsUrgent { get; set; }
    public DateTime? ResolutionDate { get; set; }
    
    // Scoring
    public int Score { get; set; }
    public Dictionary<string, int> CategoryScores { get; set; } = [];
    
    // Compliance
    public bool IsCompliant { get; set; }
    public List<string> ViolationCodes { get; set; } = [];
    public string? RemediationPlan { get; set; }
    
    // Navigation properties
    public virtual StaffMember Inspector { get; set; } = null!;
    public virtual ICollection<QualityCheck> QualityChecks { get; set; } = [];
    public virtual ICollection<InspectionImage> Images { get; set; } = [];
}

// QualityCheck.cs
public class QualityCheck : TenantEntityBase
{
    public int Id { get; set; }
    public int InspectionId { get; set; }
    public QualityCheckType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Criterion { get; set; } = string.Empty;
    public int Score { get; set; }
    public string? Comments { get; set; }
    public List<string> Issues { get; set; } = [];
    public bool RequiresAction { get; set; }
    public string? ActionTaken { get; set; }
    
    // Optional equipment/item references
    public int? EquipmentId { get; set; }
    public int? IngredientId { get; set; }
    public int? DishId { get; set; }
    
    // Navigation properties
    public virtual Inspection Inspection { get; set; } = null!;
    public virtual Equipment? Equipment { get; set; }
    public virtual Ingredient? Ingredient { get; set; }
    public virtual Dish? Dish { get; set; }
}

// SafetyReport.cs
public class SafetyReport : TenantEntityBase
{
    public int Id { get; set; }
    public DateTime ReportDate { get; set; }
    public SafetyCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RiskLevel Risk { get; set; }
    public bool RequiresImmediate { get; set; }
    public string? ActionTaken { get; set; }
    public int ReportedById { get; set; }
    public SafetyStatus Status { get; set; }
    
    // Incident details (if applicable)
    public DateTime? IncidentDate { get; set; }
    public string? IncidentLocation { get; set; }
    public List<string> AffectedParties { get; set; } = [];
    
    // Navigation properties
    public virtual StaffMember ReportedBy { get; set; } = null!;
    public virtual ICollection<SafetyImage> Images { get; set; } = [];
}
```

## 2. Service Layer Implementation

### 2.1 Quality Service Interface
```csharp
public interface IQualityService
{
    Task<Result<Inspection>> CreateInspection(InspectionCreateDto dto);
    Task<Result<Inspection>> CompleteInspection(
        int inspectionId, 
        InspectionResultDto result);
    Task<Result<List<QualityCheck>>> GetFailedChecks(
        int restaurantId, 
        DateTime? since = null);
    Task<Result<ComplianceReport>> GetComplianceReport(
        int restaurantId, 
        DateTime startDate, 
        DateTime endDate);
    Task<Result<List<SafetyReport>>> GetActiveSafetyIssues(
        int restaurantId);
}

public class QualityService : IQualityService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<QualityService> _logger;

    public async Task<Result<Inspection>> CreateInspection(
        InspectionCreateDto dto)
    {
        using var transaction = 
            await _context.Database.BeginTransactionAsync();
        try
        {
            var inspection = new Inspection
            {
                InspectionDate = DateTime.UtcNow,
                Type = dto.Type,
                Status = InspectionStatus.InProgress,
                InspectorId = dto.InspectorId,
                RestaurantId = dto.RestaurantId,
                IsUrgent = dto.IsUrgent
            };

            // Create quality checks
            foreach (var checkDto in dto.Checks)
            {
                inspection.QualityChecks.Add(new QualityCheck
                {
                    Type = checkDto.Type,
                    Category = checkDto.Category,
                    Criterion = checkDto.Criterion,
                    RestaurantId = dto.RestaurantId
                });
            }

            _context.Inspections.Add(inspection);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Notify relevant staff
            await NotifyInspectionCreated(inspection);

            return Result<Inspection>.Success(inspection);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating inspection");
            return Result<Inspection>.Failure(
                "Failed to create inspection");
        }
    }
}
```

## 3. Blazor Components

### 3.1 Quality Dashboard Component
```razor
@* Components/Quality/QualityDashboard.razor *@
@inject IQualityService QualityService
@inject INotificationService NotificationService

<div class="quality-dashboard">
    <div class="dashboard-header">
        <h2>Quality Control Dashboard</h2>
        <div class="action-buttons">
            <button class="btn btn-primary" 
                    @onclick="StartInspection">
                New Inspection
            </button>
            <button class="btn btn-danger"
                    @onclick="ReportSafetyIssue">
                Report Safety Issue
            </button>
        </div>
    </div>

    <div class="metrics-grid">
        <MetricCard Title="Compliance Score"
                   Value="@($"{complianceScore:P0}")"
                   Icon="check-circle"
                   Trend="@complianceTrend" />
        <MetricCard Title="Open Issues"
                   Value="@openIssues.ToString()"
                   Icon="exclamation-triangle"
                   AlertLevel="@(openIssues > 5 ? "danger" : "normal")" />
        <MetricCard Title="Next Inspection"
                   Value="@nextInspection?.ToString("d") ?? "Not Scheduled""
                   Icon="calendar" />
    </div>

    <div class="content-grid">
        <div class="recent-inspections">
            <h3>Recent Inspections</h3>
            @foreach (var inspection in recentInspections)
            {
                <InspectionCard Inspection="@inspection"
                              OnView="@(() => ViewInspection(inspection))" />
            }
        </div>

        <div class="active-issues">
            <h3>Active Issues</h3>
            @foreach (var issue in activeIssues)
            {
                <IssueCard Issue="@issue"
                          OnResolve="@(() => ResolveIssue(issue))" />
            }
        </div>
    </div>

    @if (showInspectionDialog)
    {
        <InspectionDialog OnSubmit="@HandleInspectionSubmit"
                         OnCancel="@(() => showInspectionDialog = false)" />
    }
</div>

@code {
    [Parameter]
    public int RestaurantId { get; set; }

    private List<Inspection> recentInspections = [];
    private List<QualityIssue> activeIssues = [];
    private double complianceScore;
    private int openIssues;
    private DateTime? nextInspection;
    private bool showInspectionDialog;

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardData();
    }

    private async Task LoadDashboardData()
    {
        var tasks = new[]
        {
            LoadRecentInspections(),
            LoadActiveIssues(),
            LoadComplianceMetrics()
        };

        await Task.WhenAll(tasks);
    }
}
```

### 3.2 Safety Checklist Component
```razor
@* Components/Quality/SafetyChecklist.razor *@
@inject IQualityService QualityService

<div class="safety-checklist">
    <div class="checklist-header">
        <h3>Safety Checklist</h3>
        <span class="completion-status">
            @completedItems / @totalItems Complete
        </span>
    </div>

    <div class="checklist-categories">
        @foreach (var category in categories)
        {
            <div class="category-section">
                <h4>@category.Name</h4>
                @foreach (var item in category.Items)
                {
                    <div class="checklist-item @GetItemClass(item)">
                        <div class="item-status">
                            <input type="checkbox"
                                   checked="@item.IsCompleted"
                                   @onchange="e => ToggleItem(item, e)" />
                        </div>
                        <div class="item-content">
                            <span class="item-name">@item.Name</span>
                            <span class="item-description">
                                @item.Description
                            </span>
                        </div>
                        @if (!item.IsCompleted)
                        {
                            <button class="report-issue"
                                    @onclick="() => ReportIssue(item)">
                                Report Issue
                            </button>
                        }
                    </div>
                }
            </div>
        }
    </div>
</div>

@code {
    [Parameter]
    public int RestaurantId { get; set; }

    private List<SafetyCategory> categories = [];
    private int completedItems;
    private int totalItems;

    protected override async Task OnInitializedAsync()
    {
        await LoadChecklist();
    }

    private async Task LoadChecklist()
    {
        var result = await QualityService
            .GetSafetyChecklist(RestaurantId);
            
        if (result.IsSuccess)
        {
            categories = result.Value;
            UpdateCounts();
        }
    }
}
```

## 4. Background Services

### 4.1 Quality Monitoring Service
```csharp
public class QualityMonitoringService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<QualityMonitoringService> _logger;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var qualityService = scope.ServiceProvider
                    .GetRequiredService<IQualityService>();

                // Check compliance
                await CheckComplianceStatus(qualityService);

                // Process scheduled inspections
                await ProcessScheduledInspections(qualityService);

                // Generate reports
                await GenerateQualityReports(qualityService);

                // Check for alerts
                await CheckQualityAlerts(qualityService);

                await Task.Delay(TimeSpan.FromMinutes(30), 
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error in quality monitoring");
                await Task.Delay(TimeSpan.FromMinutes(5), 
                    stoppingToken);
            }
        }
    }
}
```

## Implementation Checklist

### Phase 1: Core Features
- [ ] Inspection system
- [ ] Quality checks
- [ ] Safety reporting
- [ ] Basic compliance

### Phase 2: Advanced Features
- [ ] Automated monitoring
- [ ] Risk assessment
- [ ] Issue tracking
- [ ] Compliance reporting

### Phase 3: Integration
- [ ] Inventory system
- [ ] Order system
- [ ] Staff training
- [ ] Analytics platform

### Phase 4: Enhancement
- [ ] Mobile inspections
- [ ] Real-time alerts
- [ ] Trend analysis
- [ ] Predictive maintenance

## Monitoring and Maintenance

### Key Metrics
1. Compliance rate
2. Issue resolution time
3. Inspection scores
4. Safety incidents

### Regular Tasks
1. Inspection scheduling
2. Compliance checks
3. Report generation
4. Staff training

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0   | TBD  | Initial implementation |
| 1.1.0   | TBD  | Advanced monitoring |
| 1.2.0   | TBD  | Mobile support |