using System.Collections.Generic;

namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Represents an admin user for business/admin logic and sensitive features.
    /// Business rules are now defined as properties on this entity.
    /// </summary>
    public class AdminUser : UserBase
    {
        public string Role { get; set; } = "Admin"; // e.g., "Admin", "Manager"
        /// <summary>
        /// business rule: Minimum number of sales for a dish to be considered popular.If a dish has fewer sales than this threshold, it may be considered for removal or adjustment since is underperforming.
        /// </summary>
        public int SalesThreshold { get; set; } = 35;

        /// <summary>
        /// business rule: Minimum sentiment score for a review to be considered positive. If a review has a sentiment score below this threshold, it may be flagged for review or considered negative.
        /// </summary>
        public double SentimentThreshold { get; set; } = 0.6;

        /// <summary>
        /// business rule: Minimum number of reviews required for a dish to be considered well-reviewed. If a dish has fewer reviews than this threshold, it may be considered for removal or adjustment since is underperforming.
        /// </summary>        
        public int ReviewCountThreshold { get; set; } = 5;

        /// <summary>
        /// business rule: Minimum number of sales required for a dish to be considered well-sold. If a dish has fewer sales than this threshold, it may be considered for removal or adjustment since is underperforming.
        /// </summary>        
        public int WellSoldThreshold { get; set; } = 20;

        /// <summary>
        /// business rule: Minimum number of reviews left by a customer for them to be considered a regular customer. If a customer has fewer reviews than this threshold, they may be considered a new or occasional customer.
        /// <summary>        
        public int RegularCustomerReviewCountThreshold { get; set; } = 3;

        /// <summary>
        /// business rule: Minimum number of reviews left by a customer for them to be considered as a premium customer. If a customer has fewer reviews than this threshold, they may be considered a regular or new customer.
        /// <summary> 
        
        public int PremiumCustomerReviewCountThreshold { get; set; } = 10;

        // <summary>
        /// List of permissions or roles assigned to this admin user.This can be used to control access to different features or areas of the application.

        public List<string> Permissions { get; set; } = new List<string>();

        /// <summary>
    }
}
