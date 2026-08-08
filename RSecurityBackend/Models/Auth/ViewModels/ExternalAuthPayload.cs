namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// verified claims extracted from an external identity provider's token, produced by an
    /// <see cref="Services.IExternalAuthValidator"/> implementation and consumed by
    /// <see cref="Services.IAppUserService.ExternalLogin"/>. By the time this reaches
    /// ExternalLogin it is trusted at face value - all validation must already have happened
    /// inside the IExternalAuthValidator that produced it.
    /// </summary>
    public class ExternalAuthPayload
    {
        /// <summary>
        /// provider name, e.g. "Google" - stored as AspNetUserLogins.LoginProvider
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// stable unique identifier for the user at the provider (Google's "sub" claim) -
        /// stored as AspNetUserLogins.ProviderKey
        /// </summary>
        public string ProviderKey { get; set; }

        /// <summary>
        /// email address reported by the provider, if any
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// true only if the provider itself attests this email is verified (e.g. Google's
        /// "email_verified" claim). Required before this email can be used to auto-link to an
        /// existing local account or to auto-create a new one - never infer this from anything
        /// other than the provider's own claim.
        /// </summary>
        public bool EmailVerified { get; set; }

        /// <summary>
        /// display name reported by the provider, if any
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// given/first name reported by the provider, if any
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// family/last name reported by the provider, if any
        /// </summary>
        public string SurName { get; set; }
    }
}
