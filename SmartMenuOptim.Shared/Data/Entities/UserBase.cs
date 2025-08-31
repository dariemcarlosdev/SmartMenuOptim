using System;
using System.Collections.Generic;

namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Abstract base class for shared user properties.
    /// </summary>
    public abstract class UserBase
    {
        // === Standalone Properties ===

        /// <summary>
        /// Primary key for the user entity.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Username for authentication.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password for authentication.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the user account is active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
