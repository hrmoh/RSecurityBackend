using System;

namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// a safe subset of RUserSession
    /// </summary>
    public class PublicRUserSession
    {

        /// <summary>
        /// Session Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public PublicRAppUser RAppUser { get; set; }

        /// <summary>
        /// Client IP Address
        /// </summary>
        public string ClientIPAddress { get; set; }

        /// <summary>
        /// Client Application Name
        /// </summary>
        /// <example>App Angular Client</example>
        public string ClientAppName { get; set; }

        /// <summary>
        /// Client Language
        /// </summary>
        /// <example>fa-IR</example>
        public string Language { get; set; }

        /// <summary>
        /// Login Date
        /// </summary>
        public DateTime LoginTime { get; set; }

        /// <summary>
        /// Last Renewal
        /// </summary>
        public DateTime LastRenewal { get; set; }

        /// <summary>
        /// When this session's sliding idle-timeout window (see
        /// AppUserService.SessionIdleTimeoutInDays) expires if it is not renewed again - a session
        /// past this point can no longer be used to relogin, and is rejected by SessionExists. Exposed
        /// here so callers listing sessions (e.g. an admin screen) can tell which ones are about to go
        /// stale, rather than just when they last logged in.
        /// </summary>
        public DateTime ValidUntil { get; set; }
    }
}
