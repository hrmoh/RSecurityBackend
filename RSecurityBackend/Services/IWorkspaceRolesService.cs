using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Cloud;
using RSecurityBackend.Models.Generic;
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
        /// <returns></returns>
        Task<RServiceResult<RWSRole[]>> GetAllRoles();

        /// <summary>
        /// find role by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<RWSRole> FindByNameAsync(string name);


        /// <summary>
        /// returns user role information
        /// </summary>       
        /// <param name="roleName"></param>        
        /// <returns></returns>
        Task<RServiceResult<RWSRole>> GetRoleInformation(string roleName);

        /// <summary>
        /// modify existing user role
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="updateRoleInfo"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ModifyRole(string roleName, RWSRole updateRoleInfo);

        /// <summary>
        /// delete user role
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns>true if succeeds</returns>
        Task<RServiceResult<bool>> DeleteRole(string roleName);

        /// <summary>
        /// adds a new user role
        /// </summary>
        /// <param name="newRoleInfo">new role info</param>
        /// <returns>update user role info (id)</returns>
        Task<RServiceResult<RWSRole>> AddRole(RWSRole newRoleInfo);

        /// <summary>
        /// Has role specified permission
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> HasPermission(string roleName, string securableItemShortName, string operationShortName);

        /// <summary>
        /// roles having specific permission
        /// </summary>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <returns></returns>
        Task<RServiceResult<RWSRole[]>> GetRolesHavingPermission(string securableItemShortName, string operationShortName);

        /// <summary>
        /// gets list of SecurableItem, should be reimplemented in end user applications
        /// </summary>
        /// <returns></returns>
        SecurableItem[] GetSecurableItems();

        /// <summary>
        /// Lists role permissions
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        Task<RServiceResult<SecurableItem[]>> GetRoleSecurableItemsStatus(string roleName);

        /// <summary>
        /// Saves role permissions
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="securableItems"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SetRoleSecurableItemsStatus(string roleName, SecurableItem[] securableItems);
    }
}
