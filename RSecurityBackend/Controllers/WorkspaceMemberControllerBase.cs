using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RSecurityBackend.Services;
using RSecurityBackend.Models.Cloud;
using System;
using System.Linq;
using RSecurityBackend.Models.Cloud.ViewModels;

namespace RSecurityBackend.Controllers
{
    /// <summary>
    /// Workspace Members
    /// </summary>
    [Produces("application/json")]
    [Route("api/workspace/members")]
    public abstract class WorkspaceMemberControllerBase : Controller
    {

        /// <summary>
        /// delete member
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpDelete("{userId}/from/{workspace}")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.RemoveMembersOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> DeleteMemberAsync(Guid workspace, Guid userId)
        {
            RServiceResult<bool> result = await _workspaceService.DeleteMemberAsync(workspace, userId, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if (!result.Result)
                return NotFound();
            return Ok(result.Result);
        }


        /// <summary>
        /// get workspace members (if RestrictWorkspaceMembersQueryToAuthorizarion returns true looks at workspace:vumembers)
        /// </summary>
        /// <param name="workspace"></param>
        /// <returns></returns>
        [HttpGet("{workspace}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RWSUserViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> GetWorkspaceMembersAsync(Guid workspace)
        {
            if (_workspaceService.RestrictWorkspaceMembersQueryToAuthorizarion)
            {
                Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
                Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
                RServiceResult<bool>
                    canAdd =
                        await _userPermissionChecker.Check
                            (
                                loggedOnUserId,
                                sessionId,
                                User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR",
                                SecurableItem.WorkpsaceEntityShortName,
                                SecurableItem.QueryMembersListOperationShortName
                                );
                if (!string.IsNullOrEmpty(canAdd.ExceptionString))
                {
                    return BadRequest(canAdd.ExceptionString);
                }
                if (canAdd.Result == false)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
            }
            RServiceResult<RWSUserViewModel[]> result = await _workspaceService.GetWorkspaceMembersAsync(workspace, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            return Ok(result.Result);
        }


        /// <summary>
        /// change member status
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="userId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpPut("{userId}/in/{workspace}/status/{status}")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.ChangeMemberStatusOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> ChangeMemberStatusAsync(Guid workspace, Guid userId, RWSUserMembershipStatus status)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<bool> result = await _workspaceService.ChangeMemberStatusAsync(workspace, loggedOnUserId, userId, status, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if (!result.Result)
                return NotFound();
            return Ok(result.Result);
        }

        /// <summary>
        /// add user to role in a workspace
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="userId"></param>
        /// <param name="role"></param>
        /// <returns></returns>

        [HttpPost("{userId}/in/{workspace}/role/{role}")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.ChangeMemberRoleShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> AddUserToRoleInWorkspaceAsync(Guid workspace, Guid userId, string role)
        {
            RServiceResult<bool> result = await _workspaceService.AddUserToRoleInWorkspaceAsync(workspace, userId, role, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if (!result.Result)
                return NotFound();
            return Ok(result.Result);
        }

        /// <summary>
        /// remove user from role in workspace
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="userId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        [HttpDelete("{userId}/from/{role}/in/{workspace}")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.ChangeMemberRoleShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> RemoveUserFromRoleInWorkspaceAsync(Guid workspace, Guid userId, string role)
        {
            RServiceResult<bool> result = await _workspaceService.RemoveUserFromRoleInWorkspaceAsync(workspace, userId, role, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if (!result.Result)
                return NotFound();
            return Ok(result.Result);
        }

        /// <summary>
        /// get user rols
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("{userId}/in/{workspace}/role")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public virtual async Task<IActionResult> GetUserRoles(Guid workspace, Guid userId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            if (loggedOnUserId != userId)
            {
                RServiceResult<bool> canViewAllUsersInformation =
                    await _userPermissionChecker.Check
                    (
                        loggedOnUserId,
                        new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value),
                        User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR",
                        SecurableItem.WorkpsaceEntityShortName,
                        SecurableItem.ChangeMemberRoleShortName,
                        workspace
                        );
                if (!string.IsNullOrEmpty(canViewAllUsersInformation.ExceptionString))
                    return BadRequest(canViewAllUsersInformation.ExceptionString);

                if (!canViewAllUsersInformation.Result)
                    return Forbid();
            }


            RServiceResult<IList<string>> roles = await _workspaceService.GetUserRoles(workspace, userId, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!string.IsNullOrEmpty(roles.ExceptionString))
                return BadRequest(roles.ExceptionString);

            return Ok(roles.Result.ToArray());
        }

        /// <summary>
        ///  add member  (does not send any email)
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="email"></param>
        /// <param name="notify">notify user</param>
        /// <returns></returns>
        [HttpPost("invitation/{workspace}/{email}/{notify}")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.InviteMembersOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> InviteMemberAsync(Guid workspace, string email, bool notify)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> result = await _workspaceService.InviteMemberAsync(workspace, loggedOnUserId, email, notify, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if (!result.Result)
                return NotFound();
            return Ok(result.Result);
        }

        /// <summary>
        /// revoke member invitation to a workspace
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="userId"></param>
        /// <returns></returns>

        [HttpDelete("invitation/{workspace}/{userId}")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.InviteMembersOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> RevokeInvitationAsync(Guid workspace, Guid userId)
        {
            RServiceResult<bool> result = await _workspaceService.RevokeInvitationAsync(workspace, userId, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if (!result.Result)
                return NotFound();
            return Ok(result.Result);
        }

        /// <summary>
        /// user workspace invitations
        /// </summary>
        /// <returns></returns>

        [HttpGet("invitation/mine")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(WorkspaceUserInvitationViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> GetUserInvitationsAsync()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<WorkspaceUserInvitationViewModel[]> result = await _workspaceService.GetUserInvitationsAsync(loggedOnUserId, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            return Ok(result.Result);
        }

        /// <summary>
        /// workspace invitations
        /// </summary>
        /// <param name="workspace"></param>
        /// <returns></returns>

        [HttpGet("invitation/{workspace}")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.InviteMembersOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(WorkspaceUserInvitationViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> GetWorkspaceInvitationsAsync(Guid workspace)
        {
            RServiceResult<WorkspaceUserInvitationViewModel[]> result = await _workspaceService.GetWorkspaceInvitationsAsync(workspace, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            return Ok(result.Result);
        }


        /// <summary>
        /// process workspace invitation
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="reject"></param>
        /// <returns></returns>
        [HttpPut("invitation/{workspaceId}/process/{reject}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> ProcessWorkspaceInvitationAsync(Guid workspaceId, bool reject)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<bool> result = await _workspaceService.ProcessWorkspaceInvitationAsync(workspaceId, loggedOnUserId, reject, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if (!result.Result)
                return NotFound();
            return Ok(result.Result);
        }


        /// <summary>
        /// leave a workspace
        /// </summary>
        /// <param name="workspace"></param>
        /// <returns></returns>
        [HttpDelete("leave/{workspace}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> LeaveWorkspaceAsync(Guid workspace)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<bool> result = await _workspaceService.LeaveWorkspaceAsync(workspace, loggedOnUserId, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if (!result.Result)
                return NotFound();
            return Ok(result.Result);
        }

        /// <summary>
        /// get logged on user securableitems (permissions) in workspace
        /// </summary>
        /// <param name="workspace"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [Route("securableitems/{workspace}")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<SecurableItem>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> GetUserSecurableItemsStatus(Guid workspace)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<SecurableItem[]> res = await _workspaceService.GetUserSecurableItemsStatus(workspace, loggedOnUserId, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");

            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }


        /// <summary>
        /// workspace service
        /// </summary>
        protected readonly IWorkspaceService _workspaceService;

        /// <summary>
        /// IUserPermissionChecker instance
        /// </summary>
        protected IUserPermissionChecker _userPermissionChecker;

        /// <summary>
        /// IAppUserService instance
        /// </summary>
        protected IAppUserService _appUserService;

        /// <summary>
        /// workspace service
        /// </summary>
        /// <param name="workspaceService"></param>
        /// <param name="userPermissionChecker"></param>
        /// <param name="appUserService"></param>
        public WorkspaceMemberControllerBase(IWorkspaceService workspaceService, IUserPermissionChecker userPermissionChecker, IAppUserService appUserService)
        {
            _workspaceService = workspaceService;
            _userPermissionChecker = userPermissionChecker;
            _appUserService = appUserService;
        }
    }
}
