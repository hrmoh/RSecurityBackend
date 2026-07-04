using RSecurityBackend.Services;
using System.Net.Http;
using System.Net.Http.Json;

namespace SampleProject.Sms
{
    /// <summary>
    /// ippanel (https://ippanel.com) sms sender.
    /// Reference implementation of the v2 REST "send/webservice/single" endpoint - check
    /// ippanel's current docs (https://apidoc.ippanel.com) before relying on this in production,
    /// gateway APIs change over time.
    /// </summary>
    public class IppanelSmsSender : ISmsSender
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly IppanelConfig _config;

        /// <summary>
        /// constructor
        /// </summary>
        public IppanelSmsSender(IppanelConfig config)
        {
            _config = config;
        }

        /// <inheritdoc/>
        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
                throw new InvalidOperationException("ippanel ApiKey is not configured (SmsConfig:Ippanel:ApiKey).");

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api2.ippanel.com/api/v1/sms/send/webservice/single");
            request.Headers.TryAddWithoutValidation("apikey", _config.ApiKey);
            request.Content = JsonContent.Create(new
            {
                recipient = phoneNumber,
                sender = _config.Originator,
                message = message,
            });

            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"ippanel sms send failed ({(int)response.StatusCode}): {body}");
            }
        }
    }
}
