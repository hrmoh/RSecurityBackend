using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSecurityBackend.Controllers
{
    /// <summary>
    /// Worspace API
    /// </summary>
    [Produces("application/json")]
    [Route("api/users")]
    public class WorkspaceControllerBase : Controller
    {
        /// <summary>
        /// workspace service
        /// </summary>
        protected readonly IWorkspaceService _workspaceService;
        
        /// <summary>
        /// workspace service
        /// </summary>
        /// <param name="workspaceService"></param>
        public WorkspaceControllerBase(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }
    }
}
