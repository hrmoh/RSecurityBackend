using RSecurityBackend.Models.Auth.Db;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RSecurityBackend.Models.Cloud
{
    /// <summary>
    /// Workspace (a reference for a set of records separated from each other, users may have access to different workspaces and have different roles in each of them)
    /// </summary>
    public class RWorkspace
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        [MinLength(1)]
        public string Name { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// order
        /// </summary>
        public int WorkspaceOrder { get; set; }

        /// <summary>
        /// worspace create date
        /// </summary>
        [Required]
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// users having access to the workspace
        /// </summary>
        public ICollection<RWSUser> Members { get; set; }

        /// <summary>
        /// active
        /// </summary>
        public bool Active { get; set; }
    }
}
