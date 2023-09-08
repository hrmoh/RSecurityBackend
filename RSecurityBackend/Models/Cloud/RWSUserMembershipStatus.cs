﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSecurityBackend.Models.Cloud
{
    /// <summary>
    /// User Memnership status
    /// </summary>
    public enum RWSUserMembershipStatus
    {
        /// <summary>
        /// Owner
        /// </summary>
        Owner = 0,

        /// <summary>
        /// Member
        /// </summary>
        Member = 1,

        /// <summary>
        /// Invited
        /// </summary>
        Invited = 2,
    }
}
