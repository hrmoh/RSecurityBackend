using RSecurityBackend.Models.Cloud;
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
        Task<RServiceResult<RWorkspace>> AddWorkpspaceAsync(Guid userId, string name, string description, bool isPublic);

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
        Task<RServiceResult<RWorkspace[]>> GetOwnedWorkspacesAsync(Guid userId, bool onlyActive);

        /// <summary>
        /// member workspaces
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="onlyActive"></param>
        /// <returns></returns>
        Task<RServiceResult<RWorkspace[]>> GetMemberWorkspacesAsync(Guid userId, bool onlyActive);

        /// <summary>
        /// get workspace by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<RWorkspace>> GetWorkspaceByIdAsync(Guid id);

        /// <summary>
        /// add member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> AddMemberAsync(Guid workspaceId, Guid ownerId, Guid userId);

        /// <summary>
        /// delete member
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="ownerId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteMemberAsync(Guid workspaceId, Guid ownerId, Guid userId);
    }
}
