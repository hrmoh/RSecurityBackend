using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Services.Implementation;
using SampleProject.Models.Auth.Memory;

namespace SampleProject.Services.Implementation
{
    public class WorkspaceRolesService : WorkspaceRolesServiceBase
    {
        public WorkspaceRolesService(RSecurityDbContext<RAppUser, RAppRole, Guid> context) : base(context)
        {
        }

        /// <summary>
        /// gets list of SecurableItem, should be reimplemented in the end user applications
        /// </summary>
        /// <returns></returns>
        public override SecurableItem[] GetSecurableItems()
        {
            return RSecurableItem.WorkspaceItems;
        }

    }
}
