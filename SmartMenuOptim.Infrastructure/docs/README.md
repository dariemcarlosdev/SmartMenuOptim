# SmartMenuOptim Infrastructure Layer Documentation

> **Clean Architecture - Infrastructure Layer Documentation**

This folder contains comprehensive documentation for the Infrastructure Layer of SmartMenuOptim, organized by architectural concerns following Clean Architecture principles.

---

## 📁 Documentation Structure

```
SmartMenuOptim.Infrastructure/docs/
├── 📄 README.md                          ← You are here
├── 📄 SMARTMENU.INFRASTRUCTURE_RESUME.md ← Layer overview
├── 📁 01-Persistence/                    ← Database Context & EF Core
├── 📁 02-Repositories/                   ← Repository Pattern Implementation
├── 📁 03-Migrations/                     ← EF Core Migrations Guide
├── 📁 04-ValueObjectMapping/             ← Value Object to DB Mapping
├── 📁 05-Verification/                   ← Testing & Verification Checklists
├── 📁 06-BackgroundJobs/                 ← Background Jobs & Hosted Services
├── 📁 07-Middleware/                     ← HTTP Pipeline Middleware
└── 📁 08-EventDispatching/               ← Domain Event Dispatching
```

---

## 📚 Documentation Index

### 📁 01-Persistence
*Database context and Entity Framework Core configuration*

| Document | Description |
|----------|-------------|
| [DATABASE_CONTEXT.md](01-Persistence/DATABASE_CONTEXT.md) | `AppDbContext` configuration and entity mappings |

---

### 📁 02-Repositories
*Repository pattern implementation*

| Document | Description |
|----------|-------------|
| [REPOSITORY_PATTERN_DESIGN.md](02-Repositories/REPOSITORY_PATTERN_DESIGN.md) | Generic repository and Unit of Work patterns |
| [REPOSITORY_REFACTORING_GUIDE.md](02-Repositories/REPOSITORY_REFACTORING_GUIDE.md) | Repository refactoring guide |
| [BEFORE_AFTER_REPOSITORY_PATTERN_REFACTORING.md](02-Repositories/BEFORE_AFTER_REPOSITORY_PATTERN_REFACTORING.md) | Before/after refactoring comparison |
| [CLEAN_ARCH_REPOSITORY_REFACTORING.md](02-Repositories/CLEAN_ARCH_REPOSITORY_REFACTORING.md) | Clean Architecture repository patterns |

---

### 📁 03-Migrations
*Entity Framework Core migrations*

| Document | Description |
|----------|-------------|
| [MIGRATION_GUIDE.md](03-Migrations/MIGRATION_GUIDE.md) | EF Core migration commands and best practices |
| [EF_MIGRATION_GUIDE.md](03-Migrations/EF_MIGRATION_GUIDE.md) | Detailed EF migration guide |

---

### 📁 04-ValueObjectMapping
*Mapping domain value objects to database*

| Document | Description |
|----------|-------------|
| [EF_CORE_VALUE_OBJECT_RESOLUTION.md](04-ValueObjectMapping/EF_CORE_VALUE_OBJECT_RESOLUTION.md) | Value object mapping strategies |
| [VALUE_OBJECT_FINAL_RESOLUTION.md](04-ValueObjectMapping/VALUE_OBJECT_FINAL_RESOLUTION.md) | Final resolution for complex value objects |

---

### 📁 05-Verification
*Testing and verification checklists*

| Document | Description |
|----------|-------------|
| [VERIFICATION_CHECKLIST.md](05-Verification/VERIFICATION_CHECKLIST.md) | Pre-deployment verification checklist |

---

### 📁 06-BackgroundJobs
*Background jobs and hosted services*

| Document | Description |
|----------|-------------|
| [RESERVATION_AUTO_CLEANUP_BACKGROUND_JOB.md](06-BackgroundJobs/RESERVATION_AUTO_CLEANUP_BACKGROUND_JOB.md) | Reservation cleanup job documentation |
| [RESERVATION_CLEANUP_QUICK_START.md](06-BackgroundJobs/RESERVATION_CLEANUP_QUICK_START.md) | Quick start guide for cleanup job |
| [RESERVATION_STATUS_AUTOCLEANUP_IMPLEMENTATION.md](06-BackgroundJobs/RESERVATION_STATUS_AUTOCLEANUP_IMPLEMENTATION.md) | Implementation details |

---

### 📁 07-Middleware
*HTTP pipeline middleware components*

| Document | Description |
|----------|-------------|
| [GLOBAL_EXCEPTION_HANDLING_MIDDLEWARE.md](07-Middleware/GLOBAL_EXCEPTION_HANDLING_MIDDLEWARE.md) | Centralized exception handling with domain exception integration |

---

### 📁 08-EventDispatching
*Domain event dispatching mechanism using MediatR*

| Document | Description |
|----------|-------------|
| [DOMAIN_EVENT_DISPATCHING_MECHANISM.md](08-EventDispatching/DOMAIN_EVENT_DISPATCHING_MECHANISM.md) | MediatR-based event dispatching, handlers, and dead letter queue |

---

## 🏗️ Architecture Overview

### Infrastructure Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────┐
│                   INFRASTRUCTURE LAYER                      │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────┐   │
│  │              Persistence (01-*)                      │   │
│  │  AppDbContext, Entity Configurations, Converters    │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              Repositories (02-*)                     │   │
│  │  IRepository<T>, UnitOfWork, Specification Support  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              External Services                       │   │
│  │  Azure AI, OpenAI, Email, Payment Gateways          │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              Background Jobs (06-*)                  │   │
│  │  IHostedService implementations, Scheduled Tasks   │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              Middleware (07-*)                       │   │
│  │  GlobalExceptionHandling, Domain Exception Mapping │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              Event Dispatching (08-*)                │   │
│  │  MediatRDomainEventDispatcher, Dead Letter Queue   │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Hexagonal Architecture (Ports & Adapters)

| Component | Type | Example |
|-----------|------|---------|
| **Ports** | Interfaces (Domain) | `ISentimentAnalyzer`, `IRepository<T>` |
| **Adapters** | Implementations (Infrastructure) | `SentimentService`, `Repository<T>` |

---

## 🔌 Key Infrastructure Components

### Database & Persistence

| Component | File | Description |
|-----------|------|-------------|
| `AppDbContext` | `Persistence/Context/AppDbContext.cs` | Main EF Core context |
| `Repository<T>` | `Persistence/Repositories/Repository.cs` | Generic repository |
| `UnityOfWork` | `Persistence/Repositories/UnityOfWork.cs` | Transaction management |

### External Service Adapters

| Adapter | Interface (Port) | Implementation |
|---------|-----------------|----------------|
| Sentiment Analysis | `ISentimentAnalyzer` | `SentimentService` (Azure AI) |
| AI Text Generation | `IAiTextGenerator` | `OpenIaGptService` (OpenAI) |

### Background Jobs

| Job | Description |
|-----|-------------|
| `ReservationAutoCleanupBackgroundService` | Cleans expired reservations |

### Middleware

| Middleware | File | Description |
|------------|------|-------------|
| `GlobalExceptionHandlingMiddleware` | `Infrastructure/Middlewares/ExceptionHandlingMiddleware.cs` | Maps domain exceptions to HTTP status codes |

### Event Dispatching

| Component | File | Description |
|-----------|------|-------------|
| `MediatRDomainEventDispatcher` | `EventDispatching/MediatRDomainEventDispatcher.cs` | Publishes domain events via MediatR |
| `InMemoryDeadLetterQueueService` | `Services/DeadLetterQueue/InMemoryDeadLetterQueueService.cs` | Stores failed events for retry |

---

## 🚀 Quick Start

### For New Developers

1. Read [DATABASE_CONTEXT.md](01-Persistence/DATABASE_CONTEXT.md) - Understand EF Core setup
2. Review [REPOSITORY_PATTERN_DESIGN.md](02-Repositories/REPOSITORY_PATTERN_DESIGN.md) - Repository pattern
3. Check [MIGRATION_GUIDE.md](03-Migrations/MIGRATION_GUIDE.md) - Database migrations

### For Database Changes

1. Modify entity configurations in `AppDbContext`
2. Create migration: `dotnet ef migrations add <Name>`
3. Apply migration: `dotnet ef database update`
4. Update [MIGRATION_GUIDE.md](03-Migrations/MIGRATION_GUIDE.md) if needed

### For External Service Integration

1. Define interface (port) in Domain layer
2. Implement adapter in Infrastructure layer
3. Register in `ServiceCollectionExtensions.cs`
4. Document the integration

---

## 📖 Related Documentation

| Layer | Location | Description |
|-------|----------|-------------|
| **Domain** | `SmartMenuOptim.Domain/docs/` | Entities, Aggregates, Value Objects |
| **Application** | `SmartMenuOptim.Application/docs/` | Application services, DTOs |
| **Root** | `docs/` | Solution-wide documentation |

---

## 🔧 Common Tasks

### Adding a New Repository

```csharp
// 1. Interface already exists: IRepository<T> in Domain layer
// 2. Use generic repository or create specific implementation

// For specific queries, use Specifications:
public class MyEntitySpecification : BaseSpecification<MyEntity>
{
    public MyEntitySpecification(int id) 
        : base(e => e.Id == id)
    {
        AddInclude(e => e.RelatedEntity);
    }
}
```

### Adding a New External Service

```csharp
// 1. Define interface in Domain/Services/Contracts/
public interface IMyExternalService
{
    Task<Result> DoSomethingAsync();
}

// 2. Implement in Infrastructure/Services/
public class MyExternalService : IMyExternalService
{
    public async Task<Result> DoSomethingAsync() { ... }
}

// 3. Register in Infrastructure/Extensions/ServiceCollectionExtensions.cs
services.AddScoped<IMyExternalService, MyExternalService>();
```

---

## 🔄 Documentation Updates

When updating infrastructure documentation:

1. Place new docs in the appropriate numbered folder
2. Update this README index
3. Follow existing naming conventions (UPPERCASE_WITH_UNDERSCORES.md)
4. Include Clean Architecture context in each document

---

*Last Updated: February 2025*
