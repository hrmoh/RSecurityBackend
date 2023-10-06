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
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<WorkspaceViewModel>> AddWorkpspaceAsync(Guid userId, string name, string description, string language);

        /// <summary>
        /// Update workspace
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="language"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpdateWorkpspaceAsync(Guid userId, string language, WorkspaceViewModel model);

        /// <summary>
        /// delete workspace
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="id"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteWorkspaceAsync(Guid userId, string language, Guid id);

        /// <summary>
        /// member workspaces
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="onlyActive"></param>
        /// <param name="onlyOwned"></param>
        /// <param name="onlyMember"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<WorkspaceViewModel[]>> GetMemberWorkspacesAsync(Guid userId, string language, bool onlyActive, bool onlyOwned, bool onlyMember);

        /// <summary>
        /// get workspace by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<WorkspaceViewModel>> GetWorkspaceByIdAsync(Guid id, string language);

        /// <summary>
        /// get user workspace information
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<WorkspaceViewModel>> GetUserWorkspaceByIdAsync(Guid id, Guid userId, string language);

        /// <summary>
        /// get workspace members
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<RWSUserViewModel[]>> GetWorkspaceMembersAsync(Guid workspaceId, string language);

        /// <summary>
        /// is user workspace member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> IsUserWorkspaceMember(Guid workspaceId, Guid userId, string language);

        /// <summary>
        /// add member (does not send any email)
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="inviterId"></param>
        /// <param name="email"></param>
        /// <param name="notifyUser"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> InviteMemberAsync(Guid workspaceId, Guid inviterId, string email, bool notifyUser, string language);

        /// <summary>
        /// revoke invitation
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RevokeInvitationAsync(Guid workspaceId, Guid userId, string language);

        /// <summary>
        /// user invitations
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<WorkspaceUserInvitationViewModel[]>> GetUserInvitationsAsync(Guid userId, string language);

        /// <summary>
        /// workspace invitations
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<WorkspaceUserInvitationViewModel[]>> GetWorkspaceInvitationsAsync(Guid workspaceId, string language);

        /// <summary>
        /// delete member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteMemberAsync(Guid workspaceId, Guid userId, string language);

        /// <summary>
        /// leave a workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> LeaveWorkspaceAsync(Guid workspaceId, Guid userId, string language);

        /// <summary>
        /// process workspace invitation
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="reject"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ProcessWorkspaceInvitationAsync(Guid workspaceId, Guid userId, bool reject, string language);

        /// <summary>
        /// change member status
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerOrModeratorId"></param>
        /// <param name="userId"></param>
        /// <param name="status"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ChangeMemberStatusAsync(Guid workspaceId, Guid ownerOrModeratorId, Guid userId, RWSUserMembershipStatus status, string language);

        /// <summary>
        /// add user to role in a workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> AddUserToRoleInWorkspaceAsync(Guid workspaceId, Guid userId, string roleName, string language);

        /// <summary>
        /// remove user from role i
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RemoveUserFromRoleInWorkspaceAsync(Guid workspaceId, Guid userId, string roleName, string language);

        /// <summary>
        /// is user in role in workspace
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        Task<RServiceResult<bool>> IsInRoleAsync(Guid workspaceId, Guid userId, string roleName, string language);

        /// <summary>
        /// is admin
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> IsAdmin(Guid workspaceId, Guid userId, string language);

        /// <summary>
        /// get user roles
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<IList<string>>> GetUserRoles(Guid workspaceId, Guid userId, string language);

        /// <summary>
        /// has permission
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> HasPermission(Guid workspaceId, Guid userId, string securableItemShortName, string operationShortName, string language);

        /// <summary>
        /// Lists user permissions
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="userId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<SecurableItem[]>> GetUserSecurableItemsStatus(Guid workspaceId, Guid userId, string language);

        /// <summary>
        /// restrict workspace adding
        /// </summary>
        bool RestrictWorkspaceCreationToAuthorizarion { get; }

        /// <summary>
        /// allow inviting users to workspaces by default
        /// </summary>
        bool AllowInvitingUsersToWorkspacesByDefault { get; }

        /// <summary>
        /// restrict worpspace members query
        /// </summary>
        bool RestrictWorkspaceMembersQueryToAuthorizarion { get; }
    }
}
