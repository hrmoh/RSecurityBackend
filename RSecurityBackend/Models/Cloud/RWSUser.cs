using RSecurityBackend.Models.Auth.Db;
using System;

namespace RSecurityBackend.Models.Cloud
{
    /// <summary>
    /// Workspace User
    /// </summary>
    public class RWSUser
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User Id
        /// </summary>
        public Guid? RAppUserId { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public virtual RAppUser RAppUser { get; set; }

        /// <summary>
        /// status
        /// </summary>
        public RWSUserMembershipStatus Status { get; set; }

        /// <summary>
        /// invite date
        /// </summary>
        public DateTime InviteDate { get; set; }

        /// <summary>
        /// member from
        /// </summary>
        public DateTime? MemberFrom { get; set; }

        /// <summary>
        /// workspace id
        /// </summary>
        public Guid? RWorkspaceId { get; set; }

        /// <summary>
        /// workspace order for user
        /// </summary>
        public int WorkspaceOrder { get; set; }
    }
}
