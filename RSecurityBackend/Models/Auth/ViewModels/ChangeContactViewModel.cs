namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// request to change (or first-time link) the logged on user's email or phone number
    /// </summary>
    public class ChangeContactViewModel
    {
        /// <summary>
        /// new email address or phone number. If it contains "@" it is treated as an
        /// email address (verification code sent by email), otherwise as a phone
        /// number (verification code sent by sms).
        /// </summary>
        public string NewContact { get; set; }
        /// <summary>
        /// user password (re-authentication required before a contact change)
        /// </summary>
        /// <example>Test!123</example>
        public string Password { get; set; }

        /// <summary>
        ///CallbackUrl (only used for the email case)
        /// </summary>
        public string CallbackUrl { get; set; }
    }
}
