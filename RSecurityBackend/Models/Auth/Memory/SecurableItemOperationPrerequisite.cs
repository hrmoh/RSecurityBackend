
namespace RSecurityBackend.Models.Auth.Memory
{
    /// <summary>
    /// SecurableItemOperation Prerequisite
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    /// <param name="secureItemShortName"></param>
    /// <param name="operationShortName"></param>
    public class SecurableItemOperationPrerequisite(string secureItemShortName, string operationShortName)
    {
        /// <summary>
        /// Prerequisite SecureItem ShortName
        /// </summary>
        /// <example>job</example>
        public string SecureItemShortName { get; set; } = secureItemShortName;

        /// <summary>
        /// Prerequisite Operation ShortName
        /// </summary>
        /// <example>view</example>
        public string OperationShortName { get; set; } = operationShortName;
    }
}
