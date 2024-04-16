﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Models.Notification;
using RSecurityBackend.Models.Notification.ViewModels;
using RSecurityBackend.Services;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;

namespace RSecurityBackend.Controllers
{
    /// <summary>
    /// Notifications controller
    /// </summary>
    [Produces("application/json")]
    [Route("api/notifications")]
    public abstract class NotificationControllerBase : Controller
    {
        /// <summary>
        /// Get User Notifications
        /// </summary>
        /// <param name="notificationType"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNotificationViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUserNotifications(NotificationType notificationType = NotificationType.All)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserNotificationViewModel[]> res = await _notificationService.GetUserNotifications(loggedOnUserId, notificationType);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// Get User Notifications (Paginated Version)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="notificationType"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("paginated")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNotificationViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUserNotificationsPaginated([FromQuery] PagingParameterModel paging, NotificationType notificationType = NotificationType.All)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _notificationService.GetUserNotificationsPaginated(paging, loggedOnUserId, notificationType);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));
            return Ok(res.Result.Items);
        }

        /// <summary>
        /// Get unread user notifications count
        /// </summary>
        /// <param name="notificationType"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("unread/count")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(int))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUnreadUserNotificationsCount(NotificationType notificationType = NotificationType.All)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<int> res = await _notificationService.GetUnreadUserNotificationsCount(loggedOnUserId, notificationType);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// Switch Notification Status
        /// </summary>
        /// <param name="notificationId"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{notificationId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNotificationViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SwitchNotificationStatus(Guid notificationId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserNotificationViewModel> res = await _notificationService.SwitchNotificationStatus(notificationId, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// Set All User Notifications Status Read
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("allread")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SetAllNotificationsStatusRead()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> res = await _notificationService.SetAllNotificationsStatus(loggedOnUserId, NotificationStatus.Read);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// Set All User Notifications Status Unread
        /// </summary>
        [HttpPut]
        [Route("allunread")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SetAllNotificationsStatusUnread()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> res = await _notificationService.SetAllNotificationsStatus(loggedOnUserId, NotificationStatus.Unread);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// Delete Notification
        /// </summary>
        /// <param name="notificationId"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("{notificationId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeleteNotification(Guid notificationId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> res = await _notificationService.DeleteNotification(notificationId, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// Delete All Read Notifications
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeleteAllReadNotification()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> res = await _notificationService.DeleteNotification(Guid.Empty, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// Notification Service
        /// </summary>
        protected readonly IRNotificationService _notificationService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="notificationService"></param>
        public NotificationControllerBase(IRNotificationService notificationService)
        {
            _notificationService = notificationService;
        }
    }
}
