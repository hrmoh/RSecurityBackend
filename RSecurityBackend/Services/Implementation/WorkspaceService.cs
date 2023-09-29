using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Cloud;
using RSecurityBackend.Models.Cloud.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// <returns></returns>
        public virtual async Task<RServiceResult<WorkspaceViewModel>> AddWorkpspaceAsync(Guid userId, string name, string description)
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
                var roleCreationResult = await _rolesService.AddRole
                    (
                    new RWSRole()
                    {
                        WorkspaceId = ws.Id,
                        Name = _rolesService.AdministratorRoleName,
                        Description = "Admin Role (Owners + Moderators)",
                    }
                    );
                if (!string.IsNullOrEmpty(roleCreationResult.ExceptionString))
                    return new RServiceResult<WorkspaceViewModel>(null, roleCreationResult.ExceptionString);

                var roleAssignmentResult =  await AddUserToRoleInWorkspaceAsync(ws.Id, userId, _rolesService.AdministratorRoleName);
                if (!string.IsNullOrEmpty(roleAssignmentResult.ExceptionString))
                    return new RServiceResult<WorkspaceViewModel>(null, roleAssignmentResult.ExceptionString);
                return new RServiceResult<WorkspaceViewModel>
                    (
                    new WorkspaceViewModel()
                    {
                        Id = ws.Id,
                        Name = ws.Name,
                        Description = ws.Description,
                        CreateDate = ws.CreateDate,
                        Active = ws.Active,
                        WokspaceOrder = ws.WokspaceOrder,
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
                var ws = await _context.RWorkspaces.Where(w => w.Id == model.Id).SingleOrDefaultAsync();
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
                var userRoles = await _context.RWSUserRoles.Where(w => w.WorkspaceId == id).ToListAsync();
                if(userRoles.Any())
                {
                    _context.RemoveRange(userRoles);
                }
                var roles = await _context.RWSRoles.Where(w => w.WorkspaceId == id).ToListAsync();
                if (roles.Any())
                {
                    _context.RemoveRange(roles);
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
        /// member workspaces
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="onlyActive"></param>
        /// <param name="onlyOwned"></param>
        /// <param name="onlyMember"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<WorkspaceViewModel[]>> GetMemberWorkspacesAsync(Guid userId, bool onlyActive, bool onlyOwned, bool onlyMember)
        {
            try
            {
                if (onlyMember && onlyOwned)
                    return new RServiceResult<WorkspaceViewModel[]>(null, "onlyMember && onlyOwned");
                var userWorkspaces = await _context.RWSUsers.AsNoTracking().
                                        Where(u => u.RAppUserId == userId).ToArrayAsync();
                var idArray = userWorkspaces.Select(w => w.RWorkspaceId).ToArray();

                var workspacesUnfiltered = await _context.RWorkspaces.AsNoTracking().Where(w => idArray.Contains(w.Id)).ToArrayAsync();

                List<RWorkspace> workspaces = new List<RWorkspace>();
                foreach (var workspace in workspacesUnfiltered)
                {
                    if(onlyActive)
                    {
                        if (workspace.Active == false)
                            continue;
                    }
                    if(onlyOwned)
                    {
                        if (!userWorkspaces.Any(u => u.RWorkspaceId == workspace.Id && u.Status == RWSUserMembershipStatus.Owner))
                            continue;
                    }
                    if(onlyMember)
                    {
                        if (!userWorkspaces.Any(u => u.RWorkspaceId == workspace.Id && u.Status == RWSUserMembershipStatus.Member))
                            continue;
                    }
                    workspaces.Add(workspace);
                }

                
                return new RServiceResult<WorkspaceViewModel[]>(
                   workspaces.Select(ws => new WorkspaceViewModel()
                   {
                       Id = ws.Id,
                       Name = ws.Name,
                       Description = ws.Description,
                       CreateDate = ws.CreateDate,
                       Active = ws.Active,
                       WokspaceOrder = ws.WokspaceOrder,
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
                        CreateDate = ws.CreateDate,
                        Active = ws.Active,
                        WokspaceOrder = ws.WokspaceOrder,
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
                        CreateDate = ws.CreateDate,
                        Active = ws.Active,
                        WokspaceOrder = ws.WokspaceOrder,
                    });
            }
            catch (Exception exp)
            {
                return new RServiceResult<WorkspaceViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get workspace members
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<RWSUserViewModel[]>> GetWorkspaceMembersAsync(Guid workspaceId)
        {
            try
            {
                var m = await _context.RWSUsers.AsNoTracking().Include(r => r.RAppUser).Where(r => r.RWorkspaceId == workspaceId).ToListAsync();
                List<RWSUserViewModel> members = new List<RWSUserViewModel>();
                foreach (var item in m)
                {
                    members.Add
                        (
                        new RWSUserViewModel()
                        {
                            Id = item.Id,
                            RAppUser = new PublicRAppUser()
                            {
                                Id = item.RAppUser.Id,
                                Username = item.RAppUser.UserName,
                                Email = item.RAppUser.Email,
                                FirstName = item.RAppUser.FirstName,
                                SureName = item.RAppUser.SureName,
                                PhoneNumber = item.RAppUser.PhoneNumber,
                                RImageId = item.RAppUser.RImageId,
                                Status = item.RAppUser.Status,
                                NickName = item.RAppUser.NickName,
                                Website = item.RAppUser.Website,
                                Bio = item.RAppUser.Bio,
                                EmailConfirmed = item.RAppUser.EmailConfirmed
                            },
                            InviteDate = item.InviteDate,
                            Status = item.Status,
                        }
                        );
                }
                return new RServiceResult<RWSUserViewModel[]>(members.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<RWSUserViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// is user workspace member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> IsUserWorkspaceMember(Guid workspaceId, Guid userId)
        {
            try
            {
                return new RServiceResult<bool>
                    (
                    await _context.RWorkspaces.Include(w => w.Members).Where(w => w.Id == workspaceId && w.Members.Any(m => m.RAppUserId == userId)).AnyAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// add member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="inviterId"></param>
        /// <param name="email"></param>
        /// <param name="notifyUser"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> InviteMemberAsync(Guid workspaceId, Guid inviterId, string email, bool notifyUser)
        {
            try
            {
                var ws = await _context.RWorkspaces.AsNoTracking().Where(w => w.Id == workspaceId).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false);//not found
                }
                var user = await _userManager.FindByEmailAsync(email);
                if(user == null)
                {
                    return new RServiceResult<bool>(false, "User not found");
                }
                if (true == await _context.RWSUsers.AsNoTracking().Where(u => u.RAppUserId == user.Id && u.RWorkspaceId == workspaceId).AnyAsync())
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

                _context.WorkspaceUserInvitations.Add
                    (
                    new WorkspaceUserInvitation()
                    {
                        UserId = user.Id,
                        WorkspaceId = workspaceId,
                    }
                    );
                await _context.SaveChangesAsync();

                if(notifyUser)
                {
                    await _notificationService.PushNotification(user.Id, $"Invitation to {ws.Name}", $"You have been invited to join workspace {ws.Name} by {(await _userManager.Users.AsNoTracking().Where(u => u.Id == inviterId).SingleAsync()).Email} ");
                }
                

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// revoke invitation
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> RevokeInvitationAsync(Guid workspaceId, Guid userId)
        {
            try
            {
                var invitation = await _context.WorkspaceUserInvitations.Where(i => i.UserId == userId && i.WorkspaceId == workspaceId).FirstOrDefaultAsync();
                if (invitation == null)
                {
                    return new RServiceResult<bool>(false, "User got not invitation.");
                }
                _context.Remove(invitation);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// user invitations
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<WorkspaceUserInvitationViewModel[]>> GetUserInvitationsAsync(Guid userId)
        {
            try
            {
                var invitations = await _context.WorkspaceUserInvitations.AsNoTracking().Include(i => i.Workspace).Where(i => i.UserId == userId).ToListAsync();
                List<WorkspaceUserInvitationViewModel> lst = new List<WorkspaceUserInvitationViewModel>();

                foreach (var invitation in invitations)
                {
                    lst.Add
                        (
                        new WorkspaceUserInvitationViewModel()
                        {
                            Id = invitation.Id,
                            Workspace = new WorkspaceViewModel()
                            {
                                Id = invitation.Workspace.Id,
                                Name = invitation.Workspace.Name,
                                Description = invitation.Workspace.Description,
                                CreateDate = invitation.Workspace.CreateDate,
                                Active = invitation.Workspace.Active,
                                WokspaceOrder = invitation.Workspace.WokspaceOrder,
                            }
                        }
                        );
                }
                return new RServiceResult<WorkspaceUserInvitationViewModel[]>(lst.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<WorkspaceUserInvitationViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// workspace invitations
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<WorkspaceUserInvitationViewModel[]>> GetWorkspaceInvitationsAsync(Guid workspaceId)
        {
            try
            {
                var invitations = await _context.WorkspaceUserInvitations.AsNoTracking().Include(w => w.Workspace).Where(i => i.WorkspaceId == workspaceId).ToListAsync();
                List<WorkspaceUserInvitationViewModel> lst = new List<WorkspaceUserInvitationViewModel>();

                foreach (var invitation in invitations)
                {
                    lst.Add
                        (
                        new WorkspaceUserInvitationViewModel()
                        {
                            Id = invitation.Id,
                            User = new PublicRAppUser()
                            {
                                Id = invitation.User.Id,
                                Username = invitation.User.UserName,
                                Email = invitation.User.Email,
                                FirstName = invitation.User.FirstName,
                                SureName = invitation.User.SureName,
                                PhoneNumber = invitation.User.PhoneNumber,
                                RImageId = invitation.User.RImageId,
                                Status = invitation.User.Status,
                                NickName = invitation.User.NickName,
                                Website = invitation.User.Website,
                                Bio = invitation.User.Bio,
                                EmailConfirmed = invitation.User.EmailConfirmed
                            },
                        }
                        );
                }
                return new RServiceResult<WorkspaceUserInvitationViewModel[]>(lst.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<WorkspaceUserInvitationViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// delete member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> DeleteMemberAsync(Guid workspaceId, Guid userId)
        {
            try
            {
                var ws = await _context.RWorkspaces.Where(w => w.Id == workspaceId).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false);//not found
                }
                var member = await _context.RWSUsers.Where(u => u.RWorkspaceId == workspaceId && u.RAppUserId == userId).SingleOrDefaultAsync();
                if (member == null)
                {
                    return new RServiceResult<bool>(false, "User is not a member.");
                }

                _context.Remove(member);
                _context.Update(ws);
                var roles = await _context.RWSUserRoles.Where(r => r.UserId == userId && r.WorkspaceId == workspaceId).ToListAsync();
                if (roles.Any())
                {
                    _context.RemoveRange(roles);
                }
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
                var ws = await _context.RWorkspaces.AsNoTracking().Where(w => w.Id == workspaceId).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false);//not found
                }
                var member = await _context.RWSUsers.Where(u => u.RWorkspaceId == workspaceId && u.RAppUserId == userId).SingleOrDefaultAsync();
                if (member == null)
                {
                    return new RServiceResult<bool>(false, "User is not a member.");
                }

                _context.Remove(member);

                var roles = await _context.RWSUserRoles.Where(r => r.UserId == userId && r.WorkspaceId == workspaceId).ToListAsync();
                if (roles.Any())
                {
                    _context.RemoveRange(roles);
                }
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
                var ws = await _context.RWorkspaces.AsNoTracking().Where(w => w.Id == workspaceId).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false);//not found
                }
                var member = await _context.RWSUsers.Where(u => u.RWorkspaceId == workspaceId && u.RAppUserId == userId).SingleOrDefaultAsync();
                if (member == null)
                {
                    return new RServiceResult<bool>(false, "User is not a member.");
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

                    var admin = await _context.RWSUsers.AsNoTracking().Where(u => u.RWorkspaceId == workspaceId && u.RAppUserId == ownerOrModeratorId).SingleOrDefaultAsync();

                    if (admin.Status != RWSUserMembershipStatus.Owner)
                    {
                        return new RServiceResult<bool>(false, "User has not enough privileges to perform this operation.");
                    }
                }

                if(member.Status == RWSUserMembershipStatus.Owner)
                {
                    if(false == await _context.RWSUsers.Where(u => u.RWorkspaceId == workspaceId && u.Status == RWSUserMembershipStatus.Owner && u.RAppUserId != userId).AnyAsync())
                    {
                        return new RServiceResult<bool>(false, "Workspace remains ownerless after this changes and it is not permitted.");
                    }
                }

                RWSUserMembershipStatus oldStatus = member.Status;

                member.Status = status;
                _context.Update(member);
                await _context.SaveChangesAsync();

                if(
                    oldStatus != RWSUserMembershipStatus.Owner
                    &&
                    status == RWSUserMembershipStatus.Owner
                    )
                {
                    await AddUserToRoleInWorkspaceAsync(workspaceId, userId, _rolesService.AdministratorRoleName);
                }

                if (
                    status != RWSUserMembershipStatus.Owner
                    &&
                    oldStatus == RWSUserMembershipStatus.Owner
                    )
                {
                    await RemoveUserFromRoleInWorkspaceAsync(workspaceId, userId, _rolesService.AdministratorRoleName);
                }

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
                var ws = await _context.RWorkspaces.AsNoTracking().Where(w => w.Id == workspaceId).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false);//not found
                }
                var user = await _userManager.Users.AsNoTracking().Where(u => u.Id == userId).SingleAsync();
                var invitation = await _context.WorkspaceUserInvitations.Where(i => i.UserId == userId && i.WorkspaceId == ws.Id).FirstOrDefaultAsync();
                if (invitation == null)
                {
                    return new RServiceResult<bool>(false, "User got not invitation.");
                }
                _context.Remove(invitation);
                if (!reject)
                {
                    _context.RWSUsers.Add(
                        new RWSUser()
                        {
                            RWorkspaceId = workspaceId,
                            RAppUserId = userId,
                            Status = RWSUserMembershipStatus.Member
                        });

                }

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
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> AddUserToRoleInWorkspaceAsync(Guid workspaceId, Guid userId, string roleName)
        {
            try
            {
                var ws = await _context.RWorkspaces.AsNoTracking().Where(w => w.Id == workspaceId).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false, "Workspace not found.");
                }
                var member = await _context.RWSUsers.AsNoTracking().Where(u => u.RWorkspaceId == workspaceId && u.RAppUserId == userId).SingleOrDefaultAsync();
                if (member == null)
                {
                    return new RServiceResult<bool>(false, "User is not a member.");
                }

                var role = await _context.RWSRoles.AsNoTracking().Where(r => r.WorkspaceId == workspaceId && r.Name == roleName).SingleOrDefaultAsync();
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
        /// remove user from role in workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> RemoveUserFromRoleInWorkspaceAsync(Guid workspaceId, Guid userId, string roleName)
        {
            try
            {
                var ws = await _context.RWorkspaces.AsNoTracking().Where(w => w.Id == workspaceId).SingleOrDefaultAsync();
                if (ws == null)
                {
                    return new RServiceResult<bool>(false, "Workspace not found.");
                }
                
                var member = await _context.RWSUsers.AsNoTracking().Where(u => u.RWorkspaceId == workspaceId && u.RAppUserId == userId).SingleOrDefaultAsync();
                if (member == null)
                {
                    return new RServiceResult<bool>(false, "User is not a member.");
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
        /// is user in role in workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<RServiceResult<bool>> IsInRoleAsync(Guid workspaceId, Guid userId, string roleName)
        {
            try
            {
                var role = await _context.RWSRoles.AsNoTracking().Where(r => r.Name == roleName).SingleOrDefaultAsync();
                if (role == null)
                {
                    return new RServiceResult<bool>(false, "Role not foune");
                }

                return new RServiceResult<bool>(await _context.RWSUserRoles.Where(r => r.WorkspaceId == workspaceId && r.UserId == userId && r.RoleId == role.Id).AnyAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get user roles
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<IList<string>>> GetUserRoles(Guid workspaceId, Guid userId)
        {
            try
            {
                return new RServiceResult<IList<string>>(
                    await _context.RWSUserRoles.AsNoTracking()
                            .Include(r => r.Role).Where(u => u.WorkspaceId == workspaceId && u.UserId == userId)
                            .Select(r => r.Role.Name)
                            .ToListAsync()
                            );
            }
            catch (Exception exp)
            {
                return new RServiceResult<IList<string>>(null, exp.ToString());
            }

        }

        /// <summary>
        /// has permission
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> HasPermission(Guid workspaceId, Guid userId, string securableItemShortName, string operationShortName)
        {
            try
            {
                RServiceResult<IList<string>> roles = await GetUserRoles(workspaceId, userId);
                if (!string.IsNullOrEmpty(roles.ExceptionString))
                    return new RServiceResult<bool>(false, roles.ExceptionString);

                foreach (string role in roles.Result)
                {
                    RServiceResult<bool> hasPermission = await _rolesService.HasPermission(workspaceId, role, securableItemShortName, operationShortName);
                    if (!string.IsNullOrEmpty(hasPermission.ExceptionString))
                        return new RServiceResult<bool>(false, hasPermission.ExceptionString);
                    if (hasPermission.Result)
                    {
                        return new RServiceResult<bool>(true);
                    }
                }

                return
                    new RServiceResult<bool>
                    (
                        false
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
           
        }

        /// <summary>
        /// is admin
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> IsAdmin(Guid workspaceId, Guid userId)
        {
            return  await IsInRoleAsync(workspaceId, userId, _rolesService.AdministratorRoleName);
        }

        /// <summary>
        /// Lists user permissions
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<SecurableItem[]>> GetUserSecurableItemsStatus(Guid workspaceId, Guid userId)
        {
            SecurableItem[] securableItems = _rolesService.GetSecurableItems();
            RServiceResult<IList<string>> roles = await GetUserRoles(workspaceId, userId);
            if (!string.IsNullOrEmpty(roles.ExceptionString))
                return new RServiceResult<SecurableItem[]>(null, roles.ExceptionString);

            bool isAdmin = (await IsAdmin(workspaceId, userId)).Result;

            foreach (SecurableItem securableItem in securableItems)
            {
                foreach (SecurableItemOperation operation in securableItem.Operations)
                {
                    foreach (string role in roles.Result)
                    {
                        RServiceResult<bool> hasPermission = await _rolesService.HasPermission(workspaceId, role, securableItem.ShortName, operation.ShortName);
                        if (!string.IsNullOrEmpty(hasPermission.ExceptionString))
                            return new RServiceResult<SecurableItem[]>(null, hasPermission.ExceptionString);
                        if (isAdmin || hasPermission.Result)
                        {
                            operation.Status = true;
                        }
                    }
                }
            }
            return new RServiceResult<SecurableItem[]>(securableItems);
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
        /// restrict worpspace members query
        /// </summary>
        public virtual bool RestrictWorkspaceMembersQueryToAuthorizarion
        {
            get { return false; }
        }

        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RSecurityDbContext<RAppUser, RAppRole, Guid> _context;

        /// <summary>
        /// Workspace roles servoce
        /// </summary>
        public IWorkspaceRolesService _rolesService { get; set; }

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
        /// <param name="rolesService"></param>
        /// <param name="userManager"></param>
        /// <param name="optionsService"></param>
        /// <param name="notificationService"></param>
        public WorkspaceService(RSecurityDbContext<RAppUser, RAppRole, Guid> context, IWorkspaceRolesService rolesService, UserManager<RAppUser> userManager, IRGenericOptionsService optionsService, IRNotificationService notificationService)
        {
            _context = context;
            _rolesService = rolesService;
            _userManager = userManager;
            _optionsService = optionsService;
            _notificationService = notificationService;
        }
    }
}
