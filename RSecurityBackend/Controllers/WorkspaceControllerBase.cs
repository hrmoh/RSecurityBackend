using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Models.Auth.Memory;
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
                                User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR",
                                SecurableItem.WorkspaceEntityShortName,
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
            RServiceResult<WorkspaceViewModel> result = await _workspaceService.AddWorkpspaceAsync(loggedOnUserId, model.Name, model.Description, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
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
        [Authorize(Policy = SecurableItem.WorkspaceEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public virtual async Task<IActionResult> UpdateWorkpspaceAsync(Guid workspace, [FromBody] WorkspaceViewModel model)
        {
            if (model.Id != workspace)
                return BadRequest("model.Id != workspace");
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            
            RServiceResult<bool> result = await _workspaceService.UpdateWorkpspaceAsync(loggedOnUserId, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR", model);
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

            RServiceResult<bool> result = await _workspaceService.DeleteWorkspaceAsync(loggedOnUserId, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR", id);
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

            RServiceResult<WorkspaceViewModel[]> result = await _workspaceService.GetMemberWorkspacesAsync(loggedOnUserId, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR", onlyActive, onlyOwned, onlyMember);
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

            RServiceResult<WorkspaceViewModel> result = await _workspaceService.GetUserWorkspaceByIdAsync(workspace, loggedOnUserId, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);

            if(result.Result == null)
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
