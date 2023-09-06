using RSecurityBackend.Models.Auth.ViewModels;
using System;

namespace RSecurityBackend.Models.Cloud.ViewModels
{
    /// <summary>
    ///  Workspace User View Model
    /// </summary>
    public class RWSUserViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public PublicRAppUser RAppUser { get; set; }

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
    }
}
