using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartMenuOptim.Domain.Features.Restaurants;

/// <summary>
/// Base class for all tenant-specific entities in the system.
/// Provides common properties and behavior for multi-tenant data isolation.
/// </summary>
/// <remarks>
/// Multi-Tenant Architecture:
/// - Each entity inheriting from this base class is automatically scoped to a specific restaurant (tenant)
/// - The RestaurantId property ensures data isolation between different tenants
/// - The Restaurant navigation property enables easy access to the tenant context
/// 
/// PostgreSQL Transaction System Integration:
/// 1. Transaction Control:
///    - xmin: Creating Transaction ID that inserted or last modified the row
///      Example: xmin = 5970 means transaction #5970 created/modified this row
///    - xmax: Deleting Transaction ID
///      - 0 means row is current/valid and has not been deleted/updated
///      - if this row is deleted/updated, xmax will hold the transaction ID that performed that action.Think of it as a "deletion marker"
///      - Non-zero means row was deleted/updated by that transaction
/// 
/// 2. Concurrency Implementation:
///    - The [Timestamp] attribute on uint xmin property maps to PostgreSQL's system column
///    - No additional column is created; uses PostgreSQL's built-in MVCC
///    - EF Core uses xmin for optimistic concurrency checks
///    - When row is modified, xmin updates automatically
/// 
/// 3. Verification Query:
///    SELECT *, xmin, xmax FROM "YourTableName";
///    This shows current transaction states for rows
/// 
/// 4. Multi-Version Concurrency Control (MVCC):
///    - PostgreSQL maintains multiple versions of each row
///    - xmin/xmax determine visible versions for each transaction
///    - Enables concurrent access without explicit locking
///    - EF Core leverages this for optimistic concurrency
/// 
/// Reference: https://www.postgresql.org/docs/current/ddl-system-columns.html
/// </remarks>
public abstract class TenantEntityBase : EntityBase
{
     // === Required Tenant Relationship ===

    /// <summary>
    /// Foreign key to the Restaurant entity. Each entity belongs to a single restaurant.
    /// </summary>
    [Required(ErrorMessage = "RestaurantId is required for tenant-scoped entities")]
    [ForeignKey(nameof(Restaurant))]
    public int RestaurantId { get; set; }

    /// <summary>
    /// Navigation property to the Restaurant this entity is associated with.
    /// </summary>
    public Restaurant? Restaurant { get; set; }
}