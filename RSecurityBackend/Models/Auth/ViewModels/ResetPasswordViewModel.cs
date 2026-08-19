namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// reset password view model
    /// </summary>
    public class ResetPasswordViewModel
    {
        /// <summary>
        /// Email address or phone number (same value submitted to forgotpassword)
        /// </summary>
        /// <example>email@domain.com</example>
        public string Email { get; set; }

        /// <summary>
        /// Secret
        /// </summary>
        /// <example>4ozHJQN0X6CebX0He0/xaznhIjvubfySFnwdoCYLLo8=</example>
        public string Secret { get; set; }

        /// <summary>
        /// password
        /// </summary>
        /// <example>Test!123</example>
        public string Password { get; set; }
  
    }
}
