﻿using RSecurityBackend.Models.Auth.Db;
using System;

namespace RSecurityBackend.Models.Cloud
{
    /// <summary>
    /// User Workspace Role
    /// </summary>
    public class RWSUserRole
    {
        /// <summary>
        /// record id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// workspace id
        /// </summary>
        public Guid? WorkspaceId { get; set; }

        /// <summary>
        /// workspace
        /// </summary>
        public virtual RWorkspace Workspace { get; set; }

        /// <summary>
        /// user id
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// user
        /// </summary>
        public virtual RAppUser User { get; set; }

        /// <summary>
        /// role id
        /// </summary>
        public Guid? RoleId { get; set; }

        /// <summary>
        /// role
        /// </summary>
        public virtual RWSRole Role { get; set; }
    }
}
