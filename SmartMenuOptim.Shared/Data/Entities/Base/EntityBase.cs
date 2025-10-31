using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Provides a base class for entities with common properties such as primary key, creation and update timestamps, soft
/// deletion, activation status, and optimistic concurrency control.
/// </summary>
/// <remarks>This abstract class is intended to be inherited by entity types that require standardized metadata
/// and concurrency management. It includes properties for tracking creation and modification times in UTC, soft
/// deletion, and activation state. The concurrency token property is mapped to PostgreSQL's 'xmin' system column for
/// use with optimistic concurrency control in EF Core. For more information on PostgreSQL system columns, see
/// https://www.postgresql.org/docs/current/ddl-system-columns.html.</remarks>
public abstract class EntityBase
{

    // === Standalone Properties ===

    /// <summary>
    /// Primary key for the entity.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Date and time when the entity was created (UTC).
    /// </summary>
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Date and time when the entity was last updated (UTC).
    /// </summary>
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Indicates if the entity is soft-deleted.
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;
    /// <summary>
    /// Gets or sets a value indicating whether the current instance is active.
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// Concurrency token for optimistic concurrency control, mapped to the PostgreSQL 'xmin' system column.
    /// </summary>
    /// <remarks>
    /// This property maps to PostgreSQL's built-in 'xmin' system column, which is automatically managed by PostgreSQL.
    /// - It does not create a new column in the database
    /// - The 'xmin' system column exists by default for all PostgreSQL tables
    /// - It's used for transaction control and versioning
    /// - The [Timestamp] attribute tells EF Core to use this property for concurrency control
    /// 
    /// PostgreSQL Transaction System Values:
    /// - xmin: Creating Transaction ID (e.g., 5970). Indicates which transaction created/last modified the row.
    /// - xmax: Deleting Transaction ID (e.g., 0). A value of 0 means the row is current/valid.
    ///         When a row is updated/deleted, xmax gets set to the transaction ID that made the change.
    /// 
    /// To verify it in PostgreSQL:
    /// SELECT *, xmin, xmax FROM "YourTableName";
    /// 
    /// Reference: https://www.postgresql.org/docs/current/ddl-system-columns.html
    /// </remarks>
    [Timestamp]
    public uint xmin { get; set; }
}