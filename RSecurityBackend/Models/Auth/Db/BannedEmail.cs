using System;

namespace RSecurityBackend.Models.Auth.Db
{
    /// <summary>
    /// banned emails/phone numbers (previous users who had been kicked out and their email
    /// and/or phone number are logged to not allow them to signup again)
    /// </summary>
    public class BannedEmail
    {
        /// <summary>
        /// id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// all capital email (null if this ban entry does not cover an email address)
        /// </summary>
        public string NormalizedEmail { get; set; }

        /// <summary>
        /// banned phone number (null if this ban entry does not cover a phone number)
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// cause
        /// </summary>
        public string Description { get; set; }
    }
}
