# Order Management System Implementation Guide

## Overview
This guide outlines the implementation steps for the Smart Menu Optimization Order Management System, integrated with the Profile, Restaurant, and Loyalty Management Systems in a Blazor-based architecture.

## Project Structure Integration
```
SmartMenuOptim/
├── SmartMenuOptim.API/
│   └── Controllers/
│       ├── OrderController.cs
│       └── OrderStatusController.cs
├── SmartMenuOptim.Server/
│   └── Components/
│       └── Order/
│           ├── OrderDashboard.razor
│           ├── OrderCreation.razor
│           ├── OrderTracking.razor
│           └── OrderHistory.razor
├── SmartMenuOptim.Shared/
│   └── Models/
│       └── Order/
│           ├── Order.cs
│           ├── OrderItem.cs
│           └── OrderStatus.cs
└── SmartMenuOptim.Tests/
    └── Order/
        └── OrderTests.cs
```

## 1. Database Schema

### 1.1 Entity Definitions
```csharp
// Order.cs
public class Order : EntityBase
{
    public int Id { get; set; }
    public int? CustomerId { get; set; }
    public int RestaurantId { get; set; }
    public int OrderStatusId { get; set; }
    public DateTime OrderDate { get; set; }
    public string? SpecialInstructions { get; set; }
    public decimal TotalAmount { get; set; }
    public int? TableId { get; set; }
    public int? HandledByStaffId { get; set; }
    public OrderType Type { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    
    // Navigation properties
    public virtual Customer? Customer { get; set; }
    public virtual Restaurant Restaurant { get; set; } = null!;
    public virtual OrderStatus Status { get; set; } = null!;
    public virtual Table? Table { get; set; }
    public virtual StaffMember? HandledByStaff { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = [];
}

// OrderItem.cs
public class OrderItem : EntityBase
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int DishId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? SpecialRequests { get; set; }
    public int RestaurantId { get; set; }
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual Dish Dish { get; set; } = null!;
}

// OrderStatus.cs
public class OrderStatus : EntityBase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsTerminal { get; set; }
    public string? ColorCode { get; set; }
    public int RestaurantId { get; set; }
    
    public virtual Restaurant Restaurant { get; set; } = null!;
    public virtual ICollection<Order> Orders { get; set; } = [];
}

public enum OrderType
{
    DineIn,
    TakeOut,
    Delivery
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Refunded,
    Failed
}
```

### 1.2 Entity Configuration
```csharp
// AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>(entity =>
    {
        entity.HasIndex(e => new { e.RestaurantId, e.OrderDate })
              .HasDatabaseName("IX_Orders_Restaurant_Date");
              
        entity.HasIndex(e => new { e.CustomerId, e.OrderDate })
              .HasDatabaseName("IX_Orders_Customer_Date");
              
        entity.HasIndex(e => new { e.RestaurantId, e.OrderStatusId })
              .HasDatabaseName("IX_Orders_Restaurant_Status");
    });

    modelBuilder.Entity<OrderItem>(entity =>
    {
        entity.HasIndex(e => new { e.OrderId, e.DishId })
              .HasDatabaseName("IX_OrderItems_Order_Dish");
    });
}
```

## 2. Service Layer Implementation

### 2.1 Order Service Interface
```csharp
public interface IOrderService
{
    Task<Result<Order>> CreateOrder(OrderCreateDto dto);
    Task<Result<Order>> UpdateOrderStatus(int orderId, int statusId);
    Task<Result<Order>> AddOrderItems(int orderId, List<OrderItemDto> items);
    Task<Result<Order>> CompleteOrder(int orderId);
    Task<Result<List<Order>>> GetActiveOrders(int restaurantId);
    Task<Result<OrderStatistics>> GetOrderStatistics(int restaurantId, DateTime? start, DateTime? end);
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly ILoyaltyService _loyaltyService;
    private readonly INotificationService _notificationService;
    
    public async Task<Result<Order>> CreateOrder(OrderCreateDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                CustomerId = dto.CustomerId,
                RestaurantId = dto.RestaurantId,
                OrderDate = DateTime.UtcNow,
                Type = dto.OrderType,
                TableId = dto.TableId,
                SpecialInstructions = dto.SpecialInstructions
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _notificationService.NotifyNewOrder(order);
            
            return Result<Order>.Success(order);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result<Order>.Failure("Failed to create order");
        }
    }
}
```

## 3. Blazor Components

### 3.1 Order Creation Component
```razor
@* Components/Order/OrderCreation.razor *@
@inherits ComponentBase
@inject IOrderService OrderService
@inject IRestaurantService RestaurantService

<div class="order-creation">
    <EditForm Model="@orderModel" OnValidSubmit="@HandleSubmit">
        <DataAnnotationsValidator />
        
        <div class="form-group">
            <label>Order Type</label>
            <InputSelect @bind-Value="orderModel.OrderType">
                @foreach (var type in Enum.GetValues<OrderType>())
                {
                    <option value="@type">@type</option>
                }
            </InputSelect>
        </div>

        <div class="items-section">
            @foreach (var item in orderModel.Items)
            {
                <OrderItemEditor Item="@item" 
                               OnRemove="@(() => RemoveItem(item))"
                               OnQuantityChanged="@(() => UpdateTotals())" />
            }
            <button type="button" @onclick="AddItem">Add Item</button>
        </div>

        <div class="totals-section">
            <h4>Order Total: @orderModel.TotalAmount.ToString("C")</h4>
        </div>

        <button type="submit" class="btn btn-primary">Create Order</button>
    </EditForm>
</div>

@code {
    private OrderCreateModel orderModel = new();

    private async Task HandleSubmit()
    {
        var result = await OrderService.CreateOrder(
            orderModel.ToCreateDto());
            
        if (result.IsSuccess)
        {
            // Handle success
        }
    }
}
```

### 3.2 Order Tracking Component
```razor
@* Components/Order/OrderTracking.razor *@
@implements IDisposable
@inject IOrderService OrderService
@inject IHubConnection HubConnection

<div class="order-tracking">
    <div class="order-status-list">
        @foreach (var order in activeOrders)
        {
            <OrderStatusCard Order="@order" 
                           OnStatusChange="@HandleStatusChange" />
        }
    </div>
</div>

@code {
    private List<Order> activeOrders = [];
    
    protected override async Task OnInitializedAsync()
    {
        await LoadActiveOrders();
        await SetupSignalR();
    }

    private async Task SetupSignalR()
    {
        HubConnection.On<Order>("OrderUpdated", 
            order => InvokeAsync(() => HandleOrderUpdate(order)));
    }
}
```

## 4. Real-time Updates

### 4.1 SignalR Hub
```csharp
// Hubs/OrderHub.cs
public class OrderHub : Hub
{
    public async Task JoinRestaurantGroup(int restaurantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, 
            $"restaurant_{restaurantId}");
    }

    public async Task UpdateOrderStatus(int orderId, int statusId)
    {
        // Update order status
        await Clients.Group($"restaurant_{restaurantId}")
            .SendAsync("OrderStatusUpdated", orderId, statusId);
    }
}
```

## 5. Integration Points

### 5.1 Loyalty Integration
```csharp
public class OrderCompletionService
{
    private readonly IOrderService _orderService;
    private readonly ILoyaltyService _loyaltyService;
    
    public async Task<Result<Order>> CompleteOrder(int orderId)
    {
        var order = await _orderService.CompleteOrder(orderId);
        
        if (order.IsSuccess && order.Value.CustomerId.HasValue)
        {
            await _loyaltyService.AddOrderPoints(
                order.Value.CustomerId.Value,
                order.Value.RestaurantId,
                order.Value.TotalAmount);
        }
        
        return order;
    }
}
```

### 5.2 Kitchen Integration
```csharp
public class KitchenService
{
    private readonly IOrderService _orderService;
    private readonly IHubContext<OrderHub> _hubContext;
    
    public async Task ProcessOrder(int orderId)
    {
        // Process order in kitchen
        await _hubContext.Clients
            .Group($"kitchen_{restaurantId}")
            .SendAsync("NewOrderReceived", orderId);
    }
}
```

## 6. Background Processing

### 6.1 Order Cleanup Service
```csharp
public class OrderCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var orderService = scope.ServiceProvider
                .GetRequiredService<IOrderService>();
                
            await orderService.CleanupAbandonedOrders();
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

## Implementation Checklist

### Phase 1: Core Features
- [ ] Order creation
- [ ] Order management
- [ ] Status tracking
- [ ] Basic reporting

### Phase 2: Integration
- [ ] Kitchen system
- [ ] Payment processing
- [ ] Loyalty integration
- [ ] Inventory management

### Phase 3: Advanced Features
- [ ] Real-time updates
- [ ] Order analytics
- [ ] Performance optimization
- [ ] Mobile support

### Phase 4: Enhancement
- [ ] Advanced reporting
- [ ] AI-powered insights
- [ ] Customer feedback
- [ ] Order prediction

## Monitoring and Maintenance

### Key Metrics
1. Order processing time
2. Order completion rate
3. Average order value
4. Peak order times

### Regular Tasks
1. Order data archiving
2. Performance monitoring
3. Status cleanup
4. Analytics generation

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0   | TBD  | Initial implementation |
| 1.1.0   | TBD  | Real-time updates |
| 1.2.0   | TBD  | Analytics integration |