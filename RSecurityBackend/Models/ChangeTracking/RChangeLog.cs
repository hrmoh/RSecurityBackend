using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Cloud;
using System;

namespace RSecurityBackend.Models.ChangeTracking
{
    /// <summary>
    /// change log model
    /// </summary>
    public class RChangeLog
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Workspace Id
        /// </summary>
        public Guid? WorkspaceId { get; set; }

        /// <summary>
        /// Workspace
        /// </summary>
        public virtual RWorkspace Workspace { get; set; }

        /// <summary>
        /// Entity
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Operation
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// user id
        /// </summary>
        public Guid? RAppUserId { get; set; }

        /// <summary>
        /// user
        /// </summary>
        public virtual RAppUser RAppUser { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Entity Json
        /// </summary>
        public string EntityJson { get; set; }

        /// <summary>
        /// entity guid
        /// </summary>
        public Guid? EntityUId { get; set; }

        /// <summary>
        /// entity id
        /// </summary>
        public int? EntityId { get; set; }

    }
}
