using System.ComponentModel.DataAnnotations;

namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// verified sign up view model
    /// </summary>
    public class VerifiedSignUpViewModel
    {
        /// <summary>
        /// Email address or phone number (must match whatever value was originally passed to
        /// SignUp/UnverifiedSignUpViewModel.Email - "@" present means it's an email)
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

        /// <summary>
        /// First Name
        /// </summary>
        /// <example>Hamid Reza</example>
        public string FirstName { get; set; }

        /// <summary>
        /// SurName
        /// </summary>
        /// <example>Mohammadi</example>
        public string SurName { get; set; }

        /// <summary>
        /// User Mobile Phone Number (optional secondary phone number; only used when
        /// signing up with an email - ignored when Email itself holds a phone number)
        /// </summary>
        /// <example>+989121234567</example>
        [Phone]
        public string PhoneNumber { get; set; }
    }
}
