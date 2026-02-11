# 🔄 Reservation Auto-Cleanup Background Job

## 📋 Overview

Automated background service that periodically cleans up expired pending reservations and marks no-show reservations following Clean Architecture and DDD principles.

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    API Layer (Presentation)                  │
│  • ReservationReportsController (Statistics endpoints)      │
│  • Swagger/OpenAPI documentation                             │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│              Application Layer (Orchestration)               │
│  • ReservationAutoCleanupService (implements interface)      │
│  • ReservationReportingService (statistics & analytics)      │
│  • CleanupResult (DTO)                                       │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│               Domain Layer (Business Logic)                  │
│  • ReservationManagementService (cleanup rules)              │
│  • IReservationCleanupService (contract)                     │
│  • ReservationSpecifications (query logic)                   │
│  • Reservation aggregate (status transitions)                │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│           Infrastructure Layer (Implementation)              │
│  • ReservationAutoCleanupBackgroundService (IHostedService)  │
│  • Repository pattern (data access)                          │
│  • Unit of Work (transactions)                               │
└─────────────────────────────────────────────────────────────┘
```

## 📁 File Structure

```
SmartMenuOptim.Domain/
├── Aggregates/TableAggregate/
│   └── Reservation.cs (Status enum & behavioral methods)
├── Services/
│   ├── ReservationManagementService.cs (Business logic)
│   └── Contracts/
│       └── IReservationCleanupService.cs (Service contract)
└── Specifications/
    └── ReservationSpecifications.cs (Query specifications)

SmartMenuOptim.Application/
├── Services/Reservations/
│   ├── ReservationAutoCleanupService.cs (Orchestration)
│   └── ReservationReportingService.cs (Statistics/Analytics)
└── Extensions/
    └── ServiceCollectionExtensions.cs (DI registration)

SmartMenuOptim.Infrastructure/
├── BackgroundJobs/
│   └── ReservationAutoCleanupBackgroundService.cs (Hosted service)
└── Extensions/
    └── ServiceCollectionExtensions.cs (Background job registration)

SmartMenuOptim.API/
├── Controllers/
│   └── ReservationReportsController.cs (Statistics endpoints)
└── appsettings.json (Configuration)
```

## ⚙️ Configuration

### appsettings.json

```json
{
  "ReservationCleanup": {
    "IntervalHours": 6,
    "PendingExpirationHours": 24,
    "Enabled": true
  }
}
```

### Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IntervalHours` | int | 6 | Hours between cleanup executions |
| `PendingExpirationHours` | int | 24 | Hours after which pending reservations expire |
| `Enabled` | bool | true | Enable/disable the background service |

### Environment-Specific Configuration

**appsettings.Development.json** (disable for local dev):
```json
{
  "ReservationCleanup": {
    "Enabled": false
  }
}
```

**appsettings.Production.json** (run every 4 hours):
```json
{
  "ReservationCleanup": {
    "IntervalHours": 4,
    "PendingExpirationHours": 24,
    "Enabled": true
  }
}
```

## 🔧 Business Rules

### 1. Expired Pending Reservations

**Rule**: Pending reservations not confirmed within threshold are auto-cancelled.

- **Threshold**: Configurable (default 24 hours)
- **Basis**: `CreatedAt` timestamp
- **Action**: Status changed from `Pending` → `Cancelled`
- **Benefit**: Prevents table blocking, improves availability

### 2. No-Show Reservations

**Rule**: Confirmed reservations past their time with no customer arrival.

- **Grace Period**: 15 minutes after reservation time
- **Status**: `Confirmed` → `NoShow`
- **Purpose**: Track customer reliability, free up tables

### 3. State Machine Validation

All status transitions go through domain aggregate methods:
- `Cancel()` - Validates terminal states
- `MarkNoShow()` - Ensures proper status flow
- Prevents invalid state transitions

## 📊 Reporting & Statistics API

### Endpoints

#### 1. Get Comprehensive Statistics
```http
GET /api/ReservationReports/statistics?restaurantId={id}
```

**Response:**
```json
{
  "generatedAt": "2024-01-26T10:30:00Z",
  "restaurantId": 1,
  "totalReservations": 150,
  "activeReservationsCount": 45,
  "upcomingReservationsCount": 30,
  "statusDistribution": {
    "Pending": 15,
    "Confirmed": 25,
    "Seated": 5,
    "Completed": 80,
    "Cancelled": 20,
    "NoShow": 5
  },
  "completionRate": 0.762,
  "cancellationRate": 0.190,
  "noShowRate": 0.048,
  "averageLeadTimeDays": 3.5,
  "registeredCustomerReservations": 120,
  "walkInReservations": 30
}
```

#### 2. Get Status Counts
```http
GET /api/ReservationReports/status-counts?restaurantId={id}
```

**Response:**
```json
{
  "Pending": 15,
  "Confirmed": 25,
  "Seated": 5,
  "Completed": 80,
  "Cancelled": 20,
  "NoShow": 5
}
```

#### 3. Get Time-Based Statistics
```http
GET /api/ReservationReports/time-based?startDate=2024-01-01&endDate=2024-01-31&restaurantId={id}
```

**Response:**
```json
{
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-01-31T23:59:59Z",
  "totalReservations": 120,
  "reservationsByDay": {
    "2024-01-15": 8,
    "2024-01-16": 12,
    ...
  },
  "reservationsByHour": {
    "12": 5,
    "18": 25,
    "19": 30,
    ...
  },
  "peakDay": "2024-01-20T00:00:00Z",
  "peakHour": 19
}
```

#### 4. Get Active Count
```http
GET /api/ReservationReports/active-count?restaurantId={id}
```

**Response:**
```json
45
```

## 🚀 Usage Examples

### Domain Service - Business Logic

```csharp
// Identify expired pending reservations
var reservationManagement = new ReservationManagementService(logger);
var expired = reservationManagement.IdentifyExpiredPendingReservations(
    allReservations, 
    expirationHours: 24
);

// Cancel them using domain aggregate methods
var cancelledCount = reservationManagement.CancelExpiredReservations(expired);

// Get statistics
var stats = reservationManagement.GetReservationStatistics(allReservations);
```

### Application Service - Orchestration

```csharp
// Inject via DI
public class MyService
{
    private readonly IReservationCleanupService _cleanupService;
    private readonly ReservationReportingService _reportingService;
    
    public MyService(
        IReservationCleanupService cleanupService,
        ReservationReportingService reportingService)
    {
        _cleanupService = cleanupService;
        _reportingService = reportingService;
    }
    
    public async Task ProcessCleanup()
    {
        // Execute cleanup
        var result = await _cleanupService.ExecuteCleanupAsync(24);
        
        if (result.Success)
        {
            Console.WriteLine($"Cancelled: {result.CancelledPendingCount}");
            Console.WriteLine($"No-Show: {result.MarkedAsNoShowCount}");
        }
        
        // Get statistics
        var stats = await _reportingService.GetStatisticsAsync(restaurantId: 1);
        Console.WriteLine($"Total: {stats.TotalReservations}");
        Console.WriteLine($"No-Show Rate: {stats.NoShowRate:P}");
    }
}
```

### Background Service - Automatic Execution

The background service runs automatically:
- Starts 1 minute after application startup
- Executes cleanup every `IntervalHours`
- Handles errors with exponential backoff
- Logs all operations

**View logs:**
```
Reservation Auto-Cleanup Background Service starting. Interval: 6 hours, Pending Expiration: 24 hours, Enabled: True
Starting scheduled reservation cleanup cycle
✅ Reservation cleanup completed successfully: Cancelled 5/8 expired pending, NoShow 2/3 confirmed (Duration: 1.2s)
Reservation cleanup cycle complete. Next execution in 6 hours
```

## 🧪 Testing

### Unit Tests (Domain Layer)

```csharp
[Fact]
public void IdentifyExpiredPendingReservations_ShouldReturnExpired()
{
    // Arrange
    var service = new ReservationManagementService(NullLogger);
    var oldReservation = CreatePendingReservation(createdAt: DateTime.UtcNow.AddDays(-2));
    var newReservation = CreatePendingReservation(createdAt: DateTime.UtcNow.AddHours(-12));
    
    // Act
    var expired = service.IdentifyExpiredPendingReservations(
        new[] { oldReservation, newReservation },
        expirationHours: 24
    );
    
    // Assert
    Assert.Single(expired);
    Assert.Equal(oldReservation.Id, expired[0].Id);
}
```

### Integration Tests (Application Layer)

```csharp
[Fact]
public async Task ExecuteCleanupAsync_ShouldCancelExpiredAndMarkNoShow()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var cleanupService = scope.ServiceProvider.GetRequiredService<IReservationCleanupService>();
    
    await SeedExpiredReservations();
    
    // Act
    var result = await cleanupService.ExecuteCleanupAsync(24);
    
    // Assert
    Assert.True(result.Success);
    Assert.Equal(3, result.CancelledPendingCount);
    Assert.Equal(2, result.MarkedAsNoShowCount);
}
```

## 🔍 Monitoring

### Application Insights Queries

**Cleanup execution frequency:**
```kql
traces
| where message contains "Reservation cleanup completed successfully"
| summarize count() by bin(timestamp, 1h)
```

**Cancellation trends:**
```kql
traces
| where message contains "Cancelled"
| parse message with * "Cancelled " cancelled:int "/" total:int *
| project timestamp, cancelled, total
| render timechart
```

**No-show rates:**
```kql
traces
| where message contains "No-Show"
| parse message with * "No-Show " marked:int "/" identified:int *
| extend NoShowRate = todouble(marked) / todouble(identified)
| project timestamp, NoShowRate
| render timechart
```

## 📈 Performance Considerations

1. **Batch Size**: Processes all active reservations in one transaction
2. **Indexing**: Uses specifications with optimized queries
3. **Transaction Scope**: Single transaction per cleanup cycle
4. **Error Recovery**: Exponential backoff on failures
5. **Resource Usage**: Runs during off-peak hours (configurable)

## 🔒 Security

- ✅ Requires authentication for API endpoints
- ✅ Multi-tenant isolation via `RestaurantId`
- ✅ Audit trail via `UpdatedAt` timestamps
- ✅ Validation through domain aggregate methods

## 🐛 Troubleshooting

### Background Service Not Running

**Check configuration:**
```bash
# Verify Enabled = true
dotnet user-secrets list | grep "ReservationCleanup:Enabled"
```

**Check logs:**
```
Reservation cleanup is DISABLED via configuration. Service will not run.
```

### No Reservations Being Cleaned

**Verify threshold:**
- Check `PendingExpirationHours` setting
- Ensure reservations are actually expired
- Review `CreatedAt` timestamps in database

**Check specifications:**
```sql
SELECT * FROM "Reservations" 
WHERE "Status" = 0 -- Pending
  AND "CreatedAt" < NOW() - INTERVAL '24 hours'
  AND "IsDeleted" = false;
```

## 📚 Related Documentation

- [Domain Services Guide](../../SmartMenuOptim.Domain/docs/DOMAIN_SERVICE.md)
- [Specification Pattern](../../SmartMenuOptim.Domain/Specifications/README.md)
- [Clean Architecture Overview](../../docs/ARCHITECTURE.md)

## 🎯 Future Enhancements

- [ ] Notification system for cancelled reservations
- [ ] Configurable no-show grace period
- [ ] Restaurant-specific cleanup policies
- [ ] Dashboard with real-time statistics
- [ ] Scheduled reports via email
- [ ] Integration with table management system
