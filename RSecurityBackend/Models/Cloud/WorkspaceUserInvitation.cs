using RSecurityBackend.Models.Auth.Db;
using System;

namespace RSecurityBackend.Models.Cloud
{
    /// <summary>
    /// workspace user invitation
    /// </summary>
    public class WorkspaceUserInvitation
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// user id
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// user
        /// </summary>
        public virtual RAppUser User { get; set; }

        /// <summary>
        /// workspace id
        /// </summary>
        public Guid? WorkspaceId { get; set; }

        /// <summary>
        /// workspace
        /// </summary>
        public virtual RWorkspace Workspace { get; set; }
    }
}
