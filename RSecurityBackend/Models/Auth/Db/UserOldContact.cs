using System;

namespace RSecurityBackend.Models.Auth.Db
{
    /// <summary>
    /// history record for a user's previous email address or phone number,
    /// written whenever an existing (non-null) contact value is replaced via
    /// <see cref="Services.IAppUserService.ChangeContact(Guid, string, string)"/>
    /// </summary>
    public class UserOldContact
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
        /// true if <see cref="Value"/> is an old email address, false if it is an old phone number
        /// </summary>
        public bool IsEmail { get; set; }

        /// <summary>
        /// the old email address or phone number
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// normalized email (only meaningful when <see cref="IsEmail"/> is true, otherwise null)
        /// </summary>
        public string NormalizedValue { get; set; }
    }
}
