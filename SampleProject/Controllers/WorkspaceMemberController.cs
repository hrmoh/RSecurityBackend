using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Controllers;
using RSecurityBackend.Services;

namespace SampleProject.Controllers
{
    /// <summary>
    /// Workspace members
    /// </summary>
    [Produces("application/json")]
    [Route("api/workspace/members")]
    public class WorkspaceMemberController : WorkspaceMemberControllerBase
    {
        public WorkspaceMemberController(IWorkspaceService workspaceService, IUserPermissionChecker userPermissionChecker, IAppUserService appUserService) : base(workspaceService, userPermissionChecker, appUserService)
        {
        }
    }
}
