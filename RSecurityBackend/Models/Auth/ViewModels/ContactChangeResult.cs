namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// result of a successfully confirmed email/phone number change (or first-time link)
    /// </summary>
    public class ContactChangeResult
    {
        /// <summary>
        /// true if the channel that was changed/linked is email, false if it is a phone number
        /// </summary>
        public bool IsEmail { get; set; }

        /// <summary>
        /// previous value for this channel, or null if this was a first-time link
        /// (e.g. verifying a phone number for a user who signed up by email, or vice versa)
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// new, now-confirmed value
        /// </summary>
        public string NewValue { get; set; }
    }
}
