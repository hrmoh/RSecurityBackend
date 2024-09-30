using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Cloud;
using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// Workspace Role Service
    /// </summary>
    public interface IWorkspaceRolesService
    {
        /// <summary>
        /// Administrator role name
        /// </summary>
        public string AdministratorRoleName { get; }

        /// <summary>
        /// returns all user roles
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<RWSRole[]>> GetAllRoles(Guid workspaceId, string language);

        /// <summary>
        /// find role by name
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="name"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RWSRole> FindByNameAsync(Guid workspaceId, string name, string language);


        /// <summary>
        /// returns user role information
        /// </summary>       
        /// <param name="workspaceId"></param>
        /// <param name="roleName"></param>        
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<RWSRole>> GetRoleInformation(Guid workspaceId, string roleName, string language);

        /// <summary>
        /// modify existing user role
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="roleName"></param>
        /// <param name="updateRoleInfo"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ModifyRole(Guid workspaceId, string roleName, RWSRole updateRoleInfo, string language);

        /// <summary>
        /// delete user role
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="roleId"></param>
        /// <param name="language"></param>
        /// <returns>true if succeeds</returns>
        Task<RServiceResult<bool>> DeleteRole(Guid workspaceId, Guid roleId, string language);

        /// <summary>
        /// adds a new user role
        /// </summary>
        /// <param name="newRoleInfo">new role info</param>
        /// <param name="language"></param>
        /// <returns>update user role info (id)</returns>
        Task<RServiceResult<RWSRole>> AddRole(RWSRole newRoleInfo, string language);

        /// <summary>
        /// Has role specified permission
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="roleName"></param>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> HasPermission(Guid workspaceId, string roleName, string securableItemShortName, string operationShortName, string language);

        /// <summary>
        /// roles having specific permission
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<RWSRole[]>> GetRolesHavingPermission(Guid workspaceId, string securableItemShortName, string operationShortName, string language);

        /// <summary>
        /// gets list of SecurableItem, should be reimplemented in end user applications
        /// </summary>
        /// <returns></returns>
        SecurableItem[] GetSecurableItems();

        /// <summary>
        /// Lists role permissions
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="roleName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<SecurableItem[]>> GetRoleSecurableItemsStatus(Guid workspaceId, string roleName, string language);

        /// <summary>
        /// Saves role permissions
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="roleName"></param>
        /// <param name="securableItems"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SetRoleSecurableItemsStatus(Guid workspaceId, string roleName, SecurableItem[] securableItems, string language);
    }
}
