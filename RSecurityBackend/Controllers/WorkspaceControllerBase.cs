using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Cloud;
using RSecurityBackend.Models.Cloud.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RSecurityBackend.Controllers
{
    /// <summary>
    /// Worspace API
    /// </summary>
    [Produces("application/json")]
    [Route("api/workspace")]
    public abstract class WorkspaceControllerBase : Controller
    {
        /// <summary>
        /// add workspace (if you want it to be limited override WorkspaceService.RestrictWorkspaceAdding to look at workspace:add )
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(WorkspaceViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public virtual async Task<IActionResult> AddWorkpspaceAsync([FromBody] NewWorkspaceModel model)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            if (_workspaceService.RestrictWorkspaceCreationToAuthorizarion)
            {
                Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
                RServiceResult<bool>
                    canAdd =
                        await _userPermissionChecker.Check
                            (
                                loggedOnUserId,
                                sessionId,
                                User.Claims.FirstOrDefault(c => c.Type == "Language").Value,
                                SecurableItem.WorkpsaceEntityShortName,
                                SecurableItem.AddOperationShortName
                                );
                if (!string.IsNullOrEmpty(canAdd.ExceptionString))
                {
                    return BadRequest(canAdd.ExceptionString);
                }
                if(canAdd.Result == false)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
            }
            RServiceResult<WorkspaceViewModel> result = await _workspaceService.AddWorkpspaceAsync(loggedOnUserId, model.Name, model.Description, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
            if (result.Result == null)
                return BadRequest(result.ExceptionString);
            return Ok(result.Result);
        }

        /// <summary>
        /// Update workspace
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("{workspace}")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> UpdateWorkpspaceAsync(Guid workspace, [FromBody] WorkspaceViewModel model)
        {
            if (model.Id != workspace)
                return BadRequest("model.Id != workspace");
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            
            RServiceResult<bool> result = await _workspaceService.UpdateWorkpspaceAsync(loggedOnUserId, User.Claims.FirstOrDefault(c => c.Type == "Language").Value, model);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if(result.Result == false)
                return NotFound();
            
            return Ok();
        }

        /// <summary>
        /// delete workspace
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> DeleteWorkspaceAsync(Guid id)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<bool> result = await _workspaceService.DeleteWorkspaceAsync(loggedOnUserId, User.Claims.FirstOrDefault(c => c.Type == "Language").Value, id);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if (result.Result == false)
                return NotFound();

            return Ok();
        }

        /// <summary>
        /// user workspaces (member or owner)
        /// </summary>
        /// <param name="onlyActive"></param>
        /// <param name="onlyMember"></param>
        /// <param name="onlyOwned"></param>
        /// <remarks>members are invalid</remarks>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(WorkspaceViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> GetMemberWorkspacesAsync(bool onlyActive = true, bool onlyOwned = false, bool onlyMember = false)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<WorkspaceViewModel[]> result = await _workspaceService.GetMemberWorkspacesAsync(loggedOnUserId, User.Claims.FirstOrDefault(c => c.Type == "Language").Value, onlyActive, onlyOwned, onlyMember);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);

            return Ok(result.Result);
        }


        /// <summary>
        /// get user workspace information
        /// </summary>
        /// <param name="workspace"></param>
        /// <returns></returns>
        [HttpGet("{workspace}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(WorkspaceViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> GetUserWorkspaceByIdAsync(Guid workspace)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<WorkspaceViewModel> result = await _workspaceService.GetUserWorkspaceByIdAsync(workspace, loggedOnUserId, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);

            if(result.Result == null)
                return NotFound();

            return Ok(result.Result);
        }

        /// <summary>
        ///  add member  (does not send any email)
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="email"></param>
        /// <param name="notify">notify user</param>
        /// <returns></returns>
        [HttpPost("{workspace}/invite/{email}/{notify}")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.InviteMembersOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> InviteMemberAsync(Guid workspace, string email, bool notify)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> result = await _workspaceService.InviteMemberAsync(workspace, loggedOnUserId, email, notify, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if(!result.Result)
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
            RServiceResult<bool> result = await _workspaceService.RevokeInvitationAsync(workspace, userId, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
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

        [HttpGet("invitations/mine")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(WorkspaceUserInvitationViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> GetUserInvitationsAsync()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<WorkspaceUserInvitationViewModel[]> result = await _workspaceService.GetUserInvitationsAsync(loggedOnUserId, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            return Ok(result.Result);
        }

        /// <summary>
        /// workspace invitations
        /// </summary>
        /// <param name="workspace"></param>
        /// <returns></returns>

        [HttpGet("{workspace}/invitations")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.InviteMembersOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(WorkspaceUserInvitationViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> GetWorkspaceInvitationsAsync(Guid workspace)
        {
            RServiceResult<WorkspaceUserInvitationViewModel[]> result = await _workspaceService.GetWorkspaceInvitationsAsync(workspace, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            return Ok(result.Result);
        }

        /// <summary>
        /// delete member
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpDelete("member/{workspace}/{userId}")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.RemoveMembersOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> DeleteMemberAsync(Guid workspace, Guid userId)
        {
            RServiceResult<bool> result = await _workspaceService.DeleteMemberAsync(workspace, userId, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
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

            RServiceResult<bool> result = await _workspaceService.LeaveWorkspaceAsync(workspace, loggedOnUserId, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if (!result.Result)
                return NotFound();
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

            RServiceResult<bool> result = await _workspaceService.ProcessWorkspaceInvitationAsync(workspaceId, loggedOnUserId, reject, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
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
        [HttpGet("{workspace}/member")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RWSUserViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> GetWorkspaceMembersAsync(Guid workspace)
        {
            if(_workspaceService.RestrictWorkspaceMembersQueryToAuthorizarion)
            {
                Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
                Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
                RServiceResult<bool>
                    canAdd =
                        await _userPermissionChecker.Check
                            (
                                loggedOnUserId,
                                sessionId,
                                User.Claims.FirstOrDefault(c => c.Type == "Language").Value,
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
            RServiceResult<RWSUserViewModel[]> result = await _workspaceService.GetWorkspaceMembersAsync(workspace, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
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
        [HttpPut("{workspace}/member/{userId}/status/{status}")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.ChangeMemberStatusOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> ChangeMemberStatusAsync(Guid workspace, Guid userId, RWSUserMembershipStatus status)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<bool> result = await _workspaceService.ChangeMemberStatusAsync(workspace, loggedOnUserId, userId, status, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
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

        [HttpPut("{workspace}/member/{userId}/role/{role}")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.ChangeMemberRoleShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> AddUserToRoleInWorkspaceAsync(Guid workspace, Guid userId, string role)
        {
            RServiceResult<bool> result = await _workspaceService.AddUserToRoleInWorkspaceAsync(workspace, userId, role, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
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
        [HttpDelete("member/{userId}/from/{role}/in/{workspace}")]
        [Authorize(Policy = SecurableItem.WorkpsaceEntityShortName + ":" + SecurableItem.ChangeMemberRoleShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> RemoveUserFromRoleInWorkspaceAsync(Guid workspace, Guid userId, string role)
        {
            RServiceResult<bool> result = await _workspaceService.RemoveUserFromRoleInWorkspaceAsync(workspace, userId, role, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
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
        [HttpGet("{workspace}/member/{userId}/role")]
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
                        User.Claims.FirstOrDefault(c => c.Type == "Language").Value,
                        SecurableItem.WorkpsaceEntityShortName,
                        SecurableItem.ChangeMemberRoleShortName,
                        workspace
                        );
                if (!string.IsNullOrEmpty(canViewAllUsersInformation.ExceptionString))
                    return BadRequest(canViewAllUsersInformation.ExceptionString);

                if (!canViewAllUsersInformation.Result)
                    return Forbid();
            }


            RServiceResult<IList<string>> roles = await _workspaceService.GetUserRoles(workspace, userId, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);
            if (!string.IsNullOrEmpty(roles.ExceptionString))
                return BadRequest(roles.ExceptionString);

            return Ok(roles.Result.ToArray());
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

            RServiceResult<SecurableItem[]> res = await _workspaceService.GetUserSecurableItemsStatus(workspace, loggedOnUserId, User.Claims.FirstOrDefault(c => c.Type == "Language").Value);

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
        public WorkspaceControllerBase(IWorkspaceService workspaceService, IUserPermissionChecker userPermissionChecker, IAppUserService appUserService)
        {
            _workspaceService = workspaceService;
            _userPermissionChecker = userPermissionChecker;
            _appUserService = appUserService;
        }
    }
}
