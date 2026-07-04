using System.Threading.Tasks;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// Sms sender abstraction, mirroring <see cref="Microsoft.AspNetCore.Identity.UI.Services.IEmailSender"/>.
    /// RSecurityBackend ships no concrete implementation on purpose: pick/implement whichever
    /// gateway(s) you want (Kavenegar, Ghasedak, ippanel, Twilio, ...) in the consuming
    /// application and register it (or a composite/multi-provider wrapper around several of them)
    /// as <see cref="ISmsSender"/> in your DI container.
    /// </summary>
    public interface ISmsSender
    {
        /// <summary>
        /// send an sms message to a phone number
        /// </summary>
        /// <param name="phoneNumber">destination phone number (E.164 format recommended, e.g. +989121234567)</param>
        /// <param name="message">message text</param>
        /// <returns></returns>
        Task SendSmsAsync(string phoneNumber, string message);
    }
}
