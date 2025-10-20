# SmartMenuOptim.Infrastructure_Resume

## Project Intent
This project contains infrastructure implementations for SmartMenuOptim, such as logging, HTTP clients, and other cross-cutting concerns. It provides concrete implementations for abstractions defined in the shared/domain projects.

## Clean Architecture Guidance
- **Infrastructure Layer:**
  - **Data Access Implementations:** Concrete classes for repositories or data sources, such as `EfCoreRestaurantRepository` for database access using Entity Framework Core.
  - **Logging Implementations:** Integrations with logging frameworks, e.g., `SerilogLogger` or adapters for Microsoft.Extensions.Logging.
  - **External Service Integrations:** Classes for communicating with APIs, cloud storage, or messaging systems, such as `EmailSenderService` or `AzureBlobStorageService`.
  - **Cross-Cutting Concerns:** Implementations for caching, authentication, or background jobs, e.g., `RedisCacheService` or `JwtTokenProvider`.

Each implementation should fulfill an interface defined in the shared or domain layer, keeping business logic out of the infrastructure project.

## What Should Be Included
- Logging implementations
- HTTP client implementations
- Infrastructure services (e.g., file storage, email, etc.)
- Dependency injection registrations for infrastructure services

## What Should NOT Be Included
- UI components
- API controllers
- Business/domain logic

---
This file describes the intent and boundaries of the SmartMenuOptim.Infrastructure project according to Clean Architecture principles.
