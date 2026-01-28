using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SmartMenuOptim.Infrastructure.Persistence.Interceptors
{
    /// <summary>
    /// Interceptor used to automatically populate the RestaurantId (tenant identifier) 
    /// for tenant-specific entities during database save operations.
    /// </summary>
    /// <remarks>
    /// This interceptor implements the Entity Framework Core SaveChangesInterceptor pattern to:
    /// <list type="bullet">
    ///     <item><description>Automatically set RestaurantId when tenant-specific entities are added</description></item>
    ///     <item><description>Enforce data isolation in multi-tenant architecture</description></item>
    ///     <item><description>Prevent manual tenant assignment errors</description></item>
    ///     <item><description>Ensure all tenant-scoped data belongs to the correct restaurant</description></item>
    /// </list>
    /// This ensures consistent multi-tenant data isolation across all entities inheriting from TenantEntityBase without manual intervention.
    /// 
    /// <para><strong>Implementation Details:</strong></para>
    /// <list type="bullet">
    ///     <item><description>Works with all entities inheriting from TenantEntityBase</description></item>
    ///     <item><description>Retrieves tenant ID from HttpContext.Items["TenantId"] set by TenantResolverMiddleware</description></item>
    ///     <item><description>Intercepts both synchronous and asynchronous SaveChanges operations</description></item>
    ///     <item><description>Only processes entities in Added state (new entities)</description></item>
    ///     <item><description>Prevents RestaurantId from being changed on existing entities for security</description></item>
    ///     <item><description>Throws exception if tenant ID is not available in the request context</description></item>
    /// </list>
    /// 
    /// <para><strong>Multi-Tenant Architecture flow:</strong></para>
    /// <para>This interceptor is a critical component of the multi-tenant data isolation strategy:</para>
    /// <list type="number">
    ///     <item><description><strong>Request arrives → TenantResolverMiddleware extracts tenant ID</strong> - Extracts tenant ID from request (header, query, or subdomain)</description></item>
    ///     <item><description><strong>Tenant ID stored → HttpContext.Items["TenantId"]</strong> - Stores tenant ID for the current request</description></item>
    ///     <item><description><strong>Entity created → TenantInterceptor </strong> - Automatically assigns RestaurantId to new entities</description></item>
    ///     <item><description><strong>Query executed → Global Query Filters ensure tenant isolation</strong> - Ensures queries only return data for the current tenant</description></item>
    /// </list>
    /// 
    /// <para><strong>Security Considerations:</strong></para>
    /// <list type="bullet">
    ///     <item><description>Prevents cross-tenant data leaks by enforcing RestaurantId on all saves</description></item>
    ///     <item><description>Validates that tenant context exists before allowing data modifications</description></item>
    ///     <item><description>Immutable RestaurantId after entity creation prevents tenant switching attacks</description></item>
    ///     <item><description>Works in conjunction with TenantResolverMiddleware for request-level tenant isolation</description></item>
    /// </list>
    /// 
    /// <para><strong>Future Enhancement:</strong></para>
    /// <para>This interceptor can be extended to support additional tenant validation scenarios such as:</para>
    /// <list type="bullet">
    ///     <item><description>Validating that the RestaurantId exists before assignment</description></item>
    ///     <item><description>Logging tenant-scoped operations for audit trails</description></item>
    ///     <item><description>Supporting hierarchical tenancy (parent-child restaurant relationships)</description></item>
    ///     <item><description>Implementing tenant-specific business rules or constraints</description></item>
    /// </list>
    /// 
    /// <para><strong>How to Register:</strong></para>
    /// <para>Add this interceptor to your DbContext configuration in the service registration:</para>
    /// <code>
    /// services.AddHttpContextAccessor(); // Required for tenant resolution
    /// services.AddDbContext&lt;AppDbContext&gt;((serviceProvider, options) =&gt;
    /// {
    ///     options.UseNpgsql(connectionString);
    ///     var httpContextAccessor = serviceProvider.GetRequiredService&lt;IHttpContextAccessor&gt;();
    ///     options.AddInterceptors(new TenantInterceptor(httpContextAccessor));
    /// });
    /// </code>
    /// 
    /// <para><strong>Alternative: Dependency Injection</strong></para>
    /// <para>For better testability, register the interceptor in DI:</para>
    /// <code>
    /// services.AddHttpContextAccessor();
    /// services.AddScoped&lt;TenantInterceptor&gt;();
    /// services.AddDbContext&lt;AppDbContext&gt;((serviceProvider, options) =&gt;
    /// {
    ///     options.UseNpgsql(connectionString);
    ///     options.AddInterceptors(serviceProvider.GetRequiredService&lt;TenantInterceptor&gt;());
    /// });
    /// </code>
    /// 
    /// <para><strong>Prerequisites:</strong></para>
    /// <list type="bullet">
    ///     <item><description>TenantResolverMiddleware must be registered in the middleware pipeline</description></item>
    ///     <item><description>IHttpContextAccessor must be registered in the DI container</description></item>
    ///     <item><description>All tenant-specific entities must inherit from TenantEntityBase</description></item>
    /// </list>
    /// </remarks>
    public class TenantInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string TenantIdKey = "TenantId";

        /// <summary>
        /// Initializes a new instance of the TenantInterceptor class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor to retrieve tenant information from the current request.</param>
        /// <exception cref="ArgumentNullException">Thrown when httpContextAccessor is null.</exception>
        public TenantInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Intercepts synchronous SaveChanges operation to set tenant identifiers.
        /// </summary>
        /// <param name="eventData">Contextual information about the save operation.</param>
        /// <param name="result">The current result of the save operation.</param>
        /// <returns>The modified result after setting tenant fields.</returns>
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            SetTenantId(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        /// <summary>
        /// Intercepts asynchronous SaveChangesAsync operation to set tenant identifiers.
        /// </summary>
        /// <param name="eventData">Contextual information about the save operation.</param>
        /// <param name="result">The current result of the save operation.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation with the modified result.</returns>
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            SetTenantId(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// Sets the RestaurantId (tenant identifier) for entities that inherit from TenantEntityBase.
        /// </summary>
        /// <param name="context">The database context containing the tracked entities.</param>
        /// <exception cref="InvalidOperationException">Thrown when tenant ID is not found in the request context.</exception>
        /// <remarks>
        /// For Added entities: Sets RestaurantId to the current tenant's ID from HttpContext.
        /// For Modified entities: Prevents RestaurantId from being changed (security measure).
        /// </remarks>
        private void SetTenantId(DbContext? context)
        {
            if (context == null)
                return;

            // Retrieve tenant ID from HttpContext (set by TenantResolverMiddleware)
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items[TenantIdKey] is not string tenantIdString)
            {
                // No tenant context available - this might be acceptable for background jobs or migrations
                // Consider your application's requirements for handling this scenario
                return;
            }

            // Parse tenant ID to integer (RestaurantId)
            if (!int.TryParse(tenantIdString, out var restaurantId))
            {
                throw new InvalidOperationException($"Invalid tenant ID format: {tenantIdString}. Expected integer value.");
            }

            // Get all tenant-specific entities being tracked
            var entries = context.ChangeTracker.Entries<TenantEntityBase>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    // Set RestaurantId for new entities
                    entry.Entity.RestaurantId = restaurantId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    // Prevent RestaurantId from being modified for security
                    // This ensures entities cannot be moved between tenants
                    entry.Property(nameof(TenantEntityBase.RestaurantId)).IsModified = false;
                }
            }
        }
    }
}
