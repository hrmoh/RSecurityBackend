using RSecurityBackend.Models.Auth.ViewModels;
using System;

namespace RSecurityBackend.Models.Cloud.ViewModels
{
    /// <summary>
    /// workspace user invitation
    /// </summary>
    public class WorkspaceUserInvitationViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public PublicRAppUser User { get; set; }

        /// <summary>
        /// Workspace
        /// </summary>
        public WorkspaceViewModel Workspace { get; set; }
    }
}
