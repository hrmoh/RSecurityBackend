using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using System;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Workspace service implementation
    /// </summary>
    public class WorkspaceService
    {
        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RSecurityDbContext<RAppUser, RAppRole, Guid> _context;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        public WorkspaceService(RSecurityDbContext<RAppUser, RAppRole, Guid> context)
        {
            _context = context;
        }
    }
}
