using RSecurityBackend.Models.Auth.Db;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RSecurityBackend.Models.Cloud.ViewModels
{
    /// <summary>
    /// Workspace view model
    /// </summary>
    public class WorkspaceViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// order
        /// </summary>
        public int WokspaceOrder { get; set; }

        /// <summary>
        /// worspace create date
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// users having access to the workspace
        /// </summary>
        public ICollection<RAppUser> Users { get; set; }

        /// <summary>
        /// every user has access to it (Users is ignored)
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// active
        /// </summary>
        public bool Active { get; set; }
    }
}
