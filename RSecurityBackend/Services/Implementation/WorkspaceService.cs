using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Cloud;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
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
                    Users = new List<RAppUser>() { await _userManager.Users.AsNoTracking().Where(u => u.Id == userId).SingleAsync() }
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
        /// Update workspace
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdateWorkpspaceAsync(Guid userId, RWorkspace model)
        {
            try
            {
                var ws = await _context.RWorkspaces.Include(w => w.Users).Where(w => w.Id == model.Id && w.OwnerId == userId).SingleOrDefaultAsync();
                if(ws == null)
                {
                    return new RServiceResult<bool>(false);//not found
                }
                model.Name = model.Name.Trim();
                if (string.IsNullOrEmpty(model.Name))
                {
                    return new RServiceResult<bool>(false, "Name cannot be empty");
                }
                var alreadyUsedWorkspace = await _context.RWorkspaces.AsNoTracking().Where(w => w.Name == model.Name && w.OwnerId == model.OwnerId && w.Id != ws.Id).FirstOrDefaultAsync();
                if (alreadyUsedWorkspace != null)
                {
                    return new RServiceResult<bool>(false, $"The (new) owner user aleady owns a workspace called {model.Name} with code {alreadyUsedWorkspace.Id}");
                }
                
                ws.Name = model.Name;
                ws.Description = model.Description;
                ws.IsPublic = model.IsPublic;
                ws.Active = model.Active;
                ws.OwnerId = model.OwnerId;
                ws.WokspaceOrder = model.WokspaceOrder;

                //if you are transferring ownership, we ensure you have revokable access to the workspace
                if (model.OwnerId != userId)
                {
                    if (ws.Users == null)
                    {
                        ws.Users = new List<RAppUser>();
                    }
                    if (!ws.Users.Any(u => u.Id == userId))
                    {
                        ws.Users.Add(await _userManager.Users.AsNoTracking().Where(u => u.Id == userId).SingleAsync());
                    }
                }

                _context.Update(ws);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete workspace
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteWorkspaceAsync(Guid userId, Guid id)
        {
            try
            {
                var ws = await _context.RWorkspaces.Include(w => w.Users).Where(w => w.Id == id && w.OwnerId == userId).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false);//not found
                }
                _context.Remove(ws);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get owner workspaces
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="onlyActive"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RWorkspace[]>> GetOwnedWorkspacesAsync(Guid userId, bool onlyActive)
        {
            try
            {
                return new RServiceResult<RWorkspace[]>(
                    await _context.RWorkspaces.AsNoTracking()
                            .Where(w => w.OwnerId == userId && (onlyActive == false || w.Active == true))
                            .OrderBy(w => w.WokspaceOrder)
                            .ToArrayAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<RWorkspace[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// member workspaces
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="onlyActive"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RWorkspace[]>> GetMemberWorkspacesAsync(Guid userId, bool onlyActive)
        {
            try
            {
                var user = await _userManager.Users.AsNoTracking().Where(u => u.Id == userId).SingleAsync();
                return new RServiceResult<RWorkspace[]>(
                    await _context.RWorkspaces.Include(w => w.Users).AsNoTracking()
                            .Where(w => w.Users.Contains(user) && (onlyActive == false || w.Active == true))
                            .OrderBy(w => w.WokspaceOrder)
                            .ToArrayAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<RWorkspace[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RSecurityDbContext<RAppUser, RAppRole, Guid> _context;

        /// <summary>
        /// Identity User Manageer
        /// </summary>
        protected UserManager<RAppUser> _userManager;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userManager"></param>
        public WorkspaceService(RSecurityDbContext<RAppUser, RAppRole, Guid> context, UserManager<RAppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
    }
}
