using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;

namespace SampleProject.ExternalAuth
{
    /// <summary>
    /// validates a Google ID token (as obtained by the client from Google Sign-In / One Tap /
    /// GoogleSignIn SDKs) and extracts the claims RSecurityBackend needs. Registered as
    /// <see cref="IExternalAuthValidator"/> in Program.cs; see <see cref="GoogleAuthConfig"/>
    /// for the appsettings.json shape.
    /// </summary>
    public class GoogleIdTokenValidator : IExternalAuthValidator
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleIdTokenValidator> _logger;

        /// <summary>
        /// constructor
        /// </summary>
        public GoogleIdTokenValidator(IConfiguration configuration, ILogger<GoogleIdTokenValidator> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc/>
        public string ProviderName => "Google";

        /// <inheritdoc/>
        public async Task<RServiceResult<ExternalAuthPayload>> ValidateAsync(string rawToken)
        {
            try
            {
                GoogleAuthConfig config = GoogleAuthConfig.Load(_configuration);
                if (config.ClientIds.Length == 0)
                {
                    return new RServiceResult<ExternalAuthPayload>(null, "GoogleAuthConfig:ClientIds is not configured.");
                }

                //ValidateAsync verifies the token's signature against Google's published keys
                //(cached/refreshed internally by the library), and checks issuer, audience
                //(against config.ClientIds) and expiry - throws if any of that fails
                GoogleJsonWebSignature.ValidationSettings settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = config.ClientIds
                };

                GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(rawToken, settings);

                return new RServiceResult<ExternalAuthPayload>(
                    new ExternalAuthPayload()
                    {
                        Provider = ProviderName,
                        ProviderKey = payload.Subject,
                        Email = payload.Email,
                        EmailVerified = payload.EmailVerified,
                        DisplayName = payload.Name,
                        FirstName = payload.GivenName,
                        SurName = payload.FamilyName,
                    });
            }
            catch (InvalidJwtException exp)
            {
                //expected for a bad/expired/wrong-audience token - not a server error, just an
                //invalid credential from the client's point of view
                _logger.LogWarning(exp, "Rejected an invalid Google ID token");
                return new RServiceResult<ExternalAuthPayload>(null, "Invalid Google ID token.");
            }
            catch (Exception exp)
            {
                return new RServiceResult<ExternalAuthPayload>(null, exp.ToString());
            }
        }
    }
}
