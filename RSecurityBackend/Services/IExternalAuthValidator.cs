using System.Threading.Tasks;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// validates a raw external identity provider credential (e.g. a Google ID token) and
    /// extracts the claims RSecurityBackend needs to find-or-create the local account.
    /// RSecurityBackend ships no concrete implementation on purpose (mirrors <see cref="ISmsSender"/>):
    /// implement/register whichever provider(s) you use (Google, Microsoft, Apple, ...) as
    /// <see cref="IExternalAuthValidator"/> in your DI container, one registration per provider,
    /// distinguished by <see cref="ProviderName"/>.
    /// </summary>
    public interface IExternalAuthValidator
    {
        /// <summary>
        /// provider name this validator handles, e.g. "Google". Must match the provider
        /// route segment used to reach it and is stored verbatim as
        /// AspNetUserLogins.LoginProvider once a user links this provider.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// validate the raw token/credential issued by the provider - signature, issuer,
        /// audience, expiry: whatever the provider's own SDK/library checks - and return the
        /// verified claims. Must fail (ExceptionString set, Result null) rather than return a
        /// payload for any token that does not fully validate; callers trust the returned
        /// payload at face value without re-checking it.
        /// </summary>
        /// <param name="rawToken">the raw ID token / credential supplied by the client</param>
        /// <returns></returns>
        Task<RServiceResult<ExternalAuthPayload>> ValidateAsync(string rawToken);
    }
}
