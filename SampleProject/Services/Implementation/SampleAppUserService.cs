using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using RSecurityBackend.Services.Implementation;
//using SampleProject.DbContext;

namespace SampleProject.Services.Implementation
{
    public class SampleAppUserService : AppUserService
    {
        public SampleAppUserService(RSecurityDbContext<RAppUser, RAppRole, Guid> context, UserManager<RAppUser> userManager, SignInManager<RAppUser> signInManager, RoleManager<RAppRole> roleManager, ISecretGenerator secretGenerator, IImageFileService imageFileService, IUserRoleService userRoleService, IConfiguration configuration, IEmailSender emailSender) : base(context, userManager, signInManager, roleManager, secretGenerator, imageFileService, userRoleService, configuration, emailSender)
        {
        }

        /// <summary>
        /// remove user data
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public override async Task<RServiceResult<bool>> RemoveUserData(Guid userId)
        {
            //var context = _context as RDbContext; //delete sample application specific user data
            return await base.RemoveUserData(userId);
        }
    }
}
