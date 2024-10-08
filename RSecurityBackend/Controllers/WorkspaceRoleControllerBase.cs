﻿using Microsoft.AspNetCore.Authorization;
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

namespace RSecurityBackend.Controllers
{
    /// <summary>
    /// Workspace User roles
    /// </summary>
    [Produces("application/json")]
    [Route("api/workspace/roles")]
    public abstract class WorkspaceRoleControllerBase : Controller
    {
        /// <summary>
        /// All Roles Information
        /// </summary>
        /// <returns>All Roles Information</returns>
        [HttpGet("{workspace}")]
        [Authorize(Policy = SecurableItem.WorkspaceRoleEntityShortName + ":" + SecurableItem.ViewOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RWSRole>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> Get(Guid workspace)
        {
            RServiceResult<RWSRole[]> rolesInfo = await _roleService.GetAllRoles(workspace, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (rolesInfo.Result == null)
            {
                return BadRequest(rolesInfo.ExceptionString);
            }
            return Ok(rolesInfo.Result);
        }

        /// <summary>
        /// returns role information
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="roleName">role name</param>
        /// <returns>role information</returns>
        [HttpGet("{workspace}/{roleName}")]
        [Authorize(Policy = SecurableItem.WorkspaceRoleEntityShortName + ":" + SecurableItem.ViewOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RWSRole))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(Guid workspace, string roleName)
        {
            RServiceResult<RWSRole> roleInfo = await _roleService.GetRoleInformation(workspace, roleName, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (roleInfo.Result == null)
            {
                if (string.IsNullOrEmpty(roleInfo.ExceptionString))
                    return NotFound();
                return BadRequest(roleInfo.ExceptionString);
            }
            return Ok(roleInfo.Result);
        }

        /// <summary>
        /// add a new role
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="newGroupInfo"></param>
        /// <returns>id if required could be retrieved from return value</returns>
        [HttpPost("{workspace}")]
        [Authorize(Policy = SecurableItem.WorkspaceRoleEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RWSRole))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> Post(Guid workspace, [FromBody] RWSRole newGroupInfo)
        {
            if (workspace != newGroupInfo.WorkspaceId)
                return BadRequest("workspace != newGroupInfo.WorkspaceId");
            RServiceResult<RWSRole> result = await _roleService.AddRole(newGroupInfo, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (result.Result == null)
                return BadRequest(result.ExceptionString);
            return Ok(result.Result);
        }

        /// <summary>
        /// update existing role
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="roleName">role name</param>
        /// <param name="existingGroupInfo">existingGroupInfo.id could be passed empty and it is ignored completely</param>
        /// <returns>true if succeeds</returns>
        [HttpPut("{workspace}/{roleName}")]
        [Authorize(Policy = SecurableItem.WorkspaceRoleEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> Put(Guid workspace, string roleName, [FromBody] RWSRole existingGroupInfo)
        {
            if (workspace != existingGroupInfo.WorkspaceId)
                return BadRequest("workspace != existingGroupInfo.WorkspaceId");
            RServiceResult<bool> res = await _roleService.ModifyRole(workspace, roleName, existingGroupInfo, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!res.Result)
                return BadRequest(res.ExceptionString);

            return Ok(true);

        }

        /// <summary>
        /// delete role
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="roleId">role id</param>
        /// <returns>true if succeeds</returns>
        [HttpDelete("{workspace}/{roleId}")]
        [Authorize(Policy = SecurableItem.WorkspaceRoleEntityShortName + ":" + SecurableItem.DeleteOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> Delete(Guid workspace, Guid roleId)
        {

            RServiceResult<bool> res = await _roleService.DeleteRole(workspace, roleId, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");
            if (!res.Result)
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(true);
        }

        /// <summary>
        /// lists role permissions
        /// </summary>
        /// <returns></returns>
        [HttpGet("{workspace}/permissions/{roleName}")]
        [Authorize(Policy = SecurableItem.WorkspaceRoleEntityShortName + ":" + SecurableItem.ViewOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<SecurableItem[]>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetRoleSecurableItemsStatus(Guid workspace, string roleName)
        {
            RServiceResult<SecurableItem[]> res = await _roleService.GetRoleSecurableItemsStatus(workspace, roleName, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");

            if (res.Result == null)
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }


        /// <summary>
        /// Saves role permissions
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="roleName">role name</param>
        /// <param name="securableItems"></param>
        /// <returns></returns>
        [HttpPut("{workspace}/permissions/{roleName}")]
        [Authorize(Policy = SecurableItem.WorkspaceRoleEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<SecurableItem[]>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SetRoleSecurableItemsStatus(Guid workspace, string roleName, [FromBody] SecurableItem[] securableItems)
        {
            RServiceResult<bool> res = await _roleService.SetRoleSecurableItemsStatus(workspace, roleName, securableItems, User.Claims.Any(c => c.Type == "Language") ? User.Claims.FirstOrDefault(c => c.Type == "Language").Value : "fa-IR");

            if (!res.Result)
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }

        /// <summary>
        /// Get All SecurableItems
        /// </summary>
        /// <returns>All All SecurableItems</returns>
        [HttpGet]
        [Route("securableitems")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<SecurableItem>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public IActionResult GetSecurableItems()
        {
            return Ok(_roleService.GetSecurableItems());
        }


        /// <summary>
        /// IWorkspaceRolesService instance
        /// </summary>
        protected IWorkspaceRolesService _roleService;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="roleService"></param>
        public WorkspaceRoleControllerBase(IWorkspaceRolesService roleService)
        {
            _roleService = roleService;
        }
    }
}
