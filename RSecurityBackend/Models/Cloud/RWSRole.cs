using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;


namespace RSecurityBackend.Models.Cloud
{
    /// <summary>
    /// Workspace Role
    /// </summary>
    public class RWSRole : IdentityRole<Guid>
    {
        /// <summary>
        /// default constructor
        /// </summary>
        public RWSRole() : base()
        {

        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="roleName"></param>
        public RWSRole(string roleName) : base(roleName)
        {
        }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Permissions
        /// </summary>
        public ICollection<RWSPermission> Permissions { get; set; }
    }
}
