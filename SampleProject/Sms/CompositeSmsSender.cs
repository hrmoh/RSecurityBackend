using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RSecurityBackend.Services;

namespace SampleProject.Sms
{
    /// <summary>
    /// Picks an sms provider based on the "SmsConfig" section of appsettings.json and sends
    /// through it; if it throws, tries "SmsConfig:FallbackProviders" in order.
    /// This is the class registered as <see cref="ISmsSender"/> in Program.cs - swap which
    /// provider is primary/fallback purely via configuration, no code change needed.
    /// </summary>
    public class CompositeSmsSender : ISmsSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CompositeSmsSender> _logger;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// constructor
        /// </summary>
        public CompositeSmsSender(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<CompositeSmsSender>();
        }

        private ISmsSender CreateSender(SmsProviderName provider, SmsConfig config)
        {
            return provider switch
            {
                SmsProviderName.Kavenegar => new KavenegarSmsSender(config.Kavenegar),
                SmsProviderName.Ghasedak => new GhasedakSmsSender(config.Ghasedak),
                SmsProviderName.Ippanel => new IppanelSmsSender(config.Ippanel),
                _ => new MockSmsSender(_loggerFactory.CreateLogger<MockSmsSender>()),
            };
        }

        /// <inheritdoc/>
        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            SmsConfig config = SmsConfig.Load(_configuration);

            List<SmsProviderName> providersToTry = new List<SmsProviderName> { config.Provider };
            providersToTry.AddRange(config.FallbackProviders);

            Exception? lastException = null;
            foreach (SmsProviderName provider in providersToTry)
            {
                try
                {
                    await CreateSender(provider, config).SendSmsAsync(phoneNumber, message);
                    return;
                }
                catch (Exception exp)
                {
                    lastException = exp;
                    _logger.LogWarning(exp, "Sms provider {Provider} failed sending to {PhoneNumber}, trying next fallback if any", provider, phoneNumber);
                }
            }

            throw new InvalidOperationException($"All configured sms providers failed for {phoneNumber}", lastException);
        }
    }
}
