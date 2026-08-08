using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Controllers;
using RSecurityBackend.Services;

namespace SampleProject.Controllers
{
    /// <summary>
    /// User login/logout/register/...
    /// </summary>
    [Produces("application/json")]
    [Route("api/users")]
    public class AppUserController : AppUserControllerBase
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="appUserService"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="userPermissionChecker"></param>
        /// <param name="emailSender"></param>
        /// <param name="imageFileService"></param>
        /// <param name="captchaService"></param>
        /// <param name="smsSender"></param>
        /// <param name="externalAuthValidators"></param>
        public AppUserController(IConfiguration configuration, IAppUserService appUserService, IHttpContextAccessor httpContextAccessor, IUserPermissionChecker userPermissionChecker, IEmailSender emailSender, IImageFileService imageFileService, ICaptchaService captchaService, ISmsSender smsSender, IEnumerable<IExternalAuthValidator> externalAuthValidators)
            : base(configuration, appUserService, httpContextAccessor, userPermissionChecker, emailSender, imageFileService, captchaService, smsSender, externalAuthValidators)
        {

        }
    }
}
