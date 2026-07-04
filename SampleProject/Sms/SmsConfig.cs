using Microsoft.Extensions.Configuration;

namespace SampleProject.Sms
{
    /// <summary>
    /// Kavenegar provider settings
    /// </summary>
    public class KavenegarConfig
    {
        /// <summary>
        /// Api Key
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// sender line number (optional, provider default used if empty)
        /// </summary>
        public string Sender { get; set; } = "";
    }

    /// <summary>
    /// Ghasedak provider settings
    /// </summary>
    public class GhasedakConfig
    {
        /// <summary>
        /// Api Key
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// line number
        /// </summary>
        public string LineNumber { get; set; } = "";
    }

    /// <summary>
    /// ippanel provider settings
    /// </summary>
    public class IppanelConfig
    {
        /// <summary>
        /// Api Key
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// originator / sender number
        /// </summary>
        public string Originator { get; set; } = "";
    }

    /// <summary>
    /// top level sms configuration, read from the "SmsConfig" section of appsettings.json.
    /// This lives in the sample/client app (not the RSecurityBackend nuget package) so the
    /// core library never depends on any specific commercial sms vendor.
    /// </summary>
    /// <remarks>
    /// appsettings.json sample:
    /// <code>
    /// "SmsConfig": {
    ///   "Provider": "Kavenegar",
    ///   "FallbackProviders": "Ghasedak,Ippanel",
    ///   "Kavenegar": { "ApiKey": "", "Sender": "" },
    ///   "Ghasedak": { "ApiKey": "", "LineNumber": "" },
    ///   "Ippanel": { "ApiKey": "", "Originator": "" }
    /// }
    /// </code>
    /// </remarks>
    public class SmsConfig
    {
        /// <summary>
        /// primary provider
        /// </summary>
        public SmsProviderName Provider { get; set; } = SmsProviderName.Mock;

        /// <summary>
        /// ordered providers to try if the primary provider throws
        /// </summary>
        public SmsProviderName[] FallbackProviders { get; set; } = System.Array.Empty<SmsProviderName>();

        /// <summary>
        /// Kavenegar settings
        /// </summary>
        public KavenegarConfig Kavenegar { get; set; } = new KavenegarConfig();

        /// <summary>
        /// Ghasedak settings
        /// </summary>
        public GhasedakConfig Ghasedak { get; set; } = new GhasedakConfig();

        /// <summary>
        /// ippanel settings
        /// </summary>
        public IppanelConfig Ippanel { get; set; } = new IppanelConfig();

        /// <summary>
        /// build from IConfiguration ("SmsConfig" section); re-read every call so the provider can
        /// be swapped via configuration reload without an app restart
        /// </summary>
        public static SmsConfig Load(IConfiguration configuration)
        {
            IConfigurationSection section = configuration.GetSection("SmsConfig");

            SmsProviderName provider = SmsProviderName.Mock;
            string? providerRaw = section["Provider"];
            if (!string.IsNullOrWhiteSpace(providerRaw))
            {
                System.Enum.TryParse(providerRaw, true, out provider);
            }

            SmsProviderName[] fallbackProviders =
                (section["FallbackProviders"] ?? "")
                .Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                .Select(s => System.Enum.TryParse(s, true, out SmsProviderName p) ? p : (SmsProviderName?)null)
                .Where(p => p.HasValue && p.Value != provider)
                .Select(p => p!.Value)
                .Distinct()
                .ToArray();

            return new SmsConfig()
            {
                Provider = provider,
                FallbackProviders = fallbackProviders,
                Kavenegar = new KavenegarConfig()
                {
                    ApiKey = section.GetSection("Kavenegar")["ApiKey"] ?? "",
                    Sender = section.GetSection("Kavenegar")["Sender"] ?? "",
                },
                Ghasedak = new GhasedakConfig()
                {
                    ApiKey = section.GetSection("Ghasedak")["ApiKey"] ?? "",
                    LineNumber = section.GetSection("Ghasedak")["LineNumber"] ?? "",
                },
                Ippanel = new IppanelConfig()
                {
                    ApiKey = section.GetSection("Ippanel")["ApiKey"] ?? "",
                    Originator = section.GetSection("Ippanel")["Originator"] ?? "",
                },
            };
        }
    }
}
