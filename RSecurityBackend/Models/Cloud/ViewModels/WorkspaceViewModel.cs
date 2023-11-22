using System;

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
        public int WorkspaceOrder { get; set; }

        /// <summary>
        /// worspace create date
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// am I the owner?
        /// </summary>
        public bool Owned { get; set; }
    }
}
