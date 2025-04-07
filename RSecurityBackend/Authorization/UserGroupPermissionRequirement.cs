using Microsoft.AspNetCore.Authorization;

namespace RSecurityBackend.Authorization
{
    /// <summary>
    /// UserGroupPermissionRequirement
    /// </summary>
    /// <remarks>
    /// constructor
    /// </remarks>
    /// <param name="securableItemShortName"></param>
    /// <param name="operationShortName"></param>
    public class UserGroupPermissionRequirement(string securableItemShortName, string operationShortName) : IAuthorizationRequirement
    {
        /// <summary>
        ///
        /// </summary>
        /// <see cref="RSecurityBackend.Models.Auth.Memory.SecurableItem.ShortName"/>
        /// <example>job</example>
        public string SecurableItemShortName { get; set; } = securableItemShortName;

        /// <summary>
        /// 
        /// </summary>
        /// <see cref="RSecurityBackend.Models.Auth.Memory.SecurableItemOperation.ShortName"/>
        /// <example>view</example>
        public string OperationShortName { get; set; } = operationShortName;
    }
}
