namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// change email view model
    /// </summary>
    public class ChangeEmailViewModel
    {
        /// <summary>
        /// new email
        /// </summary>
        public string NewEmail { get; set; }
        /// <summary>
        /// user password
        /// </summary>
        /// <example>Test!123</example>
        public string Password { get; set; }

        /// <summary>
        ///CallbackUrl
        /// </summary>
        public string CallbackUrl { get; set; }
    }
}
