﻿using System;

namespace RSecurityBackend.Models.Cloud
{
    /// <summary>
    /// Workspace Permission line
    /// </summary>
    /// <see cref="RSecurityBackend.Models.Auth.Memory.SecurableItem"/>
    public class RWSPermission
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// SecurableItem short name
        /// </summary>
        /// <example>
        /// job
        /// </example>
        public string SecurableItemShortName { get; set; }

        /// <summary>
        /// Operation short name
        /// </summary>
        /// <example>
        /// view
        /// </example>
        public string OperationShortName { get; set; }
    }
}
