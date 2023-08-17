using RSecurityBackend.Models.Auth.Db;
using System;
using System.ComponentModel.DataAnnotations;

namespace RSecurityBackend.Models.Cloud
{
    /// <summary>
    /// Workspace (a reference for a set of records separated from each other, users may have access to different workspaces and have different roles in each of them)
    /// </summary>
    public class RAppWorkspace
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
        public string Descrpition { get; set; }

        /// <summary>
        /// worspace create date
        /// </summary>
        [Required]
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// owner id
        /// </summary>
        public Guid? OwnerId { get; set; }

        /// <summary>
        /// owner
        /// </summary>
        public virtual RAppUser Owner { get; set; }
    }
}
