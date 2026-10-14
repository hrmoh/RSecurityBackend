using System;

namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// lock a user out until a given time
    /// </summary>
    public class LockoutUserViewModel
    {
        /// <summary>
        /// User Id
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// message shown to the user when he/she tries to login while locked out (required)
        /// </summary>
        public string Cause { get; set; }

        /// <summary>
        /// when the lockout ends (must be in the future)
        /// </summary>
        public DateTimeOffset LockoutEnd { get; set; }
    }
}
