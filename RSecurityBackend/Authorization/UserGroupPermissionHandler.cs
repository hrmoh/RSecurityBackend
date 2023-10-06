using Microsoft.AspNetCore.Authorization;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using RSecurityBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace RSecurityBackend.Authorization
{
    /// <summary>
    /// UserGroupPermissionHandler
    /// </summary>
    public class UserGroupPermissionHandler : AuthorizationHandler<UserGroupPermissionRequirement>
    {
        /// <summary>
        /// HandleRequirementAsync
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requirement"></param>
        /// <returns></returns>
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UserGroupPermissionRequirement requirement)
        {
          
            if (!context.User.HasClaim(c => c.Type == "UserId") || !context.User.HasClaim(c => c.Type == "SessionId"))
            {
                context.Fail();
                return;
            }

            Guid? workspaceId = null;

            if (context.Resource is HttpContext httpContext)
            {
                if (httpContext.GetRouteValue("workspace") != null)
                {
                    workspaceId = Guid.Parse(httpContext.GetRouteValue("workspace").ToString());
                }
            }

            //this is the default policy to make sure the use session has not yet been deleted by him/her from another client
            //or by an admin (Authorize with no policy should fail on deleted sessions)
            if (requirement.SecurableItemShortName == "null"/* && requirement.OperationShortName == "null"*/)
            {
                RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(new Guid(context.User.Claims.FirstOrDefault(c => c.Type == "UserId").Value), new Guid(context.User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value));
                if (!sessionCheckResult.Result)
                {
                    context.Fail();
                    return;
                }

                if(workspaceId != null)
                {
                    var res = await _workspaceService.IsUserWorkspaceMember((Guid)workspaceId, new Guid(context.User.Claims.FirstOrDefault(c => c.Type == "UserId").Value), context.User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
                    if(!res.Result)
                    {
                        context.Fail();
                        return;
                    }
                }

                context.Succeed(requirement);
                return;
            }


            RServiceResult<bool> result = await _userPermissionChecker.Check
                (
                new Guid(context.User.Claims.FirstOrDefault(c => c.Type == "UserId").Value),
                new Guid(context.User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value),
                context.User.Claims.FirstOrDefault(c => c.Type == "Language").Value,
                requirement.SecurableItemShortName,
                requirement.OperationShortName,
                workspaceId
                );

            if(result.Result)
            {
                context.Succeed(requirement);
                return;
            }

            context.Fail();
           
        }


        /// <summary>
        /// IUserPermissionChecker instance
        /// </summary>
        private IUserPermissionChecker _userPermissionChecker;

        /// <summary>
        /// IAppUserService instance
        /// </summary>
        protected IAppUserService _appUserService;

        /// <summary>
        /// workspace service
        /// </summary>
        protected readonly IWorkspaceService _workspaceService;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="userPermissionChecker"></param>
        /// <param name="appUserService"></param>
        /// <param name="workspaceService"></param>
        public UserGroupPermissionHandler(IUserPermissionChecker userPermissionChecker, IAppUserService appUserService, IWorkspaceService workspaceService) : base()
        {
            _userPermissionChecker = userPermissionChecker;
            _appUserService = appUserService;
            _workspaceService = workspaceService;
        }
    }
}
