using Microsoft.Extensions.DependencyInjection;
using SmartMenuOptim.Application.Interfaces;
using SmartMenuOptim.Application.Services;
using SmartMenuOptim.Application.Services.Reservations;
using SmartMenuOptim.Domain.Services.Abstraction;
using SmartMenuOptim.Domain.Services.Contracts;

namespace SmartMenuOptim.Application.Extensions;

/// <summary>
/// Provides extension methods for registering Application layer services in the IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Application layer services including use cases, orchestration, and application-specific logic.
    /// </summary>
    /// <remarks>
    /// Application Services are part of the Application layer and are responsible for:
    /// - Orchestrating use cases and business workflows
    /// - Coordinating domain services, repositories, and infrastructure services
    /// - Transforming between DTOs and domain models
    /// - Implementing application-specific logic (not business rules)
    /// - Serving as entry points for use case execution
    /// 
    /// Examples of Application Services in this application:
    /// - AiImprovementService: Orchestrates AI-powered improvement suggestions for menu items and reviews
    /// - AdminAuthorizationService: Handles authorization logic for administrative operations
    /// - ReviewApplicationService: Coordinates review management use cases (CRUD operations, sentiment analysis)
    /// 
    /// Key differences:
    /// - Application Services: Use case orchestration, DTO transformation, coordination (this layer)
    /// - Domain Services: Pure business logic, domain rules (Domain layer)
    /// - Infrastructure Services: External service implementations like AI APIs, databases (Infrastructure layer)
    /// 
    /// NOTE: Application Services typically depend on:
    /// - Domain Services and Entities (for business logic)
    /// - Domain Ports/Interfaces (e.g., ISentimentAnalyzer, IAiTextGenerator)
    /// - Repositories (for data access)
    /// </remarks>
    /// <param name="services">The service collection to which the application services will be added.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application services (use cases and orchestration)
        // These services coordinate domain logic and infrastructure to fulfill use cases
        services.AddScoped<IAImprovementStrategyService, AiImprovementService>();
        services.AddScoped<IAdminAuthorizationService, AdminAuthorizationService>();
        
        // Reservation Management Application Services
        services.AddScoped<IReservationCleanupService, ReservationAutoCleanupService>();
        services.AddScoped<ReservationReportingService>();

        return services;
    }
}
