using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Cloud;
using RSecurityBackend.Models.Cloud.ViewModels;
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
    public class WorkspaceService : IWorkspaceService
    {
        /// <summary>
        /// add workspace
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="isPublic"></param>
        /// <returns></returns>
        public async Task<RServiceResult<WorkspaceViewModel>> AddWorkpspaceAsync(Guid userId, string name, string description, bool isPublic)
        {
            try
            {
                name = name.Trim();
                if(string.IsNullOrEmpty(name) )
                {
                    return new RServiceResult<WorkspaceViewModel>(null, "Name cannot be empty");
                }
                var alreadyUsedWorkspace = await _context.RWorkspaces.Include(w => w.Members).AsNoTracking().Where(w => w.Name == name && w.Members.Any(m => m.RAppUserId == userId && m.Status == RWSUserMembershipStatus.Owner)).FirstOrDefaultAsync();
                if(alreadyUsedWorkspace != null)
                {
                    return new RServiceResult<WorkspaceViewModel>(null, $"The user aleady owns a workspace called {name} with code {alreadyUsedWorkspace.Id}");
                }
                var ws = new RWorkspace()
                {
                    Name = name,
                    Description = description,
                    IsPublic = isPublic,
                    CreateDate = DateTime.Now,
                    Active = true,
                    Members = new List<RWSUser>()
                    {  
                        new RWSUser()
                        {
                            RAppUserId = userId,
                            InviteDate = DateTime.Now,
                            MemberFrom = DateTime.Now,
                            Status = RWSUserMembershipStatus.Owner,
                        }
                    }
                };
                _context.Add(ws);
                await _context.SaveChangesAsync();
                return new RServiceResult<WorkspaceViewModel>
                    (
                    new WorkspaceViewModel()
                    {
                        Id = ws.Id,
                        Name = ws.Name,
                        Description = ws.Description,
                        IsPublic = ws.IsPublic,
                        CreateDate = ws.CreateDate,
                        Active = ws.Active,
                        WokspaceOrder = ws.WokspaceOrder,
                        Members = ws.Members.Select(m => new RWSUserViewModel()
                        {
                            Id = m.Id,
                            RAppUser = new PublicRAppUser()
                            {
                                Id = m.RAppUser.Id,
                                Username = m.RAppUser.UserName,
                                Email = m.RAppUser.Email,
                                FirstName = m.RAppUser.FirstName,
                                SureName = m.RAppUser.SureName,
                                PhoneNumber = m.RAppUser.PhoneNumber,
                                RImageId = m.RAppUser.RImageId,
                                Status = m.RAppUser.Status,
                                NickName = m.RAppUser.NickName,
                                Website = m.RAppUser.Website,
                                Bio = m.RAppUser.Bio,
                                EmailConfirmed = m.RAppUser.EmailConfirmed
                            },
                            Status = m.Status,
                            InviteDate = m.InviteDate,
                            MemberFrom = m.MemberFrom,
                        }).ToArray(),
                    }
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<WorkspaceViewModel>(null, exp.ToString());
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
                var ws = await _context.RWorkspaces.Include(w => w.Members).Where(w => w.Id == model.Id && w.Members.Any(m => m.RAppUserId == userId && m.Status == RWSUserMembershipStatus.Owner) ).SingleOrDefaultAsync();
                if(ws == null)
                {
                    return new RServiceResult<bool>(false);//not found
                }
                model.Name = model.Name.Trim();
                if (string.IsNullOrEmpty(model.Name))
                {
                    return new RServiceResult<bool>(false, "Name cannot be empty");
                }
                var alreadyUsedWorkspace = await _context.RWorkspaces.AsNoTracking().Where(w => w.Name == model.Name && w.Members.Any(m => m.RAppUserId == userId && m.Status == RWSUserMembershipStatus.Owner) && w.Id != ws.Id).FirstOrDefaultAsync();
                if (alreadyUsedWorkspace != null)
                {
                    return new RServiceResult<bool>(false, $"The (new) owner user aleady owns a workspace called {model.Name} with code {alreadyUsedWorkspace.Id}");
                }
                
                ws.Name = model.Name;
                ws.Description = model.Description;
                ws.IsPublic = model.IsPublic;
                ws.Active = model.Active;
                ws.WokspaceOrder = model.WokspaceOrder;

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
                var ws = await _context.RWorkspaces.Include(w => w.Members).Where(w => w.Id == id && w.Members.Any(m => m.RAppUserId == userId && m.Status == RWSUserMembershipStatus.Owner)).SingleOrDefaultAsync();
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
                            .Where(w => w.Members.Any(m => m.RAppUserId == userId && m.Status == RWSUserMembershipStatus.Owner) && (onlyActive == false || w.Active == true))
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
                var workspaces = await _context.RWorkspaces.Include(w => w.Members).AsNoTracking()
                            .Where(w => w.Members.Any(m => m.RAppUserId == userId) && (onlyActive == false || w.Active == true))
                            .OrderBy(w => w.WokspaceOrder)
                            .ToArrayAsync();
                return new RServiceResult<RWorkspace[]>(
                   workspaces
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<RWorkspace[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get workspace by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RWorkspace>> GetWorkspaceByIdAsync(Guid id)
        {
            try
            {
                return new RServiceResult<RWorkspace>(await _context.RWorkspaces.AsNoTracking().Where(w => w.Id == id).SingleOrDefaultAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<RWorkspace>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get user workspace information
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RWorkspace>> GetUserWorkspaceByIdAsync(Guid id, Guid userId)
        {
            try
            {
                var ws = await _context.RWorkspaces.AsNoTracking().Where(w => w.Id == id && w.Members.Any(m => m.RAppUserId == userId)).SingleOrDefaultAsync();
                return new RServiceResult<RWorkspace>(ws);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RWorkspace>(null, exp.ToString());
            }
        }

        /// <summary>
        /// add member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerOrModeratorId"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> AddMemberByEmailAsync(Guid workspaceId, Guid ownerOrModeratorId, string email)
        {
            try
            {
                var ws = await _context.RWorkspaces.Include(w => w.Members).Where(w => w.Id == workspaceId && w.Members.Any(m => m.RAppUserId == ownerOrModeratorId && (m.Status == RWSUserMembershipStatus.Owner || m.Status == RWSUserMembershipStatus.Moderator ))).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false);//not found
                }
                var user = await _userManager.FindByEmailAsync(email);
                if(ws.Members.Any(m => m.RAppUserId == user.Id))
                {
                    return new RServiceResult<bool>(false, "User is already a member."); 
                }
                ws.Members.Add(new RWSUser()
                {
                    RAppUserId = user.Id,
                    Status = RWSUserMembershipStatus.Invited,
                    InviteDate = DateTime.Now,
                });
                _context.Update(ws);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString() );
            }
        }

        /// <summary>
        /// delete member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerOrModeratorId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteMemberAsync(Guid workspaceId, Guid ownerOrModeratorId, Guid userId)
        {
            try
            {
                var ws = await _context.RWorkspaces.Include(w => w.Members).Where(w => w.Id == workspaceId && w.Members.Any(m => m.RAppUserId == ownerOrModeratorId && (m.Status == RWSUserMembershipStatus.Owner || m.Status == RWSUserMembershipStatus.Moderator))).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false);//not found
                }
                var user = await _userManager.Users.AsNoTracking().Where(u => u.Id == userId).SingleAsync();
                var member = ws.Members.Where(m => m.RAppUserId == userId).SingleOrDefault();
                if (member == null)
                {
                    return new RServiceResult<bool>(false, "User is not a member.");
                }
                
                ws.Members.Remove(member);
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
        /// leave a workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> LeaveWorkspaceAsync(Guid workspaceId, Guid userId)
        {
            try
            {
                var ws = await _context.RWorkspaces.Include(w => w.Members).Where(w => w.Id == workspaceId).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false);//not found
                }
                var user = await _userManager.Users.AsNoTracking().Where(u => u.Id == userId).SingleAsync();
                var member = ws.Members.Where(m => m.RAppUserId == userId).SingleOrDefault();
                if (member == null)
                {
                    return new RServiceResult<bool>(false, "User is not a member.");
                }

                ws.Members.Remove(member);
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
        /// restrict workspace adding
        /// </summary>
        public virtual bool RestrictWorkspaceAdding
        {
            get { return false; }
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
