namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Base Class for all entities to include common properties. All classes should inherit from this.
    /// </summary>
    internal class BaseEntity
    {
        // IsDeleted property to indicate soft deletion status
        public bool IsDeleted { get; set; }
    }
}