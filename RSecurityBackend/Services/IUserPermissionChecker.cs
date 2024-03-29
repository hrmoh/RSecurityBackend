﻿using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// Permission checker service
    /// </summary>
    public interface IUserPermissionChecker
    {
        /// <summary>
        /// check to see if user has permission to do operation
        /// </summary>
        /// <param name="userId">userId</param>
        /// <param name="sessionId">sessionId</param>
        /// <param name="language"></param>
        /// <param name="securableItemShortName">form</param>
        /// <param name="operationShortName">operation</param>
        /// <param name="workspaceId"></param>
        /// <returns>true if has permission</returns>
        Task<RServiceResult<bool>> Check(Guid userId, Guid sessionId, string language, string securableItemShortName, string operationShortName, Guid? workspaceId = null);
    }
}
