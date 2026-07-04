using RSecurityBackend.Services;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SampleProject.Sms
{
    /// <summary>
    /// Ghasedak (https://ghasedak.me) sms sender.
    /// Reference implementation of the "send/simple" REST endpoint - check Ghasedak's current
    /// docs (https://ghasedak.me/docs) before relying on this in production, gateway APIs change
    /// over time and Ghasedak also offers a dedicated OTP-template endpoint you may prefer.
    /// </summary>
    public class GhasedakSmsSender : ISmsSender
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly GhasedakConfig _config;

        /// <summary>
        /// constructor
        /// </summary>
        public GhasedakSmsSender(GhasedakConfig config)
        {
            _config = config;
        }

        /// <inheritdoc/>
        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
                throw new InvalidOperationException("Ghasedak ApiKey is not configured (SmsConfig:Ghasedak:ApiKey).");

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.ghasedak.me/v2/sms/send/simple");
            request.Headers.TryAddWithoutValidation("apikey", _config.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            List<KeyValuePair<string, string>> formValues = new List<KeyValuePair<string, string>>
            {
                new("message", message),
                new("receptor", phoneNumber),
            };
            if (!string.IsNullOrWhiteSpace(_config.LineNumber))
            {
                formValues.Add(new("linenumber", _config.LineNumber));
            }
            request.Content = new FormUrlEncodedContent(formValues);

            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Ghasedak sms send failed ({(int)response.StatusCode}): {body}");
            }
        }
    }
}
