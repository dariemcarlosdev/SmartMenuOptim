using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartMenuOptim.Application.Handlers.LoyaltyEventHandlers;
using SmartMenuOptim.Application.Handlers.MenuEventHandlers;
using SmartMenuOptim.Application.Handlers.OrderEventHandlers;
using SmartMenuOptim.Application.Handlers.SaleEventHandlers;
using SmartMenuOptim.Application.Features.Admin.Services;
using SmartMenuOptim.Application.Features.Reservations.Services;
using SmartMenuOptim.Application.Features.Restaurants.Services;
using SmartMenuOptim.Application.Features.AI.Contracts;
using SmartMenuOptim.Application.Features.AI.Services;
using SmartMenuOptim.Domain.Services.Abstractions;
using SmartMenuOptim.Application.Contracts;

namespace SmartMenuOptim.Application.Extensions;

/// <summary>
/// Provides extension methods for registering Application layer services in the IServiceCollection.
/// </summary>
/// <remarks>
/// <para><strong>Application Layer Registration:</strong></para>
/// <para>This class registers all Application layer services including:</para>
/// <list type="bullet">
///     <item><description><strong>MediatR:</strong> For CQRS pattern and domain event handling</description></item>
///     <item><description><strong>Event Handlers:</strong> For processing domain events</description></item>
///     <item><description><strong>Application Services:</strong> For use case orchestration</description></item>
///     <item><description><strong>Validators:</strong> For FluentValidation request validation</description></item>
/// </list>
/// </remarks>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Application layer services including MediatR, event handlers, and application services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR and all handlers from this assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);

        // Register application services (use cases and orchestration)
        services.AddScoped<IAImprovementStrategyService, AiImprovementService>();
        services.AddScoped<IAdminAuthorizationService, AdminAuthorizationService>();

        // Reservation Management Application Services
        services.AddScoped<IReservationCleanupService, ReservationAutoCleanupService>();
        services.AddScoped<ReservationReportingService>();

        // Restaurant Management Application Services (Phase 2)
        services.AddScoped<IRestaurantService, RestaurantService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IDishService, DishService>();

        return services;
    }
}
