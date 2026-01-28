using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SmartMenuOptim.Infrastructure.Persistence.Interceptors
{
    /// <summary>
    /// Interceptor used to automatically populate audit fields (CreatedAt, UpdatedAt) 
    /// for entities during database save operations.
    /// </summary>
    /// <remarks>
    /// This interceptor implements the Entity Framework Core SaveChangesInterceptor pattern to:
    /// <list type="bullet">
    ///     <item><description>Automatically set CreatedAt when entities are added</description></item>
    ///     <item><description>Automatically update UpdatedAt when entities are modified</description></item>
    ///     <item><description>Track when actions occurred with UTC timestamps</description></item>
    /// </list>
    /// This ensures consistent audit trail across all entities inheriting from EntityBase without manual intervention.
    /// 
    /// <para><strong>Implementation Details:</strong></para>
    /// <list type="bullet">
    ///     <item><description>Works with all entities inheriting from EntityBase (which includes TenantEntityBase)</description></item>
    ///     <item><description>Uses UTC timestamps for consistency across time zones</description></item>
    ///     <item><description>Intercepts both synchronous and asynchronous SaveChanges operations</description></item>
    ///     <item><description>Automatically processes Added and Modified entity states</description></item>
    ///     <item><description>Prevents CreatedAt from being modified on updates</description></item>
    /// </list>
    /// 
    /// <para><strong>Future Enhancement:</strong></para>
    /// <para>This interceptor can be extended to support user tracking by adding CreatedBy and ModifiedBy fields 
    /// to EntityBase and capturing the current user identity (e.g., via IHttpContextAccessor or ICurrentUserService).
    /// This would provide a complete audit trail showing both when and who made changes to entities.</para>
    /// 
    /// <para><strong>How to Register:</strong></para>
    /// <para>Add this interceptor to your DbContext configuration in the service registration:</para>
    /// <code>
    /// services.AddDbContext&lt;AppDbContext&gt;(options =&gt;
    /// {
    ///     options.UseNpgsql(connectionString);
    ///     options.AddInterceptors(new AuditInterceptor());
    /// });
    /// </code>
    /// 
    /// <para><strong>Alternative: Dependency Injection</strong></para>
    /// <para>For better testability, register the interceptor in DI:</para>
    /// <code>
    /// services.AddSingleton&lt;AuditInterceptor&gt;();
    /// services.AddDbContext&lt;AppDbContext&gt;((serviceProvider, options) =&gt;
    /// {
    ///     options.UseNpgsql(connectionString);
    ///     options.AddInterceptors(serviceProvider.GetRequiredService&lt;AuditInterceptor&gt;());
    /// });
    /// </code>
    /// </remarks>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        /// <summary>
        /// Intercepts synchronous SaveChanges operation to set audit timestamps.
        /// </summary>
        /// <param name="eventData">Contextual information about the save operation.</param>
        /// <param name="result">The current result of the save operation.</param>
        /// <returns>The modified result after setting audit fields.</returns>
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            SetAuditFields(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        /// <summary>
        /// Intercepts asynchronous SaveChangesAsync operation to set audit timestamps.
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
            SetAuditFields(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// Sets audit timestamp fields (CreatedAt, UpdatedAt) for entities that inherit from EntityBase.
        /// </summary>
        /// <param name="context">The database context containing the tracked entities.</param>
        /// <remarks>
        /// For Added entities: Sets CreatedAt and UpdatedAt to current UTC time.
        /// For Modified entities: Updates UpdatedAt to current UTC time.
        /// </remarks>
        private void SetAuditFields(DbContext? context)
        {
            if (context == null)
                return;

            var now = DateTime.UtcNow;

            // Get all entities that inherit from EntityBase
            var entries = context.ChangeTracker.Entries<EntityBase>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    // Set both CreatedAt and UpdatedAt for new entities
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    // Only update UpdatedAt for modified entities
                    // Prevent CreatedAt from being modified
                    entry.Property(nameof(EntityBase.CreatedAt)).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                }
            }
        }
    }
}
