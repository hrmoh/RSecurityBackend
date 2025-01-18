using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Services;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace RSecurityBackend.Controllers
{
    /// <summary>
    /// options controller
    /// </summary>
    [Produces("application/json")]
    [Route("api/options")]
    public abstract class RGenericOptionsControllerBase : Controller
    {
        /// <summary>
        /// get user level option, if option value is not found it returns empty string
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("{name}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> GetValue(string name)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            var res = await _optionsService.GetValueAsync(name, loggedOnUserId, null);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get user level option
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPut("{name}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> SetValue(string name, [FromBody] string value)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _optionsService.SetAsync(name, value, loggedOnUserId, null);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get global option value, Security Warning: every authenticated user could see value of global options, so do not store sensitive data into them  or you can override this behaviour in your derived class
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>

        [HttpGet("global/{name}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> GetGlobalOptionValue(string name)
        {
            var res = await _optionsService.GetValueAsync(name, null, null);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }


        /// <summary>
        /// set global option value, Security Warning: every authenticated user could change value of global options, so do not store sensitive data into them or you can override this behaviour in your derived class
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPut("global/{name}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> SetGlobalOptionValue(string name, [FromBody] string value)
        {
            var res = await _optionsService.SetAsync(name, value, null, null);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get user level option in a workspace, if option value is not found it returns empty string
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("{workspace}/{name}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> GetWorkspaceOptionValue(Guid workspace, string name)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            var res = await _optionsService.GetValueAsync(name, loggedOnUserId, workspace);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get user level option in a workspace
        /// </summary>
        /// <param name="name"></param>
        /// <param name="workspace"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPut("{workspace}/{name}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> SetWorkspaceOptionValue(Guid workspace, string name, [FromBody] string value)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _optionsService.SetAsync(name, value, loggedOnUserId, workspace);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get workspace global option value, Security Warning: every authenticated user in a workspace could see value of global options, so do not store sensitive data into them  or you can override this behaviour in your derived class
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="name"></param>
        /// <returns></returns>

        [HttpGet("global/{workspace}/{name}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> GetGlobalWorkspaceOptionValue(Guid workspace, string name)
        {
            var res = await _optionsService.GetValueAsync(name, null, workspace);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }


        /// <summary>
        /// set global option value, Security Warning: every authenticated user could change value of global options, so do not store sensitive data into them or you can override this behaviour in your derived class
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPut("global/{workspace}/{name}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> SetGlobalWorkspaceOptionValue(Guid workspace, string name, [FromBody] string value)
        {
            var res = await _optionsService.SetAsync(name, value, null, workspace);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="optionsService"></param>
        public RGenericOptionsControllerBase(IRGenericOptionsService optionsService)
        {
            _optionsService = optionsService;
        }

        /// <summary>
        /// Options Service
        /// </summary>
        protected readonly IRGenericOptionsService _optionsService;
    }
}
