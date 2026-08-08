namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// request body for external (Google, ...) login. The client is responsible for
    /// performing the provider's own sign-in (its SDK) and obtaining <see cref="IdToken"/> -
    /// this API only verifies and consumes it, it never redirects to the provider itself.
    /// </summary>
    public class ExternalLoginViewModel
    {
        /// <summary>
        /// raw ID token obtained from the identity provider's own client-side SDK
        /// </summary>
        public string IdToken { get; set; }

        /// <summary>
        /// client app name (same convention as LoginViewModel.ClientAppName)
        /// </summary>
        public string ClientAppName { get; set; }

        /// <summary>
        /// client language (same convention as LoginViewModel.Language)
        /// </summary>
        /// <example>fa-IR</example>
        public string Language { get; set; }
    }
}
