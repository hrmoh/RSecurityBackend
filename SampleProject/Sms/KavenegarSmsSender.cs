using RSecurityBackend.Services;
using System.Net.Http;

namespace SampleProject.Sms
{
    /// <summary>
    /// Kavenegar (https://kavenegar.com) sms sender.
    /// Reference implementation of the REST "send.json" endpoint - check Kavenegar's current
    /// docs (https://kavenegar.com/rest.html) before relying on this in production, gateway
    /// APIs change over time.
    /// </summary>
    public class KavenegarSmsSender : ISmsSender
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly KavenegarConfig _config;

        /// <summary>
        /// constructor
        /// </summary>
        public KavenegarSmsSender(KavenegarConfig config)
        {
            _config = config;
        }

        /// <inheritdoc/>
        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
                throw new InvalidOperationException("Kavenegar ApiKey is not configured (SmsConfig:Kavenegar:ApiKey).");

            string url = $"https://api.kavenegar.com/v1/{Uri.EscapeDataString(_config.ApiKey)}/sms/send.json" +
                         $"?receptor={Uri.EscapeDataString(phoneNumber)}" +
                         $"&message={Uri.EscapeDataString(message)}" +
                         (string.IsNullOrWhiteSpace(_config.Sender) ? "" : $"&sender={Uri.EscapeDataString(_config.Sender)}");

            using HttpResponseMessage response = await _httpClient.GetAsync(url);
            string body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Kavenegar sms send failed ({(int)response.StatusCode}): {body}");
            }
        }
    }
}
