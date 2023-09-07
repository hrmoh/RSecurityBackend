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
        public virtual async Task<RServiceResult<WorkspaceViewModel>> AddWorkpspaceAsync(Guid userId, string name, string description, bool isPublic)
        {
            try
            {
                name = name.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    return new RServiceResult<WorkspaceViewModel>(null, "Name cannot be empty");
                }
                var alreadyUsedWorkspace = await _context.RWorkspaces.Include(w => w.Members).AsNoTracking().Where(w => w.Name == name && w.Members.Any(m => m.RAppUserId == userId && m.Status == RWSUserMembershipStatus.Owner)).FirstOrDefaultAsync();
                if (alreadyUsedWorkspace != null)
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
        public virtual async Task<RServiceResult<bool>> UpdateWorkpspaceAsync(Guid userId, WorkspaceViewModel model)
        {
            try
            {
                var ws = await _context.RWorkspaces.Include(w => w.Members).Where(w => w.Id == model.Id && w.Members.Any(m => m.RAppUserId == userId && m.Status == RWSUserMembershipStatus.Owner)).SingleOrDefaultAsync();
                if (ws == null)
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
        public virtual async Task<RServiceResult<bool>> DeleteWorkspaceAsync(Guid userId, Guid id)
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
        public virtual async Task<RServiceResult<WorkspaceViewModel[]>> GetOwnedWorkspacesAsync(Guid userId, bool onlyActive)
        {
            try
            {
                var workspaces = await _context.RWorkspaces.Include(w => w.Members).ThenInclude(m => m.RAppUser).AsNoTracking()
                            .Where(w => w.Members.Any(m => m.RAppUserId == userId && m.Status == RWSUserMembershipStatus.Owner) && (onlyActive == false || w.Active == true))
                            .OrderBy(w => w.WokspaceOrder)
                            .ToArrayAsync();
                return new RServiceResult<WorkspaceViewModel[]>(
                   workspaces.Select(ws => new WorkspaceViewModel()
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
                   }).ToArray()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<WorkspaceViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// member workspaces
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="onlyActive"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<WorkspaceViewModel[]>> GetMemberWorkspacesAsync(Guid userId, bool onlyActive)
        {
            try
            {
                var workspaces = await _context.RWorkspaces.Include(w => w.Members).ThenInclude(m => m.RAppUser).AsNoTracking()
                            .Where(w => w.Members.Any(m => m.RAppUserId == userId && m.Status != RWSUserMembershipStatus.Invited) && (onlyActive == false || w.Active == true))
                            .OrderBy(w => w.WokspaceOrder)
                            .ToArrayAsync();
                return new RServiceResult<WorkspaceViewModel[]>(
                   workspaces.Select(ws => new WorkspaceViewModel()
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
                   }).ToArray()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<WorkspaceViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get workspace by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<WorkspaceViewModel>> GetWorkspaceByIdAsync(Guid id)
        {
            try
            {
                var ws = await _context.RWorkspaces.AsNoTracking().Where(w => w.Id == id).SingleOrDefaultAsync();
                return new RServiceResult<WorkspaceViewModel>
                    (
                    ws == null ? null :
                    new WorkspaceViewModel()
                    {
                        Id = ws.Id,
                        Name = ws.Name,
                        Description = ws.Description,
                        IsPublic = ws.IsPublic,
                        CreateDate = ws.CreateDate,
                        Active = ws.Active,
                        WokspaceOrder = ws.WokspaceOrder,
                        Members = null,
                    }
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<WorkspaceViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get user workspace information
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<WorkspaceViewModel>> GetUserWorkspaceByIdAsync(Guid id, Guid userId)
        {
            try
            {
                var ws = await _context.RWorkspaces.Include(w => w.Members).ThenInclude(m => m.RAppUser).AsNoTracking().Where(w => w.Id == id && w.Members.Any(m => m.RAppUserId == userId)).SingleOrDefaultAsync();
                return new RServiceResult<WorkspaceViewModel>(ws == null ? null :
                    new WorkspaceViewModel()
                    {
                        Id = ws.Id,
                        Name = ws.Name,
                        Description = ws.Description,
                        IsPublic = ws.IsPublic,
                        CreateDate = ws.CreateDate,
                        Active = ws.Active,
                        WokspaceOrder = ws.WokspaceOrder,
                        Members = ws.Members.Where(m => m.RAppUserId == userId).Select(m => new RWSUserViewModel()
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
                    });
            }
            catch (Exception exp)
            {
                return new RServiceResult<WorkspaceViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// add member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerOrModeratorId"></param>
        /// <param name="email"></param>
        /// <param name="notifyUser"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> InviteMemberAsync(Guid workspaceId, Guid ownerOrModeratorId, string email, bool notifyUser)
        {
            try
            {
                var ws = await _context.RWorkspaces.Include(w => w.Members).Where(w => w.Id == workspaceId && w.Members.Any(m => m.RAppUserId == ownerOrModeratorId && (m.Status == RWSUserMembershipStatus.Owner || m.Status == RWSUserMembershipStatus.Moderator))).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false);//not found
                }
                var user = await _userManager.FindByEmailAsync(email);
                if (ws.Members.Any(m => m.RAppUserId == user.Id))
                {
                    return new RServiceResult<bool>(false, "User is already a member.");
                }
                var rsOption = await _optionsService.GetValueAsync("AllowInvitingMeToWorkspaces", user.Id);
                if(!string.IsNullOrEmpty(rsOption.ExceptionString))
                {
                    return new RServiceResult<bool>(false, rsOption.ExceptionString);
                }
                var optionValue = rsOption.Result;
                if(string.IsNullOrEmpty(optionValue))
                {
                    optionValue = AllowInvitingUsersToWorkspacesByDefault.ToString();
                }
                if(!bool.Parse(optionValue))
                {
                    return new RServiceResult<bool>(false, "User does not allow adding him/her to workpsaces");
                }
                ws.Members.Add(new RWSUser()
                {
                    RAppUserId = user.Id,
                    Status = RWSUserMembershipStatus.Invited,
                    InviteDate = DateTime.Now,
                });
                _context.Update(ws);
                await _context.SaveChangesAsync();

                if(notifyUser)
                {
                    await _notificationService.PushNotification(user.Id, $"Invitation to {ws.Name}", $"You have been invited to join workspace {ws.Name} by {(await _userManager.Users.AsNoTracking().Where(u => u.Id == ownerOrModeratorId).SingleAsync()).Email} ");
                }
                

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerOrModeratorId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> DeleteMemberAsync(Guid workspaceId, Guid ownerOrModeratorId, Guid userId)
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
        public virtual async Task<RServiceResult<bool>> LeaveWorkspaceAsync(Guid workspaceId, Guid userId)
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
        /// change member status
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerOrModeratorId"></param>
        /// <param name="userId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> ChangeMemberStatusAsync(Guid workspaceId, Guid ownerOrModeratorId, Guid userId, RWSUserMembershipStatus status)
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

                var admin = ws.Members.Where(m => m.RAppUserId == ownerOrModeratorId).SingleOrDefault();
                if(admin.Status != RWSUserMembershipStatus.Owner && admin.Status != RWSUserMembershipStatus.Moderator)
                {
                    return new RServiceResult<bool>(false, "User has not enough privileges to perform this operation.");
                }


                if(member.Status == status)
                {
                    return new RServiceResult<bool>(false, "New status is the same as original.");
                }

                if(status == RWSUserMembershipStatus.Owner)
                {
                    var alreadyUsedWorkspace = await _context.RWorkspaces.Include(w => w.Members).AsNoTracking().Where(w => w.Name == ws.Name && w.Members.Any(m => m.RAppUserId == userId && m.Status == RWSUserMembershipStatus.Owner)).FirstOrDefaultAsync();
                    if (alreadyUsedWorkspace != null)
                    {
                        return new RServiceResult<bool>(false, $"The user aleady owns a workspace called {ws.Name} with code {alreadyUsedWorkspace.Id}");
                    }

                    if (admin.Status != RWSUserMembershipStatus.Owner)
                    {
                        return new RServiceResult<bool>(false, "User has not enough privileges to perform this operation.");
                    }
                }

                if(member.Status == RWSUserMembershipStatus.Owner)
                {
                    if(!ws.Members.Where(m => m.RAppUserId != userId && m.Status == RWSUserMembershipStatus.Owner).Any())
                    {
                        return new RServiceResult<bool>(false, "Workspace remains ownerless after this changes and it is not permitted.");
                    }
                }

                member.Status = status;
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
        /// process workspace invitation
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="reject"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> ProcessWorkspaceInvitationAsync(Guid workspaceId, Guid userId, bool reject)
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
                if(reject)
                {
                    ws.Members.Remove(member);
                }
                else
                {
                    member.Status = RWSUserMembershipStatus.Member;
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
        /// add user to role in a workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerOrModeratorId"></param>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> AddUserToRoleInWorkspaceAsync(Guid workspaceId, Guid ownerOrModeratorId, Guid userId, string roleName)
        {
            try
            {
                var ws = await _context.RWorkspaces.AsNoTracking().Include(w => w.Members).Where(w => w.Id == workspaceId).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false, "Workspace not found.");
                }
                var user = await _userManager.Users.AsNoTracking().Where(u => u.Id == userId).SingleAsync();
                var member = ws.Members.Where(m => m.RAppUserId == userId).SingleOrDefault();
                if (member == null)
                {
                    return new RServiceResult<bool>(false, "User is not a member.");
                }

                var admin = ws.Members.Where(m => m.RAppUserId == ownerOrModeratorId).SingleOrDefault();
                if (admin.Status != RWSUserMembershipStatus.Owner && admin.Status != RWSUserMembershipStatus.Moderator)
                {
                    return new RServiceResult<bool>(false, "User has not enough privileges to perform this operation.");
                }

                var role = await _context.RWSRoles.AsNoTracking().Where(r => r.Name == roleName).SingleOrDefaultAsync();
                if(role == null)
                {
                    return new RServiceResult<bool>(false, "Role not foune");
                }

                if(_context.RWSUserRoles.Where(r => r.WorkspaceId == workspaceId && r.UserId == userId && r.RoleId == role.Id).Any())
                {
                    return new RServiceResult<bool>(false, "User already in role.");
                }

                RWSUserRole userRole = new RWSUserRole()
                {
                    WorkspaceId = workspaceId,
                    UserId = userId,
                    RoleId = role.Id,
                };

                _context.Add(userRole);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// remove user from role i
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerOrModeratorId"></param>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> RemoveUserFromRoleInWorkspaceAsync(Guid workspaceId, Guid ownerOrModeratorId, Guid userId, string roleName)
        {
            try
            {
                var ws = await _context.RWorkspaces.AsNoTracking().Include(w => w.Members).Where(w => w.Id == workspaceId).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false, "Workspace not found.");
                }
                var user = await _userManager.Users.AsNoTracking().Where(u => u.Id == userId).SingleAsync();
                var member = ws.Members.Where(m => m.RAppUserId == userId).SingleOrDefault();
                if (member == null)
                {
                    return new RServiceResult<bool>(false, "User is not a member.");
                }

                var admin = ws.Members.Where(m => m.RAppUserId == ownerOrModeratorId).SingleOrDefault();
                if (admin.Status != RWSUserMembershipStatus.Owner && admin.Status != RWSUserMembershipStatus.Moderator)
                {
                    return new RServiceResult<bool>(false, "User has not enough privileges to perform this operation.");
                }

                var role = await _context.RWSRoles.AsNoTracking().Where(r => r.Name == roleName).SingleOrDefaultAsync();
                if (role == null)
                {
                    return new RServiceResult<bool>(false, "Role not foune");
                }

                var userRole = _context.RWSUserRoles.Where(r => r.WorkspaceId == workspaceId && r.UserId == userId && r.RoleId == role.Id).SingleOrDefaultAsync();
                if (userRole == null)
                {
                    return new RServiceResult<bool>(false, "User is not in role.");
                }

                _context.Remove(userRole);
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
        public virtual bool RestrictWorkspaceCreationToAuthorizarion
        {
            get { return false; }
        }

        /// <summary>
        /// allow inviting users to workspaces by default
        /// </summary>
        public virtual bool AllowInvitingUsersToWorkspacesByDefault
        { 
            get { return true; } 
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
        /// Options service
        /// </summary>
        protected IRGenericOptionsService _optionsService;

        /// <summary>
        /// Notification Service
        /// </summary>
        protected IRNotificationService _notificationService;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userManager"></param>
        /// <param name="optionsService"></param>
        /// <param name="notificationService"></param>
        public WorkspaceService(RSecurityDbContext<RAppUser, RAppRole, Guid> context, UserManager<RAppUser> userManager, IRGenericOptionsService optionsService, IRNotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _optionsService = optionsService;
            _notificationService = notificationService;
        }
    }
}
