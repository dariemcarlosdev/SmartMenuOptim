# Inventory Management System Implementation Guide

## Overview
This guide outlines the implementation of the Smart Menu Optimization Inventory Management System, integrating with Restaurant, Order, and Analytics systems in a Blazor-based architecture.

## Project Structure Integration
```
SmartMenuOptim/
??? SmartMenuOptim.API/
?   ??? Controllers/
?       ??? InventoryController.cs
?       ??? IngredientController.cs
?       ??? StockController.cs
??? SmartMenuOptim.Server/
?   ??? Components/
?       ??? Inventory/
?           ??? InventoryDashboard.razor
?           ??? StockManager.razor
?           ??? IngredientEditor.razor
?           ??? StockAlerts.razor
??? SmartMenuOptim.Shared/
?   ??? Models/
?       ??? Inventory/
?           ??? Inventory.cs
?           ??? Ingredient.cs
?           ??? StockTransaction.cs
?           ??? InventoryAlert.cs
??? SmartMenuOptim.Tests/
    ??? Inventory/
        ??? InventoryTests.cs
```

## 1. Entity Definitions

### 1.1 Core Models
```csharp
// Inventory.cs
public class Inventory : TenantEntityBase
{
    public int Id { get; set; }
    public int IngredientId { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal MaximumStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public DateTime LastRestockDate { get; set; }
    
    // Navigation properties
    public virtual Ingredient Ingredient { get; set; } = null!;
    public virtual ICollection<StockTransaction> Transactions { get; set; } = [];
}

// Ingredient.cs
public class Ingredient : TenantEntityBase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool RequiresRefrigeration { get; set; }
    public int ShelfLifeDays { get; set; }
    public string? StorageInstructions { get; set; }
    public string? AllergenInfo { get; set; }
    
    // Navigation properties
    public virtual ICollection<DishIngredient> DishIngredients { get; set; } = [];
    public virtual ICollection<Supplier> Suppliers { get; set; } = [];
}

// StockTransaction.cs
public class StockTransaction : TenantEntityBase
{
    public int Id { get; set; }
    public int InventoryId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Reference { get; set; }
    public DateTime TransactionDate { get; set; }
    public int? OrderId { get; set; }
    public int? SupplierId { get; set; }
    
    // Navigation properties
    public virtual Inventory Inventory { get; set; } = null!;
    public virtual Order? Order { get; set; }
    public virtual Supplier? Supplier { get; set; }
}

public enum TransactionType
{
    Purchase,
    Consumption,
    Waste,
    Adjustment,
    Return
}
```

### 1.2 Entity Configuration
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Inventory>(entity =>
    {
        entity.HasIndex(e => new { e.RestaurantId, e.IngredientId })
              .IsUnique()
              .HasDatabaseName("IX_Inventory_Restaurant_Ingredient");

        entity.HasIndex(e => e.CurrentStock)
              .HasDatabaseName("IX_Inventory_CurrentStock");
    });

    modelBuilder.Entity<StockTransaction>(entity =>
    {
        entity.HasIndex(e => new { e.RestaurantId, e.TransactionDate })
              .HasDatabaseName("IX_StockTransactions_Restaurant_Date");
    });
}
```

## 2. Service Layer Implementation

### 2.1 Inventory Service Interface
```csharp
public interface IInventoryService
{
    Task<Result<Inventory>> GetInventory(int ingredientId);
    Task<Result<List<Inventory>>> GetLowStockItems();
    Task<Result<StockTransaction>> AddStock(
        StockTransactionDto transaction);
    Task<Result<bool>> UpdateStockLevel(
        int ingredientId, 
        decimal quantity, 
        TransactionType type);
    Task<Result<List<InventoryAlert>>> GetAlerts();
}

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<InventoryService> _logger;
    private readonly INotificationService _notificationService;

    public async Task<Result<StockTransaction>> AddStock(
        StockTransactionDto transaction)
    {
        using var dbTransaction = 
            await _context.Database.BeginTransactionAsync();
        try
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => 
                    i.IngredientId == transaction.IngredientId &&
                    i.RestaurantId == transaction.RestaurantId);

            if (inventory == null)
                return Result<StockTransaction>
                    .Failure("Inventory not found");

            var stockTransaction = new StockTransaction
            {
                InventoryId = inventory.Id,
                RestaurantId = transaction.RestaurantId,
                Type = transaction.Type,
                Quantity = transaction.Quantity,
                UnitPrice = transaction.UnitPrice,
                Reference = transaction.Reference,
                TransactionDate = DateTime.UtcNow
            };

            // Update inventory levels
            inventory.CurrentStock += transaction.Quantity;
            inventory.LastRestockDate = DateTime.UtcNow;

            _context.StockTransactions.Add(stockTransaction);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // Check for alerts
            await CheckAndNotifyStockLevels(inventory);

            return Result<StockTransaction>.Success(stockTransaction);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error processing stock transaction");
            return Result<StockTransaction>
                .Failure("Failed to process transaction");
        }
    }
}
```

## 3. Blazor Components

### 3.1 Stock Manager Component
```razor
@* Components/Inventory/StockManager.razor *@
@inject IInventoryService InventoryService

<div class="stock-manager">
    <div class="filters">
        <div class="filter-group">
            <label>Category:</label>
            <select @bind="selectedCategory">
                <option value="">All Categories</option>
                @foreach (var category in categories)
                {
                    <option value="@category">@category</option>
                }
            </select>
        </div>

        <div class="filter-group">
            <label>Stock Level:</label>
            <select @bind="stockFilter">
                <option value="All">All Items</option>
                <option value="Low">Low Stock</option>
                <option value="OutOfStock">Out of Stock</option>
            </select>
        </div>
    </div>

    <div class="inventory-grid">
        @foreach (var item in filteredItems)
        {
            <InventoryCard Item="@item"
                          OnRestock="@(() => ShowRestockDialog(item))"
                          OnAdjust="@(() => ShowAdjustmentDialog(item))" />
        }
    </div>

    @if (showRestockDialog)
    {
        <RestockDialog Item="@selectedItem"
                      OnConfirm="@HandleRestock"
                      OnCancel="@(() => showRestockDialog = false)" />
    }
</div>

@code {
    [Parameter]
    public int RestaurantId { get; set; }

    private List<Inventory> inventoryItems = [];
    private string selectedCategory = "";
    private string stockFilter = "All";
    private bool showRestockDialog;
    private Inventory? selectedItem;

    protected override async Task OnInitializedAsync()
    {
        await LoadInventory();
    }

    private async Task LoadInventory()
    {
        var result = await InventoryService
            .GetInventoryItems(RestaurantId);
        if (result.IsSuccess)
        {
            inventoryItems = result.Value;
        }
    }
}
```

### 3.2 Inventory Alerts Component
```razor
@* Components/Inventory/StockAlerts.razor *@
@inject IInventoryService InventoryService
@implements IAsyncDisposable

<div class="stock-alerts">
    <h3>Inventory Alerts</h3>
    
    <div class="alert-list">
        @foreach (var alert in alerts.OrderByDescending(a => a.Severity))
        {
            <div class="alert-item @GetAlertClass(alert)">
                <div class="alert-icon">
                    <i class="@GetAlertIcon(alert)" />
                </div>
                <div class="alert-content">
                    <h4>@alert.Title</h4>
                    <p>@alert.Description</p>
                </div>
                <div class="alert-actions">
                    @if (alert.Type == AlertType.LowStock)
                    {
                        <button @onclick="() => HandleRestock(alert)">
                            Restock Now
                        </button>
                    }
                </div>
            </div>
        }
    </div>
</div>

@code {
    private List<InventoryAlert> alerts = [];
    private HubConnection? hubConnection;

    protected override async Task OnInitializedAsync()
    {
        await LoadAlerts();
        await SetupRealTimeUpdates();
    }

    private async Task SetupRealTimeUpdates()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl("inventory")
            .WithAutomaticReconnect()
            .Build();

        hubConnection.On<InventoryAlert>("ReceiveAlert", 
            alert => InvokeAsync(() => HandleNewAlert(alert)));

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

## 4. Background Processing

### 4.1 Inventory Monitoring Service
```csharp
public class InventoryMonitoringService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InventoryMonitoringService> _logger;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var inventoryService = scope.ServiceProvider
                    .GetRequiredService<IInventoryService>();

                // Check stock levels
                await CheckLowStockLevels(inventoryService);

                // Check expiry dates
                await CheckExpiryDates(inventoryService);

                // Generate reports
                await GenerateInventoryReports(inventoryService);

                // Wait for next check
                await Task.Delay(TimeSpan.FromHours(1), 
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error monitoring inventory");
                await Task.Delay(TimeSpan.FromMinutes(5), 
                    stoppingToken);
            }
        }
    }
}
```

## Implementation Checklist

### Phase 1: Core Features
- [ ] Stock management
- [ ] Ingredient tracking
- [ ] Basic reporting
- [ ] Alert system

### Phase 2: Advanced Features
- [ ] Automated reordering
- [ ] Supplier management
- [ ] Cost tracking
- [ ] Waste management

### Phase 3: Integration
- [ ] Order system
- [ ] Menu planning
- [ ] Analytics system
- [ ] Financial system

### Phase 4: Enhancement
- [ ] Mobile support
- [ ] Barcode scanning
- [ ] Recipe scaling
- [ ] Cost optimization

## Monitoring and Maintenance

### Key Metrics
1. Stock turnover rate
2. Stock accuracy
3. Wastage rate
4. Reorder efficiency

### Regular Tasks
1. Stock reconciliation
2. Cost updates
3. Supplier reviews
4. Data cleanup

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0   | TBD  | Initial implementation |
| 1.1.0   | TBD  | Cost tracking |
| 1.2.0   | TBD  | Advanced analytics |