using Microsoft.AspNetCore.Identity;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Services.Implementation;
using SampleProject.Models.Auth.Memory;

namespace SampleProject.Services.Implementation
{
    /// <summary>
    /// Role Service Implementation
    /// </summary>
    public class RoleService : RoleServiceBase
    {
        /// <summary>
        /// gets list of SecurableItem, should be reimplemented in the end user applications
        /// </summary>
        /// <returns></returns>
        public override SecurableItem[] GetSecurableItems()
        {
            return RSecurableItem.Items;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="roleManager"></param>       
        public RoleService(
            RoleManager<RAppRole> roleManager
            ) : base(roleManager)
        {
        }

    }
}
