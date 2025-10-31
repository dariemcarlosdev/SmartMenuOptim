using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartMenuOptim.Shared.Data.Entities.GlobalEntities
{
    /// <summary>
    /// Abstract base class for authenticatable users in the system. Inherited by specific user types like Admin, Customer, etc.
    /// </summary>
    public abstract class UserBase : EntityBase
    {
        /// <summary>
        /// Username for authentication. Must be unique and between 3-50 characters.
        /// </summary>
        [Required(ErrorMessage = "Username is required")]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters long")]
        [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password for authentication. Required for security.
        /// </summary>
        [Required(ErrorMessage = "Password hash is required")]
        [MaxLength(128, ErrorMessage = "Password hash cannot exceed 128 characters")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Email address of the user. Must be unique and valid email format.
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Computed property that returns the normalized email for consistent lookups.
        /// </summary>
        [NotMapped]
        public string NormalizedEmail => Email.ToUpperInvariant();

        /// <summary>
        /// Computed property that returns the normalized username for consistent lookups.
        /// </summary>
        [NotMapped]
        public string NormalizedUsername => Username.ToUpperInvariant();
    }
}
