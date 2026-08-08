using Microsoft.Extensions.Configuration;

namespace SampleProject.ExternalAuth
{
    /// <summary>
    /// Google external-login settings, read from the "GoogleAuthConfig" section of
    /// appsettings.json. This lives in the sample/client app (not the RSecurityBackend
    /// nuget package) so the core library never depends on Google's SDK.
    /// </summary>
    /// <remarks>
    /// appsettings.json sample:
    /// <code>
    /// "GoogleAuthConfig": {
    ///   "ClientIds": "1234567890-abc.apps.googleusercontent.com"
    /// }
    /// </code>
    /// <see cref="ClientIds"/> is comma-separated because you'll often have more than one
    /// OAuth client id to accept (e.g. one per platform: web, Android, iOS) - Google issues a
    /// separate client id per platform, and an ID token's "aud" claim must match one of them.
    /// </remarks>
    public class GoogleAuthConfig
    {
        /// <summary>
        /// one or more (comma-separated) Google OAuth client ids this backend accepts as a
        /// valid audience. Get these from the Google Cloud Console credentials page - one per
        /// platform (web/Android/iOS) that signs users in.
        /// </summary>
        public string[] ClientIds { get; set; } = System.Array.Empty<string>();

        /// <summary>
        /// build from IConfiguration ("GoogleAuthConfig" section)
        /// </summary>
        public static GoogleAuthConfig Load(IConfiguration configuration)
        {
            IConfigurationSection section = configuration.GetSection("GoogleAuthConfig");

            string[] clientIds =
                (section["ClientIds"] ?? "")
                .Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);

            return new GoogleAuthConfig()
            {
                ClientIds = clientIds
            };
        }
    }
}
