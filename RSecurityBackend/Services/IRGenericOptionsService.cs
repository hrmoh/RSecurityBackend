using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using System;
using System.Threading.Tasks;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// generic options service
    /// </summary>
    public interface IRGenericOptionsService
    {
        /// <summary>
        /// modify an option or add a new one if an option with the requested name does not exist
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<RGenericOption>> SetAsync(string optionName, string optionValue, Guid? userId);

        /// <summary>
        /// get option value, if option value is not found it returns empty string
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<string>> GetValueAsync(string optionName, Guid? userId);

        /// <summary>
        /// get option value using context
        /// </summary>
        /// <param name="context"></param>
        /// <param name="optionName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<string>> GetValueAsync(RSecurityDbContext<RAppUser, RAppRole, Guid> context, string optionName, Guid? userId);
    }
}
