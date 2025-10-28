using System;
using System.Collections.Generic;

namespace SmartMenuOptim.Shared.Data.Entities.GlobalEntities
{
    /// <summary>
    /// Abstract base class for authenticatable users in the system. Inherited by specific user types like Admin, Customer, etc.
    /// </summary>
    public abstract class UserBase : GlobalEntity
    {
       
        /// <summary>
        /// Username for authentication.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password for authentication.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;


    }
}
