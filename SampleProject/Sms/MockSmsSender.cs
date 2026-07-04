using Microsoft.Extensions.Logging;
using RSecurityBackend.Services;

namespace SampleProject.Sms
{
    /// <summary>
    /// no-op sms sender: just logs the message. Safe default for local development so the app
    /// runs out of the box without any gateway credentials configured.
    /// </summary>
    public class MockSmsSender : ISmsSender
    {
        private readonly ILogger<MockSmsSender> _logger;

        /// <summary>
        /// constructor
        /// </summary>
        public MockSmsSender(ILogger<MockSmsSender> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// "sends" the sms by writing it to the log
        /// </summary>
        public Task SendSmsAsync(string phoneNumber, string message)
        {
            _logger.LogInformation("[MockSmsSender] to {PhoneNumber}: {Message}", phoneNumber, message);
            return Task.CompletedTask;
        }
    }
}
