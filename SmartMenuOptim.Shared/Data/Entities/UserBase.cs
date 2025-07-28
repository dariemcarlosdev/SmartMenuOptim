using System;
using System.Collections.Generic;

namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Abstract base class for shared user properties.
    /// </summary>
    public abstract class UserBase
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        /// <summary>
        /// Indicates if the user account is active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
