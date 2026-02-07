using Microsoft.Extensions.DependencyInjection;
using SmartMenuOptim.Domain.Services;

namespace SmartMenuOptim.Domain.Extensions;

/// <summary>
/// Provides extension methods for registering Domain layer services in the IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Domain layer services including business logic and domain services.
    /// </summary>
    /// <remarks>
    /// Domain Services are part of the Domain layer and contain business logic that:
    /// - Doesn't naturally fit within an Entity or Value Object
    /// - Operates on multiple domain objects
    /// - Contains core business rules and domain logic
    /// 
    /// Examples of Domain Services in this application:
    /// - ReviewSentimentAnalysisService: Orchestrates sentiment analysis business rules for reviews
    /// - MenuPricingService: Implements pricing strategies and business rules for menu items
    /// 
    /// NOTE: Do NOT register here:
    /// - Application Services (use Application layer ServiceCollectionExtensions)
    /// - Infrastructure adapters/implementations (use Infrastructure layer ServiceCollectionExtensions)
    /// - External service ports/interfaces (define in Domain, implement in Infrastructure)
    /// </remarks>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The configured IServiceCollection.</returns>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Register Domain services (business logic)
        // These services contain pure business logic without external dependencies
        
        // Customer & Review Analysis Services
        services.AddScoped<ReviewSentimentAnalysisService>();
        
        // Menu & Dish Management Services
        services.AddScoped<MenuPricingService>();
        services.AddScoped<MenuOptimizationService>();
        services.AddScoped<DishPopularityRankingService>();
        
        // Inventory & Forecasting Services
        services.AddScoped<InventoryForecastingService>();
        
        // Financial & Revenue Analysis Services
        services.AddScoped<RevenueAnalysisService>();
        
        // Promotion & Marketing Services
        services.AddScoped<PromotionEligibilityService>();
        
        // Table & Reservation Management Services
        services.AddScoped<TableAvailabilityService>();
        services.AddScoped<ReservationManagementService>();

        return services;
    }
}
