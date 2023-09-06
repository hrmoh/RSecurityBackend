using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Cloud;
using RSecurityBackend.Models.Cloud.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using System;
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
    public class WorkspaceControllerBase : Controller
    {
        /// <summary>
        /// add workspace (if you want it to be limited override WorkspaceService.RestrictWorkspaceAdding)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(WorkspaceViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> AddWorkpspaceAsync([FromBody] NewWorkspaceModel model)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(loggedOnUserId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }
            if (_workspaceService.RestrictWorkspaceAdding)
            {
                RServiceResult<bool>
                    canAdd =
                        await _userPermissionChecker.Check
                            (
                                loggedOnUserId,
                                sessionId,
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
            RServiceResult<WorkspaceViewModel> result = await _workspaceService.AddWorkpspaceAsync(loggedOnUserId, model.Name, model.Description, model.IsPublic);
            if (result.Result == null)
                return BadRequest(result.ExceptionString);
            return Ok(result.Result);
        }

        /// <summary>
        /// Update workspace
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateWorkpspaceAsync([FromBody] RWorkspace model)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(loggedOnUserId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }
            
            RServiceResult<bool> result = await _workspaceService.UpdateWorkpspaceAsync(loggedOnUserId, model);
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
        public async Task<IActionResult> DeleteWorkspaceAsync(Guid id)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(loggedOnUserId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            RServiceResult<bool> result = await _workspaceService.DeleteWorkspaceAsync(loggedOnUserId, id);
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
        /// <remarks>if user is not owner of a workspace owner data + users are invalid</remarks>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(WorkspaceViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetMemberWorkspacesAsync(bool onlyActive)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(loggedOnUserId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            RServiceResult<WorkspaceViewModel[]> result = await _workspaceService.GetMemberWorkspacesAsync(loggedOnUserId, onlyActive);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);

            return Ok(result.Result);
        }

        /// <summary>
        /// get owned workspaces
        /// </summary>
        /// <param name="onlyActive"></param>
        /// <returns></returns>
        [HttpGet("owned")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(WorkspaceViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetOwnedWorkspacesAsync(bool onlyActive)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(loggedOnUserId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            RServiceResult<WorkspaceViewModel[]> result = await _workspaceService.GetOwnedWorkspacesAsync(loggedOnUserId, onlyActive);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);

            return Ok(result.Result);
        }

        /// <summary>
        /// get user workspace information (if user is not the owner, owner data is invaid)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RWorkspace))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserWorkspaceByIdAsync(Guid id)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(loggedOnUserId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            RServiceResult<RWorkspace> result = await _workspaceService.GetUserWorkspaceByIdAsync(id, loggedOnUserId);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);

            if(result.Result == null)
                return NotFound();

            return Ok(result.Result);
        }

        /// <summary>
        ///  add member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost("{workspaceId}/member/{email}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> AddMemberByEmailAsync(Guid workspaceId, string email)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(loggedOnUserId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }
       
            RServiceResult<bool> result = await _workspaceService.AddMemberByEmailAsync(workspaceId, loggedOnUserId, email);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if(!result.Result)
                return NotFound();
            return Ok(result.Result);
        }

        /// <summary>
        /// delete member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpDelete("{workspaceId}/member/{userId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteMemberAsync(Guid workspaceId, Guid userId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(loggedOnUserId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            RServiceResult<bool> result = await _workspaceService.DeleteMemberAsync(workspaceId, loggedOnUserId, userId);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if (!result.Result)
                return NotFound();
            return Ok(result.Result);
        }

        /// <summary>
        /// leave a workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <returns></returns>
        [HttpDelete("{workspaceId}/leave")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> LeaveWorkspaceAsync(Guid workspaceId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(loggedOnUserId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            RServiceResult<bool> result = await _workspaceService.LeaveWorkspaceAsync(workspaceId, loggedOnUserId);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            if (!result.Result)
                return NotFound();
            return Ok(result.Result);
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
