namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Represents a business rule managed by an admin user (e.g., thresholds, analytics settings).
    /// Deprecated: Use properties on AdminUser instead.
    /// </summary>
    public class BusinessRule
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Value { get; set; }
        public int AdminUserId { get; set; }
        // public AdminUser? AdminUser { get; set; } // Deprecated
    }
}
