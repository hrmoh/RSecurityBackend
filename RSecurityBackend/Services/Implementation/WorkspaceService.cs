using Microsoft.EntityFrameworkCore;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Cloud;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Workspace service implementation
    /// </summary>
    public class WorkspaceService
    {
        /// <summary>
        /// add workspace
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="isPublic"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RWorkspace>> AddWorkpspaceAsync(Guid userId, string name, string description, bool isPublic)
        {
            try
            {
                name = name.Trim();
                if(string.IsNullOrEmpty(name) )
                {
                    return new RServiceResult<RWorkspace>(null, "Name cannot be empty");
                }
                var alreadyUsedWorkspace = await _context.RWorkspaces.AsNoTracking().Where(w => w.Name == name && w.OwnerId == userId).FirstOrDefaultAsync();
                if(alreadyUsedWorkspace != null)
                {
                    return new RServiceResult<RWorkspace>(null, $"The user aleady owns a workspace called {name} with code {alreadyUsedWorkspace.Id}");
                }
                var ws = new RWorkspace()
                {
                    Name = name,
                    Description = description,
                    IsPublic = isPublic,
                    CreateDate = DateTime.Now,
                    OwnerId = userId,
                    Active = true,
                };
                _context.Add(ws);
                await _context.SaveChangesAsync();
                return new RServiceResult<RWorkspace>(ws);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RWorkspace>(null, exp.ToString());
            }
        }
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
