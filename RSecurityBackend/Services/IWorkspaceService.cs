using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Cloud;
using RSecurityBackend.Models.Cloud.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// Workspace service
    /// </summary>
    public interface IWorkspaceService
    {
        /// <summary>
        /// add workspace
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="isPublic"></param>
        /// <returns></returns>
        Task<RServiceResult<WorkspaceViewModel>> AddWorkpspaceAsync(Guid userId, string name, string description, bool isPublic);

        /// <summary>
        /// Update workspace
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpdateWorkpspaceAsync(Guid userId, WorkspaceViewModel model);

        /// <summary>
        /// delete workspace
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteWorkspaceAsync(Guid userId, Guid id);

        /// <summary>
        /// get owner workspaces
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="onlyActive"></param>
        /// <returns></returns>
        Task<RServiceResult<WorkspaceViewModel[]>> GetOwnedWorkspacesAsync(Guid userId, bool onlyActive);

        /// <summary>
        /// member workspaces
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="onlyActive"></param>
        /// <returns></returns>
        Task<RServiceResult<WorkspaceViewModel[]>> GetMemberWorkspacesAsync(Guid userId, bool onlyActive);

        /// <summary>
        /// get workspace by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<WorkspaceViewModel>> GetWorkspaceByIdAsync(Guid id);

        /// <summary>
        /// get user workspace information
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<WorkspaceViewModel>> GetUserWorkspaceByIdAsync(Guid id, Guid userId);

        /// <summary>
        /// is user workspace member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> IsUserWorkspaceMember(Guid workspaceId, Guid userId);

        /// <summary>
        /// add member (does not send any email)
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerOrModeratorId"></param>
        /// <param name="email"></param>
        /// <param name="notifyUser"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> InviteMemberAsync(Guid workspaceId, Guid ownerOrModeratorId, string email, bool notifyUser);

        /// <summary>
        /// delete member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerOrModeratorId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteMemberAsync(Guid workspaceId, Guid ownerOrModeratorId, Guid userId);

        /// <summary>
        /// leave a workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> LeaveWorkspaceAsync(Guid workspaceId, Guid userId);

        /// <summary>
        /// process workspace invitation
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="reject"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ProcessWorkspaceInvitationAsync(Guid workspaceId, Guid userId, bool reject);

        /// <summary>
        /// change member status
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerOrModeratorId"></param>
        /// <param name="userId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ChangeMemberStatusAsync(Guid workspaceId, Guid ownerOrModeratorId, Guid userId, RWSUserMembershipStatus status);

        /// <summary>
        /// add user to role in a workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerOrModeratorId"></param>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> AddUserToRoleInWorkspaceAsync(Guid workspaceId, Guid ownerOrModeratorId, Guid userId, string roleName);

        /// <summary>
        /// remove user from role i
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RemoveUserFromRoleInWorkspaceAsync(Guid workspaceId, Guid userId, string roleName);

        /// <summary>
        /// is user in role in workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        Task<RServiceResult<bool>> IsInRoleAsync(Guid workspaceId, Guid userId, string roleName);

        /// <summary>
        /// is admin
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> IsAdmin(Guid workspaceId, Guid userId);

        /// <summary>
        /// get user roles
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<IList<string>>> GetUserRoles(Guid workspaceId, Guid userId);

        /// <summary>
        /// has permission
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> HasPermission(Guid workspaceId, Guid userId, string securableItemShortName, string operationShortName);

        /// <summary>
        /// Lists user permissions
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<SecurableItem[]>> GetUserSecurableItemsStatus(Guid workspaceId, Guid userId);

        /// <summary>
        /// restrict workspace adding
        /// </summary>
        bool RestrictWorkspaceCreationToAuthorizarion { get; }

        /// <summary>
        /// allow inviting users to workspaces by default
        /// </summary>
        bool AllowInvitingUsersToWorkspacesByDefault { get; }
    }
}
