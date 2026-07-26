# Financial Management System Implementation Guide

## Overview
This guide outlines the implementation of the Smart Menu Optimization Financial Management System, integrating with Order, Inventory, and Analytics systems in a Blazor-based architecture.

## Project Structure Integration
```
SmartMenuOptim/
├── SmartMenuOptim.API/
│   └── Controllers/
│       ├── FinanceController.cs
│       ├── ExpenseController.cs
│       └── RevenueController.cs
├── SmartMenuOptim.Server/
│   └── Components/
│       └── Finance/
│           ├── FinanceDashboard.razor
│           ├── ExpenseManager.razor
│           ├── RevenueTracker.razor
│           └── FinancialReports.razor
├── SmartMenuOptim.Shared/
│   └── Models/
│       └── Finance/
│           ├── Transaction.cs
│           ├── Expense.cs
│           ├── Revenue.cs
│           └── FinancialMetrics.cs
└── SmartMenuOptim.Tests/
    └── Finance/
        └── FinanceTests.cs
```

## 1. Entity Definitions

### 1.1 Financial Models
```csharp
// Transaction.cs
public class Transaction : TenantEntityBase
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public TransactionStatus Status { get; set; }
    
    // Optional references
    public int? OrderId { get; set; }
    public int? ExpenseId { get; set; }
    public int? SupplierId { get; set; }
    
    // Navigation properties
    public virtual Order? Order { get; set; }
    public virtual Expense? Expense { get; set; }
    public virtual Supplier? Supplier { get; set; }
}

// Expense.cs
public class Expense : TenantEntityBase
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ExpenseType Type { get; set; }
    public ExpenseStatus Status { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public int? SupplierId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? DueDate { get; set; }
    
    // Budget tracking
    public int? BudgetCategoryId { get; set; }
    public bool IsPlanned { get; set; }
    public string? ApprovedBy { get; set; }
    
    // Navigation properties
    public virtual Supplier? Supplier { get; set; }
    public virtual BudgetCategory? BudgetCategory { get; set; }
    public virtual ICollection<Transaction> Transactions { get; set; } = [];
}

// Revenue.cs
public class Revenue : TenantEntityBase
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public RevenueSource Source { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string? PaymentReference { get; set; }
    
    // Analysis
    public string? RevenueCategory { get; set; }
    public Dictionary<string, decimal> ItemizedRevenue { get; set; } = [];
    
    // Navigation properties
    public virtual Order? Order { get; set; }
    public virtual ICollection<Transaction> Transactions { get; set; } = [];
}

// FinancialMetrics.cs
public class FinancialMetrics : TenantEntityBase
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    
    // Revenue metrics
    public decimal TotalRevenue { get; set; }
    public decimal NetRevenue { get; set; }
    public Dictionary<string, decimal> RevenueByCategory { get; set; } = [];
    
    // Expense metrics
    public decimal TotalExpenses { get; set; }
    public Dictionary<string, decimal> ExpensesByCategory { get; set; } = [];
    
    // Profit metrics
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    
    // Operational metrics
    public decimal CostOfGoodsSold { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal LaborCost { get; set; }
    public decimal InventoryCost { get; set; }
}
```

## 2. Service Layer Implementation

### 2.1 Financial Service Interface
```csharp
public interface IFinancialService
{
    Task<Result<Transaction>> RecordTransaction(TransactionCreateDto dto);
    Task<Result<Expense>> RecordExpense(ExpenseCreateDto dto);
    Task<Result<Revenue>> RecordRevenue(RevenueCreateDto dto);
    Task<Result<FinancialMetrics>> GetFinancialMetrics(
        int restaurantId, 
        DateTime startDate, 
        DateTime endDate);
    Task<Result<CashFlowReport>> GetCashFlowReport(
        int restaurantId, 
        DateTime date);
    Task<Result<ProfitLossReport>> GetProfitLossReport(
        int restaurantId, 
        DateTime startDate, 
        DateTime endDate);
}

public class FinancialService : IFinancialService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FinancialService> _logger;

    public async Task<Result<Transaction>> RecordTransaction(
        TransactionCreateDto dto)
    {
        using var transaction = 
            await _context.Database.BeginTransactionAsync();
        try
        {
            var financialTransaction = new Transaction
            {
                Date = dto.Date,
                Type = dto.Type,
                Amount = dto.Amount,
                Description = dto.Description,
                Category = dto.Category,
                Reference = dto.Reference,
                PaymentMethod = dto.PaymentMethod,
                Status = TransactionStatus.Pending,
                RestaurantId = dto.RestaurantId
            };

            _context.Transactions.Add(financialTransaction);
            await _context.SaveChangesAsync();

            // Update related records
            await UpdateRelatedRecords(financialTransaction);

            // Update financial metrics
            await UpdateFinancialMetrics(
                dto.RestaurantId, 
                dto.Date.Date);

            await transaction.CommitAsync();
            return Result<Transaction>.Success(financialTransaction);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error recording transaction");
            return Result<Transaction>.Failure(
                "Failed to record transaction");
        }
    }

    private async Task UpdateFinancialMetrics(
        int restaurantId, 
        DateTime date)
    {
        var metrics = await _context.FinancialMetrics
            .FirstOrDefaultAsync(m => 
                m.RestaurantId == restaurantId && 
                m.Date.Date == date.Date);

        if (metrics == null)
        {
            metrics = new FinancialMetrics
            {
                RestaurantId = restaurantId,
                Date = date
            };
            _context.FinancialMetrics.Add(metrics);
        }

        // Calculate metrics
        var transactions = await _context.Transactions
            .Where(t => 
                t.RestaurantId == restaurantId && 
                t.Date.Date == date.Date)
            .ToListAsync();

        metrics.TotalRevenue = transactions
            .Where(t => t.Type == TransactionType.Revenue)
            .Sum(t => t.Amount);

        metrics.TotalExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        metrics.GrossProfit = metrics.TotalRevenue - metrics.CostOfGoodsSold;
        metrics.NetProfit = metrics.GrossProfit - metrics.OperatingExpenses;
        metrics.ProfitMargin = metrics.TotalRevenue > 0 
            ? metrics.NetProfit / metrics.TotalRevenue 
            : 0;

        await _context.SaveChangesAsync();
    }
}
```

## 3. Blazor Components

### 3.1 Financial Dashboard Component
```razor
@* Components/Finance/FinanceDashboard.razor *@
@inject IFinancialService FinancialService
@implements IDisposable

<div class="finance-dashboard">
    <div class="dashboard-header">
        <h2>Financial Dashboard</h2>
        <div class="date-range">
            <DateRangePicker @bind-StartDate="startDate"
                            @bind-EndDate="endDate"
                            OnRangeSelected="LoadFinancials" />
        </div>
    </div>

    <div class="metrics-grid">
        <MetricCard Title="Revenue"
                   Value="@metrics.TotalRevenue.ToString("C")"
                   Trend="@revenueTrend"
                   Icon="dollar-sign" />
        <MetricCard Title="Expenses"
                   Value="@metrics.TotalExpenses.ToString("C")"
                   Trend="@expensesTrend"
                   Icon="credit-card" />
        <MetricCard Title="Net Profit"
                   Value="@metrics.NetProfit.ToString("C")"
                   Trend="@profitTrend"
                   Icon="trending-up" />
        <MetricCard Title="Profit Margin"
                   Value="@metrics.ProfitMargin.ToString("P")"
                   Icon="percent" />
    </div>

    <div class="charts-section">
        <div class="revenue-chart">
            <h3>Revenue Breakdown</h3>
            <PieChart Data="@revenueData"
                     Options="@chartOptions" />
        </div>

        <div class="expenses-chart">
            <h3>Expense Categories</h3>
            <PieChart Data="@expenseData"
                     Options="@chartOptions" />
        </div>

        <div class="trend-chart">
            <h3>Financial Trends</h3>
            <LineChart Data="@trendData"
                      Options="@trendOptions" />
        </div>
    </div>

    <div class="recent-transactions">
        <h3>Recent Transactions</h3>
        <TransactionList Transactions="@recentTransactions"
                        OnView="@ViewTransaction" />
    </div>
</div>

@code {
    [Parameter]
    public int RestaurantId { get; set; }

    private FinancialMetrics metrics = new();
    private DateTime startDate = DateTime.Today.AddMonths(-1);
    private DateTime endDate = DateTime.Today;
    private List<Transaction> recentTransactions = [];
    private HubConnection? hubConnection;

    protected override async Task OnInitializedAsync()
    {
        await LoadFinancials();
        await SetupRealTimeUpdates();
    }

    private async Task LoadFinancials()
    {
        var result = await FinancialService.GetFinancialMetrics(
            RestaurantId, startDate, endDate);
            
        if (result.IsSuccess)
        {
            metrics = result.Value;
            UpdateCharts();
        }
    }

    private async Task SetupRealTimeUpdates()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl("finance")
            .WithAutomaticReconnect()
            .Build();

        hubConnection.On<Transaction>("ReceiveTransaction", 
            transaction => InvokeAsync(() => 
                HandleNewTransaction(transaction)));

        await hubConnection.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}
```

### 3.2 Financial Reports Component
```razor
@* Components/Finance/FinancialReports.razor *@
@inject IFinancialService FinancialService

<div class="financial-reports">
    <div class="reports-header">
        <h3>Financial Reports</h3>
        <div class="report-controls">
            <select @bind="selectedReport">
                <option value="ProfitLoss">Profit & Loss</option>
                <option value="CashFlow">Cash Flow</option>
                <option value="Balance">Balance Sheet</option>
            </select>
            <button @onclick="GenerateReport">
                Generate Report
            </button>
            <button @onclick="ExportReport">
                Export
            </button>
        </div>
    </div>

    <div class="report-content">
        @if (loading)
        {
            <LoadingSpinner />
        }
        else
        {
            @switch (selectedReport)
            {
                case "ProfitLoss":
                    <ProfitLossReport Data="@profitLossData" />
                    break;
                case "CashFlow":
                    <CashFlowReport Data="@cashFlowData" />
                    break;
                case "Balance":
                    <BalanceSheet Data="@balanceData" />
                    break;
            }
        }
    </div>
</div>

@code {
    [Parameter]
    public int RestaurantId { get; set; }

    private string selectedReport = "ProfitLoss";
    private bool loading;
    private object? profitLossData;
    private object? cashFlowData;
    private object? balanceData;

    private async Task GenerateReport()
    {
        loading = true;
        try
        {
            switch (selectedReport)
            {
                case "ProfitLoss":
                    await LoadProfitLossReport();
                    break;
                case "CashFlow":
                    await LoadCashFlowReport();
                    break;
                case "Balance":
                    await LoadBalanceSheet();
                    break;
            }
        }
        finally
        {
            loading = false;
        }
    }
}
```

## 4. Background Services

### 4.1 Financial Processing Service
```csharp
public class FinancialProcessingService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<FinancialProcessingService> _logger;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var financialService = scope.ServiceProvider
                    .GetRequiredService<IFinancialService>();

                // Process pending transactions
                await ProcessPendingTransactions(financialService);

                // Update financial metrics
                await UpdateFinancialMetrics(financialService);

                // Generate reports
                await GenerateScheduledReports(financialService);

                // Check for alerts
                await CheckFinancialAlerts(financialService);

                await Task.Delay(TimeSpan.FromMinutes(15), 
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error in financial processing");
                await Task.Delay(TimeSpan.FromMinutes(1), 
                    stoppingToken);
            }
        }
    }
}
```

## Implementation Checklist

### Phase 1: Core Features
- [ ] Transaction management
- [ ] Expense tracking
- [ ] Revenue recording
- [ ] Basic reporting

### Phase 2: Advanced Features
- [ ] Automated reconciliation
- [ ] Budget management
- [ ] Forecasting
- [ ] Cost analysis

### Phase 3: Integration
- [ ] Order system
- [ ] Inventory system
- [ ] Payroll system
- [ ] Tax management

### Phase 4: Enhancement
- [ ] Advanced analytics
- [ ] Cash flow forecasting
- [ ] Automated alerts
- [ ] Mobile support

## Monitoring and Maintenance

### Key Metrics
1. Revenue growth
2. Profit margins
3. Operating costs
4. Cash flow

### Regular Tasks
1. Transaction reconciliation
2. Financial reporting
3. Budget reviews
4. Performance analysis

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0   | TBD  | Initial implementation |
| 1.1.0   | TBD  | Advanced reporting |
| 1.2.0   | TBD  | Forecasting features |