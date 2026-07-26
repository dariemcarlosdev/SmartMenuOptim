# 🚀 Quick Start Guide - Reservation Auto-Cleanup

## ⚡ TL;DR

This implementation adds automatic cleanup of expired/no-show reservations plus comprehensive reporting.

## 🎯 Run Migration (REQUIRED)

```powershell
# From solution root

1. Create migration
$env:ConnectionStrings__DefaultConnection="Server=localhost;Port=5432;Database=SmartMenuDb;User Id=postgres;Password=admin123;TrustServerCertificate=True;"; dotnet ef migrations add <MigrationName> --context AppDbContext --project SmartMenuOptim.Infrastructure --startup-project SmartMenuOptim.API


2. Apply migration
$env:ConnectionStrings__DefaultConnection="Server=localhost;Port=5432;Database=SmartMenuDb;User Id=postgres;Password=admin123;TrustServerCertificate=True;"; dotnet ef database update --context AppDbContext --project SmartMenuOptim.Infrastructure --startup-project SmartMenuOptim.API

Notes: 
1. Replace <MigrationName> with a descriptive name like "AddReservationStatusAndCleanup".
2. Ensure the connection string matches your database setup. The connection string environment variable is needed cuz the CLI tools can't read from appsettings.json directly, since it is stored in user secrets and EF Core CLI doesn't support that.
3. This migration is REQUIRED to add the Reservation Status property and related configurations.
```

## 🔧 Configuration

**appsettings.json**:
```json
{
  "ReservationCleanup": {
    "IntervalHours": 6,         // Run cleanup every 6 hours
    "PendingExpirationHours": 24, // Cancel pending after 24 hours
    "Enabled": true              // Set false to disable
  }
}
```

## 📊 Use the API

### Get Statistics
```http
GET /api/ReservationReports/statistics?restaurantId=1
```

### Get Status Counts
```http
GET /api/ReservationReports/status-counts
```

### Get Time-Based Stats
```http
GET /api/ReservationReports/time-based?startDate=2024-01-01&endDate=2024-01-31
```

### Get Active Count
```http
GET /api/ReservationReports/active-count
```

## 🧪 Test It

```csharp
// Inject the service
public class MyController
{
    private readonly ReservationReportingService _reporting;
    
    public MyController(ReservationReportingService reporting)
    {
        _reporting = reporting;
    }
    
    public async Task<IActionResult> GetStats()
    {
        var stats = await _reporting.GetStatisticsAsync(restaurantId: 1);
        return Ok(stats);
    }
}
```

## 📁 Files Changed/Created

```
Domain/
├── Aggregates/TableAggregate/
│   └── Reservation.cs (Status property + methods)
├── Services/
│   ├── ReservationManagementService.cs (NEW)
│   ├── TableAvailabilityService.cs (logging added)
│   ├── PromotionEligibilityService.cs (logging added)
│   └── Contracts/
│       └── IReservationCleanupService.cs (NEW)
└── Specifications/
    └── ReservationSpecifications.cs (NEW)

Application/
└── Services/Reservations/
    ├── ReservationAutoCleanupService.cs (NEW)
    └── ReservationReportingService.cs (NEW)

Infrastructure/
├── BackgroundJobs/
│   └── ReservationAutoCleanupBackgroundService.cs (NEW)
└── Persistence/Context/
    └── AppDbContext.cs (Reservation config added)

API/
├── Controllers/
│   └── ReservationReportsController.cs (NEW)
└── appsettings.json (config added)
```

## 🔍 Monitor Logs

Watch for:
- `"Reservation Auto-Cleanup Background Service starting"`
- `"✅ Reservation cleanup completed successfully"`
- `"Cancelled X/Y expired pending"`
- `"No-Show X/Y confirmed"`

## 🎓 Architecture

```
API Layer (ReservationReportsController)
    ↓
Application Layer (ReservationAutoCleanupService, ReservationReportingService)
    ↓
Domain Layer (ReservationManagementService, Specifications)
    ↓
Infrastructure Layer (Background Service, Repository)
```

## ❓ Troubleshooting

**Build errors / Assembly binding issues?**
- Ensure all EF Core packages use the same version (8.0.0 recommended for .NET 8)
- Run `dotnet restore` after changing package versions
- Ensure Microsoft.Extensions.* packages match your target framework version

**Background service not running?**
- Check `Enabled: true` in appsettings.json
- Check logs for startup message

**No cleanup happening?**
- Verify reservations are actually older than threshold
- Check specifications are correct
- Review logs for errors

**Build errors?**
- Ensure all NuGet packages restored
- Migration might be pending
- Check project references

## 📚 Full Documentation

- [Complete Guide](RESERVATION_AUTO_CLEANUP.md)
- [Implementation Summary](IMPLEMENTATION_SUMMARY.md)

---

**Status**: ✅ Build successful | ⚠️ Migration required
