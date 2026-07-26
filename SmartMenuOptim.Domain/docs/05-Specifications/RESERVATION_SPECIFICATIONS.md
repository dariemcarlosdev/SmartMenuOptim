# Reservation Specifications

## Overview

This document describes the Specification Pattern implementation for Reservation queries in the Domain layer. Specifications encapsulate query logic in a reusable, testable, and infrastructure-agnostic way, following Clean Architecture and Domain-Driven Design principles.

## 📋 Table of Contents

- [Architecture Placement](#architecture-placement)
- [Available Specifications](#available-specifications)
- [Specification Pattern Benefits](#specification-pattern-benefits)
- [Usage Examples](#usage-examples)
- [Clean Architecture Compliance](#clean-architecture-compliance)
- [Best Practices](#best-practices)
- [Testing](#testing)

---

## Architecture Placement

**Layer**: Domain  
**Namespace**: `SmartMenuOptim.Domain.Specifications`  
**File**: `ReservationSpecifications.cs`

```
┌─────────────────────────────────────────────────┐
│          Presentation Layer (API/UI)            │
└─────────────────┬───────────────────────────────┘
                  │ Uses
┌─────────────────▼───────────────────────────────┐
│          Application Layer                      │
│  - Uses Specifications via Repository           │
└─────────────────┬───────────────────────────────┘
                  │ Depends on
┌─────────────────▼───────────────────────────────┐
│          Domain Layer (Core)                    │
│  ✅ Specifications defined here                 │
│  ✅ IRepository<T> interface                    │
│  ✅ Business logic encapsulation                │
└─────────────────▲───────────────────────────────┘
                  │ Implements
┌─────────────────┴───────────────────────────────┐
│          Infrastructure Layer                   │
│  - Repository implementation                    │
│  - Applies specifications to queries            │
└─────────────────────────────────────────────────┘
```

---

## Available Specifications

### 1. ActiveReservationsSpecification

**Purpose**: Queries reservations that are currently active (Pending or Confirmed status) and not soft-deleted.

**Business Rule**: 
> "Active reservations are those awaiting confirmation or confirmed for a future date, excluding cancelled, completed, or soft-deleted entries."

**Implementation**:
```csharp
public class ActiveReservationsSpecification : BaseSpecification<Reservation>
{
    public ActiveReservationsSpecification() 
        : base(r => (r.Status == ReservationStatus.Pending || 
                     r.Status == ReservationStatus.Confirmed) 
                    && !r.IsDeleted)
    {
        // No additional ordering or includes needed for cleanup operations
    }
}
```

**Filters**:
- ✅ Status = `Pending` OR `Confirmed`
- ✅ `IsDeleted` = `false`

**Use Cases**:
- Automated cleanup background jobs
- Capacity planning queries
- Active booking management
- Customer notification systems

---

### 2. NonDeletedReservationsSpecification

**Purpose**: Queries all reservations that have not been soft-deleted, regardless of status.

**Business Rule**: 
> "Include all reservation records that are part of the active dataset, excluding only soft-deleted entries."

**Implementation**:
```csharp
public class NonDeletedReservationsSpecification : BaseSpecification<Reservation>
{
    public NonDeletedReservationsSpecification() 
        : base(r => !r.IsDeleted)
    {
        // No additional configuration needed
    }
}
```

**Filters**:
- ✅ `IsDeleted` = `false`

**Use Cases**:
- Reservation statistics and reporting
- Status distribution analysis
- Historical data queries
- Audit reports

---

## Specification Pattern Benefits

### 🎯 **1. Domain-Centric Query Logic**
- Query rules defined in the Domain layer where business logic belongs
- No leakage of query logic into Application or Infrastructure layers

### ♻️ **2. Reusability**
- Single definition used across multiple use cases
- Consistent query behavior throughout the application

### ✅ **3. Testability**
- Specifications can be unit tested independently
- Easy to verify business rules without database

### 🔌 **4. Infrastructure Independence**
- No dependency on Entity Framework, SQL, or any specific ORM
- Specifications work with any IRepository implementation

### 📚 **5. Expressiveness**
- Self-documenting code - specification names express intent
- Easier to understand than scattered LINQ queries

### 🛡️ **6. Maintainability**
- Centralized query logic - change once, apply everywhere
- Reduces code duplication

---

## Usage Examples

### Example 1: Using in Application Service (Recommended)

```csharp
public class ReservationAutoCleanupService : IReservationCleanupService
{
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IUnityOfWork _unitOfWork;

    public async Task<CleanupResult> ExecuteCleanupAsync(
        int pendingExpirationHours = 24,
        CancellationToken cancellationToken = default)
    {
        // ✅ Use specification to fetch active reservations
        var activeReservationsSpec = new ActiveReservationsSpecification();
        var activeReservations = await _reservationRepository
            .FindAsync(activeReservationsSpec);

        // Process reservations...
        
        await _unitOfWork.SaveChangesAsync();
        
        return result;
    }
}
```

### Example 2: Statistics and Reporting

```csharp
public async Task<ReservationStatistics> GetStatisticsAsync()
{
    // Get all non-deleted reservations for analysis
    var spec = new NonDeletedReservationsSpecification();
    var allReservations = await _reservationRepository.FindAsync(spec);

    return new ReservationStatistics
    {
        TotalReservations = allReservations.Count(),
        PendingCount = allReservations.Count(r => r.Status == ReservationStatus.Pending),
        ConfirmedCount = allReservations.Count(r => r.Status == ReservationStatus.Confirmed),
        CompletedCount = allReservations.Count(r => r.Status == ReservationStatus.Completed),
        CancelledCount = allReservations.Count(r => r.Status == ReservationStatus.Cancelled),
        NoShowCount = allReservations.Count(r => r.Status == ReservationStatus.NoShow)
    };
}
```

### Example 3: Combining with Domain Services

```csharp
public class ReservationManagementService
{
    private readonly IRepository<Reservation> _repository;

    public async Task<List<Reservation>> GetExpiredPendingReservationsAsync(
        int expirationHours)
    {
        // ✅ Get active reservations using specification
        var activeSpec = new ActiveReservationsSpecification();
        var activeReservations = await _repository.FindAsync(activeSpec);

        // ✅ Apply additional domain logic
        var cutoffTime = DateTime.UtcNow.AddHours(-expirationHours);
        return activeReservations
            .Where(r => r.Status == ReservationStatus.Pending && 
                       r.CreatedAt < cutoffTime)
            .ToList();
    }
}
```

---

## Clean Architecture Compliance

### ✅ **Dependency Rule**
- Domain layer has **NO** dependencies on outer layers
- Application and Infrastructure depend **ON** Domain
- Specifications are pure domain concepts

### ✅ **SOLID Principles**

#### **Single Responsibility Principle (SRP)**
Each specification has one reason to change - the specific query rule it encapsulates.

#### **Open/Closed Principle (OCP)**
New query requirements = new specifications (extension), not modification of existing ones.

#### **Dependency Inversion Principle (DIP)**
- High-level modules (Application) depend on abstractions (ISpecification, IRepository)
- Low-level modules (Infrastructure) implement those abstractions

### ✅ **Domain-Driven Design**

#### **Ubiquitous Language**
Specification names use business terminology:
- `ActiveReservationsSpecification` - matches business concept of "active reservations"
- Not: `ReservationsByStatusAndDeletedFlagSpecification`

#### **Encapsulation**
Complex query logic encapsulated in cohesive specification classes.

---

## Best Practices

### ✅ **DO**

1. **Name specifications expressively**
   ```csharp
   ✅ ActiveReservationsSpecification
   ✅ ExpiredReservationsSpecification
   ❌ ReservationSpec1
   ```

2. **Keep specifications focused**
   - One clear query purpose per specification
   - Avoid "kitchen sink" specifications that do too much

3. **Use base class properly**
   ```csharp
   public class MySpecification : BaseSpecification<Reservation>
   {
       public MySpecification() : base(/* criteria expression */)
       {
           // Additional configuration (includes, ordering, etc.)
       }
   }
   ```

4. **Document business rules**
   - Include XML comments
   - Explain **why** the specification exists

5. **Compose specifications when needed**
   ```csharp
   var spec = new ActiveReservationsSpecification()
       .And(new ReservationsByRestaurantSpecification(restaurantId));
   ```

### ❌ **DON'T**

1. **Don't add infrastructure concerns**
   ```csharp
   ❌ Don't reference DbContext or Entity Framework types
   ❌ Don't include SQL-specific logic
   ```

2. **Don't make specifications too generic**
   ```csharp
   ❌ new GenericFilterSpecification<Reservation>(someExpression)
   ✅ new ActiveReservationsSpecification()
   ```

3. **Don't bypass specifications**
   ```csharp
   ❌ var reservations = repository.Query()
                                   .Where(r => !r.IsDeleted)
                                   .ToList();
   ✅ var spec = new NonDeletedReservationsSpecification();
      var reservations = await repository.FindAsync(spec);
   ```

---

## Testing

### Unit Testing Specifications

```csharp
public class ActiveReservationsSpecificationTests
{
    [Fact]
    public void Should_Include_Pending_Reservations()
    {
        // Arrange
        var spec = new ActiveReservationsSpecification();
        var reservation = new Reservation { Status = ReservationStatus.Pending, IsDeleted = false };
        var expression = spec.Criteria.Compile();

        // Act
        var result = expression(reservation);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Should_Include_Confirmed_Reservations()
    {
        // Arrange
        var spec = new ActiveReservationsSpecification();
        var reservation = new Reservation { Status = ReservationStatus.Confirmed, IsDeleted = false };
        var expression = spec.Criteria.Compile();

        // Act
        var result = expression(reservation);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Should_Exclude_Deleted_Reservations()
    {
        // Arrange
        var spec = new ActiveReservationsSpecification();
        var reservation = new Reservation { Status = ReservationStatus.Pending, IsDeleted = true };
        var expression = spec.Criteria.Compile();

        // Act
        var result = expression(reservation);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Should_Exclude_Cancelled_Reservations()
    {
        // Arrange
        var spec = new ActiveReservationsSpecification();
        var reservation = new Reservation { Status = ReservationStatus.Cancelled, IsDeleted = false };
        var expression = spec.Criteria.Compile();

        // Act
        var result = expression(reservation);

        // Assert
        result.Should().BeFalse();
    }
}
```

---

## Related Documentation

- [Specification Pattern Base Implementation](./SPECIFICATION_PATTERN.md)
- [Repository Pattern](../repositories/REPOSITORY_PATTERN.md)
- [Domain Services](../DOMAIN_SERVICE.md)
- [Unit of Work Pattern](../repositories/UNIT_OF_WORK.md)

---

## Version History

| Version | Date       | Author | Changes |
|---------|------------|--------|---------|
| 1.0     | 2024-01-XX | Development Team | Initial creation of reservation specifications |

---

## References

- **Specification Pattern**: Eric Evans, Domain-Driven Design
- **Clean Architecture**: Robert C. Martin
- **SOLID Principles**: Robert C. Martin

---

**Last Updated**: January 2024  
**Maintained By**: SmartMenuOptim Development Team
