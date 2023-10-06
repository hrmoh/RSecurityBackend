using Microsoft.EntityFrameworkCore;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Cloud;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Workspace Role Service implementation
    /// </summary>
    public class WorkspaceRolesServiceBase : IWorkspaceRolesService
    {
        /// <summary>
        /// Administrator role name
        /// </summary>
        public string AdministratorRoleName { get { return "Administrator"; } }

        /// <summary>
        /// returns all user roles
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RWSRole[]>> GetAllRoles(Guid workspaceId, string language)
        {
            try
            {
                return new RServiceResult<RWSRole[]>(await _context.RWSRoles.AsNoTracking().Where(r => r.WorkspaceId == workspaceId).Include(r => r.Permissions).ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<RWSRole[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// find role by name
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="name"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public async Task<RWSRole> FindByNameAsync(Guid workspaceId, string name, string language)
        {
            return await _context.RWSRoles.AsNoTracking().Where(r => r.WorkspaceId == workspaceId && r.Name == name).FirstOrDefaultAsync();
        }


        /// <summary>
        /// returns user role information
        /// </summary>       
        /// <param name="roleName"></param>        
        /// <param name="workspaceId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RWSRole>> GetRoleInformation( Guid workspaceId, string roleName, string language)
        {
            try
            {
                return new RServiceResult<RWSRole>(await FindByNameAsync(workspaceId, roleName, language));
            }
            catch (Exception exp)
            {
                return new RServiceResult<RWSRole>(null, exp.ToString());
            }

        }


        /// <summary>
        /// modify existing user role
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="roleName"></param>
        /// <param name="updateRoleInfo"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ModifyRole(Guid workspaceId, string roleName, RWSRole updateRoleInfo, string language)
        {
            RWSRole existingInfo = await _context.RWSRoles.Where(r => r.WorkspaceId == workspaceId && r.Name == roleName).FirstOrDefaultAsync();
            if (existingInfo == null)
            {
                return new RServiceResult<bool>(false, "role not found");
            }
            if (existingInfo.Name != updateRoleInfo.Name)
            {

                RWSRole anotherWithSameName = await _context.RWSRoles.AsNoTracking().Where(g => g.WorkspaceId == workspaceId && g.Name == updateRoleInfo.Name && g.Id != existingInfo.Id).SingleOrDefaultAsync();

                if (anotherWithSameName != null)
                {
                    return new RServiceResult<bool>(false, "duplicated role name");
                }

                existingInfo.Name = updateRoleInfo.Name;
            }
            existingInfo.Description = updateRoleInfo.Description;
            _context.Update(existingInfo);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// delete user role
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="roleName"></param>
        /// <param name="language"></param>
        /// <returns>true if succeeds</returns>
        public async Task<RServiceResult<bool>> DeleteRole(Guid workspaceId, string roleName, string language)
        {
            RWSRole existingInfo = await _context.RWSRoles.Where(r => r.WorkspaceId == workspaceId && r.Name == roleName).FirstOrDefaultAsync();
            if (existingInfo != null)
            {
                _context.Remove(existingInfo);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            return new RServiceResult<bool>(false, "role not found.");
        }

        /// <summary>
        /// adds a new user role
        /// </summary>
        /// <param name="newRoleInfo">new role info</param>
        /// <param name="language"></param>
        /// <returns>update user role info (id)</returns>
        public async Task<RServiceResult<RWSRole>> AddRole(RWSRole newRoleInfo, string language)
        {
            RWSRole existingRole = await FindByNameAsync((Guid)newRoleInfo.WorkspaceId, newRoleInfo.Name, language);
            if (existingRole != null)
            {
                return new RServiceResult<RWSRole>(null, "Role name is in use");
            }
            _context.Add(newRoleInfo);
            await _context.SaveChangesAsync();
            return new RServiceResult<RWSRole>(newRoleInfo);
        }

        /// <summary>
        /// Has role specified permission
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="roleName"></param>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> HasPermission(Guid workspaceId, string roleName, string securableItemShortName, string operationShortName, string language)
        {
            RWSRole roleByName = await FindByNameAsync(workspaceId, roleName, language);
            if (roleByName == null)
            {
                return new RServiceResult<bool>(false, "role not found");
            }

            RWSRole role = await _context.RWSRoles.AsNoTracking().Include(g => g.Permissions)
                .Where(g => g.Id == roleByName.Id)
                .SingleOrDefaultAsync();

            return
                new RServiceResult<bool>(
                role.Permissions.Where(p => p.SecurableItemShortName == securableItemShortName && p.OperationShortName == operationShortName)
                .SingleOrDefault() != null
                );

        }

        /// <summary>
        /// roles having specific permission
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RWSRole[]>> GetRolesHavingPermission(Guid workspaceId, string securableItemShortName, string operationShortName, string language)
        {
            RWSRole[] rolesInfo = await _context.RWSRoles.AsNoTracking()
                                                        .Include(r => r.Permissions)
                                                        .Where(r => r.WorkspaceId == workspaceId && (r.Name == AdministratorRoleName || r.Permissions.Any(p => p.SecurableItemShortName == securableItemShortName && p.OperationShortName == operationShortName)))
                                                        .ToArrayAsync();
            return new RServiceResult<RWSRole[]>(rolesInfo);
        }

        /// <summary>
        /// gets list of SecurableItem, should be reimplemented in end user applications
        /// </summary>
        /// <returns></returns>
        public virtual SecurableItem[] GetSecurableItems()
        {
            return SecurableItem.WorkspaceItems;
        }

        /// <summary>
        /// Lists role permissions
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="roleName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public async Task<RServiceResult<SecurableItem[]>> GetRoleSecurableItemsStatus(Guid workspaceId, string roleName, string language)
        {
            RWSRole roleByName = await FindByNameAsync(workspaceId, roleName, language);
            if (roleByName == null)
            {
                return new RServiceResult<SecurableItem[]>(null, "role not found");
            }
            RWSRole role = await _context.RWSRoles.AsNoTracking().Include(g => g.Permissions).Where(g => g.Id == roleByName.Id).SingleOrDefaultAsync();
            List<SecurableItem> securableItems = new List<SecurableItem>();
            foreach (SecurableItem templateItem in GetSecurableItems())
            {
                SecurableItem item = new SecurableItem()
                {
                    ShortName = templateItem.ShortName,
                    Description = templateItem.Description
                };
                List<SecurableItemOperation> operations = new List<SecurableItemOperation>();
                foreach (SecurableItemOperation operation in templateItem.Operations)
                {
                    operations.Add(
                        new SecurableItemOperation()
                        {
                            ShortName = operation.ShortName,
                            Description = operation.Description,
                            Prerequisites = operation.Prerequisites,
                            Status = role.Permissions.Where(p => p.SecurableItemShortName == templateItem.ShortName && p.OperationShortName == operation.ShortName).SingleOrDefault() != null
                        }
                        );
                }
                item.Operations = operations.ToArray();
                securableItems.Add(item);
            }
            return new RServiceResult<SecurableItem[]>(securableItems.ToArray());
        }

        /// <summary>
        /// Saves role permissions
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="roleName"></param>
        /// <param name="securableItems"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SetRoleSecurableItemsStatus(Guid workspaceId, string roleName, SecurableItem[] securableItems, string language)
        {
            RWSRole roleByName = await FindByNameAsync(workspaceId, roleName, language);
            if (roleByName == null)
            {
                return new RServiceResult<bool>(false, "role not found");
            }
            RWSRole role = await _context.RWSRoles.Include(g => g.Permissions).Where(g => g.Id == roleByName.Id).SingleOrDefaultAsync();
            role.Permissions.Clear();
            _context.Update(role);
            await _context.SaveChangesAsync();
            List<RWSPermission> newPermissionSet = new List<RWSPermission>();
            foreach (SecurableItem securableItem in securableItems)
            {
                foreach (SecurableItemOperation operation in securableItem.Operations)
                {
                    if (operation.Status)
                    {
                        newPermissionSet.Add(new RWSPermission()
                        {
                            SecurableItemShortName = securableItem.ShortName,
                            OperationShortName = operation.ShortName
                        });
                    }
                }
            }
            role.Permissions = newPermissionSet;
            _context.Update(role);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RSecurityDbContext<RAppUser, RAppRole, Guid> _context;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        public WorkspaceRolesServiceBase(RSecurityDbContext<RAppUser, RAppRole, Guid> context)
        {
            _context = context;
        }
    }
}
