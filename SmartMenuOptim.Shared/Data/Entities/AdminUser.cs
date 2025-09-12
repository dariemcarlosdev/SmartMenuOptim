

namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Represents an admin user for business/admin logic and sensitive features.
    /// </summary>
    /// <remarks>
    /// Note: AdminUser is not tenant-specific. It acts as the owner or manager of one or more tenants (restaurants).
    /// Each AdminUser can own/manage multiple restaurants (tenants), and each Restaurant references a single AdminUser as its owner.
    /// AdminUser is a global entity and enables multi-tenancy by linking to tenant entities, but is not itself scoped to a single tenant.
    /// </remarks>
    public class AdminUser : UserBase
    {
        // === Standalone Properties ===

        /// <summary>
        /// Role of the admin user (e.g., "Admin", "Manager").
        /// </summary>
        public string Role { get; set; } = "Admin";

        /// <summary>
        /// Minimum number of sales for a dish to be considered popular.
        /// </summary>
        public int SalesThreshold { get; set; } = 30;

        /// <summary>
        /// Minimum sentiment score for a review to be considered positive.
        /// </summary>
        public double SentimentThreshold { get; set; } = 0.6;

        /// <summary>
        /// Minimum number of reviews required for a dish to be considered well-reviewed.
        /// </summary>        
        public int ReviewCountThreshold { get; set; } = 5;

        /// <summary>
        /// Minimum number of sales required for a dish to be considered well-sold.
        /// </summary>        
        public int WellSoldThreshold { get; set; } = 20;

        /// <summary>
        /// Minimum number of reviews left by a customer for them to be considered a regular customer.
        /// </summary>        
        public int RegularCustomerReviewCountThreshold { get; set; } = 3;

        /// <summary>
        /// Minimum number of reviews left by a customer for them to be considered as a premium customer.
        /// </summary> 
        public int PremiumCustomerReviewCountThreshold { get; set; } = 10;

        /// <summary>
        /// List of permissions or roles assigned to this admin user. Used to control access to different features or areas of the application.
        /// </summary>
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
