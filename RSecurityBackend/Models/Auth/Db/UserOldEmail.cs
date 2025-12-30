using System;

namespace RSecurityBackend.Models.Auth.Db
{
    /// <summary>
    /// user old email
    /// </summary>
    public class UserOldEmail
    {
        /// <summary>
        /// id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// user id
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// user
        /// </summary>
        public RAppUser User { get; set; }

        /// <summary>
        /// change date
        /// </summary>
        public DateTime ChangeDate { get; set; }

        /// <summary>
        /// email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// normalized email
        /// </summary>
        public string NormalizedEmail { get; set; }
    }
}
