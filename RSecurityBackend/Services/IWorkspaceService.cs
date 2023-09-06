using RSecurityBackend.Models.Cloud;
using RSecurityBackend.Models.Cloud.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
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
        Task<RServiceResult<bool>> UpdateWorkpspaceAsync(Guid userId, RWorkspace model);

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
        /// add member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerOrModeratorId"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> AddMemberByEmailAsync(Guid workspaceId, Guid ownerOrModeratorId, string email);

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
        /// restrict workspace adding
        /// </summary>
        bool RestrictWorkspaceCreationToAuthorizarion { get; }
    }
}
