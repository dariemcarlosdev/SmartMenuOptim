# Reservation System Implementation Guide

## Overview
This guide outlines the implementation steps for the Smart Menu Optimization Reservation System, integrated with the Restaurant and Profile Management Systems in a Blazor-based architecture.

## Project Structure Integration
```
SmartMenuOptim/
├── SmartMenuOptim.API/
│   └── Controllers/
│       ├── ReservationController.cs
│       └── TableController.cs
├── SmartMenuOptim.Server/
│   └── Components/
│       └── Reservation/
│           ├── ReservationDashboard.razor
│           ├── TableLayout.razor
│           ├── ReservationCalendar.razor
│           └── CapacityPlanner.razor
├── SmartMenuOptim.Shared/
│   └── Models/
│       └── Reservation/
│           ├── Reservation.cs
│           ├── Table.cs
│           └── TimeSlot.cs
└── SmartMenuOptim.Tests/
    └── Reservation/
        └── ReservationTests.cs
```

## 1. Database Schema

### 1.1 Entity Definitions
```csharp
// Table.cs
public class Table : EntityBase
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string TableNumber { get; set; } = string.Empty;
    
    public int RestaurantId { get; set; }
    
    [Range(1, 100)]
    public int Capacity { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    [MaxLength(200)]
    public string? Location { get; set; }
    
    public TableStatus Status { get; set; }
    
    // Navigation properties
    public virtual Restaurant Restaurant { get; set; } = null!;
    public virtual ICollection<Reservation> Reservations { get; set; } = [];
}

// Reservation.cs
public class Reservation : EntityBase
{
    public int Id { get; set; }
    
    public int TableId { get; set; }
    
    public int RestaurantId { get; set; }
    
    public int? CustomerId { get; set; }
    
    [Required]
    public DateTime ReservationTime { get; set; }
    
    [Range(1, 100)]
    public int PartySize { get; set; }
    
    public ReservationStatus Status { get; set; }
    
    [MaxLength(500)]
    public string? SpecialRequests { get; set; }
    
    public string? ContactPhone { get; set; }
    
    public string? ContactEmail { get; set; }
    
    // Navigation properties
    public virtual Table Table { get; set; } = null!;
    public virtual Restaurant Restaurant { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
}

public enum TableStatus
{
    Available,
    Reserved,
    Occupied,
    Maintenance,
    Cleaning
}

public enum ReservationStatus
{
    Pending,
    Confirmed,
    Seated,
    Completed,
    Cancelled,
    NoShow
}

// TimeSlot.cs
public class TimeSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int AvailableTables { get; set; }
    public int TotalCapacity { get; set; }
}
```

### 1.2 Entity Configuration
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Table>(entity =>
    {
        entity.HasIndex(e => new { e.RestaurantId, e.TableNumber })
              .IsUnique()
              .HasDatabaseName("IX_Tables_Restaurant_Number");
              
        entity.HasIndex(e => new { e.RestaurantId, e.IsAvailable, e.Capacity })
              .HasDatabaseName("IX_Tables_Restaurant_Availability_Capacity");
    });

    modelBuilder.Entity<Reservation>(entity =>
    {
        entity.HasIndex(e => new { e.RestaurantId, e.ReservationTime })
              .HasDatabaseName("IX_Reservations_Restaurant_Time");
              
        entity.HasIndex(e => new { e.TableId, e.ReservationTime })
              .HasDatabaseName("IX_Reservations_Table_Time");
    });
}
```

## 2. Service Layer Implementation

### 2.1 Reservation Service Interface
```csharp
public interface IReservationService
{
    Task<Result<List<TimeSlot>>> GetAvailableTimeSlots(
        int restaurantId, 
        DateTime date, 
        int partySize);
        
    Task<Result<Reservation>> CreateReservation(ReservationCreateDto dto);
    Task<Result<Reservation>> UpdateReservation(ReservationUpdateDto dto);
    Task<Result<bool>> CancelReservation(int reservationId);
    Task<Result<List<Reservation>>> GetUpcomingReservations(int restaurantId);
}

public class ReservationService : IReservationService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    
    public async Task<Result<List<TimeSlot>>> GetAvailableTimeSlots(
        int restaurantId, 
        DateTime date, 
        int partySize)
    {
        try
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.Tables)
                .FirstOrDefaultAsync(r => r.Id == restaurantId);

            if (restaurant == null)
                return Result<List<TimeSlot>>.Failure("Restaurant not found");

            var timeSlots = new List<TimeSlot>();
            var operatingHours = await GetOperatingHours(restaurant, date);

            foreach (var timeSlot in operatingHours)
            {
                var availableTables = await GetAvailableTables(
                    restaurantId, 
                    timeSlot, 
                    partySize);

                timeSlots.Add(new TimeSlot
                {
                    StartTime = timeSlot,
                    EndTime = timeSlot.AddHours(2),
                    AvailableTables = availableTables.Count,
                    TotalCapacity = availableTables.Sum(t => t.Capacity)
                });
            }

            return Result<List<TimeSlot>>.Success(timeSlots);
        }
        catch (Exception ex)
        {
            return Result<List<TimeSlot>>.Failure("Error getting available time slots");
        }
    }
}
```

## 3. Blazor Components

### 3.1 Table Layout Component
```razor
@* Components/Reservation/TableLayout.razor *@
@inherits ComponentBase
@inject ITableService TableService

<div class="table-layout">
    <div class="layout-controls">
        <button @onclick="ToggleEditMode">
            @(IsEditMode ? "View Mode" : "Edit Mode")
        </button>
    </div>

    <div class="layout-grid" 
         style="grid-template-columns: repeat(@Columns, 1fr);">
        @foreach (var table in Tables)
        {
            <div class="table-item @GetTableStatusClass(table)"
                 @onclick="() => HandleTableClick(table)">
                <span class="table-number">@table.TableNumber</span>
                <span class="capacity">@table.Capacity seats</span>
                @if (table.Status == TableStatus.Reserved)
                {
                    <span class="reservation-info">
                        Reserved: @GetNextReservation(table)
                    </span>
                }
            </div>
        }
    </div>

    @if (IsEditMode)
    {
        <TableEditor Table="@selectedTable"
                    OnSave="@HandleTableSave"
                    OnCancel="@(() => selectedTable = null)" />
    }
</div>

@code {
    private bool IsEditMode;
    private Table? selectedTable;
    private List<Table> Tables = [];

    [Parameter]
    public int RestaurantId { get; set; }

    [Parameter]
    public int Columns { get; set; } = 4;

    protected override async Task OnInitializedAsync()
    {
        await LoadTables();
    }

    private async Task LoadTables()
    {
        var result = await TableService.GetTables(RestaurantId);
        if (result.IsSuccess)
        {
            Tables = result.Value;
        }
    }
}
```

### 3.2 Reservation Calendar Component
```razor
@* Components/Reservation/ReservationCalendar.razor *@
@inject IReservationService ReservationService
@implements IDisposable

<div class="reservation-calendar">
    <div class="calendar-header">
        <div class="date-navigation">
            <button @onclick="PreviousWeek">&lt;</button>
            <h3>@StartDate.ToString("MMMM dd") - @EndDate.ToString("MMMM dd, yyyy")</h3>
            <button @onclick="NextWeek">&gt;</button>
        </div>
    </div>

    <div class="time-slots">
        @foreach (var day in GetWeekDays())
        {
            <div class="day-column">
                <h4>@day.ToString("ddd, MMM dd")</h4>
                @foreach (var slot in GetTimeSlots(day))
                {
                    <div class="time-slot @GetSlotClass(slot)"
                         @onclick="() => SelectTimeSlot(slot)">
                        <span class="time">@slot.StartTime.ToString("HH:mm")</span>
                        <span class="availability">
                            @slot.AvailableTables tables
                        </span>
                    </div>
                }
            </div>
        }
    </div>
</div>

@code {
    [Parameter]
    public int RestaurantId { get; set; }

    [Parameter]
    public DateTime StartDate { get; set; } = DateTime.Today;

    private DateTime EndDate => StartDate.AddDays(6);
    private List<TimeSlot> availableSlots = [];
    private HubConnection? hubConnection;

    protected override async Task OnInitializedAsync()
    {
        await LoadAvailability();
        await SetupSignalR();
    }
}
```

## 4. Integration Points

### 4.1 Table Tracking Service
```csharp
public class TableTrackingService
{
    private readonly IHubContext<TableHub> _hubContext;
    private readonly ITableService _tableService;
    
    public async Task UpdateTableStatus(int tableId, TableStatus status)
    {
        var table = await _tableService.UpdateTableStatus(tableId, status);
        
        if (table.IsSuccess)
        {
            await _hubContext.Clients
                .Group($"restaurant_{table.Value.RestaurantId}")
                .SendAsync("TableStatusUpdated", table.Value);
        }
    }
}
```

### 4.2 Capacity Planning Service
```csharp
public class CapacityPlanningService
{
    private readonly IReservationService _reservationService;
    private readonly ITableService _tableService;
    
    public async Task<Result<CapacityPlan>> GenerateCapacityPlan(
        int restaurantId, 
        DateTime date)
    {
        var tables = await _tableService.GetTables(restaurantId);
        var reservations = await _reservationService
            .GetReservations(restaurantId, date);
            
        return CalculateCapacityPlan(tables.Value, reservations.Value);
    }
}
```

## 5. Background Services

### 5.1 Reservation Cleanup Service
```csharp
public class ReservationCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var reservationService = scope.ServiceProvider
                .GetRequiredService<IReservationService>();
                
            await reservationService.HandleNoShows();
            await reservationService.CleanupOldReservations();
            
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

## Implementation Checklist

### Phase 1: Core Features
- [ ] Table management
- [ ] Basic reservations
- [ ] Availability checking
- [ ] Status tracking

### Phase 2: Advanced Features
- [ ] Real-time updates
- [ ] Capacity planning
- [ ] Waitlist management
- [ ] Table assignments

### Phase 3: Integration
- [ ] Customer notifications
- [ ] Staff notifications
- [ ] Order system integration
- [ ] Customer profile integration

### Phase 4: Optimization
- [ ] Performance tuning
- [ ] Analytics integration
- [ ] Reporting features
- [ ] Mobile support

## Monitoring and Maintenance

### Key Metrics
1. Reservation fulfillment rate
2. Table turnover time
3. Peak reservation times
4. No-show rate

### Regular Tasks
1. Table status verification
2. Reservation cleanup
3. Performance monitoring
4. Capacity optimization

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0   | TBD  | Initial implementation |
| 1.1.0   | TBD  | Real-time updates |
| 1.2.0   | TBD  | Capacity optimization |