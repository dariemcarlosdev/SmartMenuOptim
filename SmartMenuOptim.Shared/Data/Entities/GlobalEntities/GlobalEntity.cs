namespace SmartMenuOptim.Shared.Data.Entities.GlobalEntities
{
    /// <summary>
    /// Represents a base class for entities that have a global identity and activation status.
    /// A Global Entity is cosidered suitable for data that is not tenant-specific and can be shared across multiple tenants. Examples include AdminUser and Customer.
    /// </summary>
    /// <remarks>This abstract class provides common properties for derived entities, including a unique
    /// identifier and an activation flag. It is intended to be inherited by domain models that require these
    /// features.</remarks>
    public abstract class GlobalEntity
    {

        /// <summary>
        /// Primary key for the entity.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Indicates if the entity is active
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}