using Microsoft.EntityFrameworkCore;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Notification;
using RSecurityBackend.Models.Notification.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using RSecurityBackend.Models.Auth.Db;

namespace RSecurityBackend.Services.Implementation
{

    /// <summary>
    /// Internal messaging system implementation
    /// </summary>
    public class RNotificationService : IRNotificationService
    {
        /// <summary>
        /// Add Notification
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="subject"></param>
        /// <param name="htmlText"></param>
        /// <param name="notificationType"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNotificationViewModel>> PushNotification(Guid userId, string subject, string htmlText, NotificationType notificationType)
        {
            RUserNotification notification =
                        new RUserNotification()
                        {
                            UserId = userId,
                            DateTime = DateTime.Now,
                            Status = NotificationStatus.Unread,
                            Subject = subject,
                            HtmlText = htmlText,
                            NotificationType = notificationType,
                        };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return new RServiceResult<RUserNotificationViewModel>
                (
                new RUserNotificationViewModel()
                {
                    Id = notification.Id,
                    DateTime = notification.DateTime,
                    Status = notification.Status,
                    Subject = notification.Subject,
                    HtmlText = notification.HtmlText,
                    NotificationType = notification.NotificationType,
                }
                );
        }

        /// <summary>
        /// Switch Notification Status
        /// </summary>
        /// <param name="notificationId"></param>
        /// <param name="userId"></param>    
        /// <returns>updated notification object</returns>
        public async Task<RServiceResult<RUserNotificationViewModel>> SwitchNotificationStatus(Guid notificationId, Guid userId)
        {
            RUserNotification notification =
                        await _context.Notifications.Where(n => n.Id == notificationId && n.UserId == userId).SingleAsync();
            notification.Status = notification.Status == NotificationStatus.Unread ? NotificationStatus.Read : NotificationStatus.Unread;
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
            return new RServiceResult<RUserNotificationViewModel>
                (
                 new RUserNotificationViewModel()
                 {
                     Id = notification.Id,
                     DateTime = notification.DateTime,
                     Status = notification.Status,
                     Subject = notification.Subject,
                     HtmlText = notification.HtmlText,
                     NotificationType= notification.NotificationType,
                 }

                );
        }

        /// <summary>
        /// Set All User Notifications Status
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SetAllNotificationsStatus(Guid userId, NotificationStatus status)
        {
            var notifications = await _context.Notifications.Where(n => n.UserId == userId).ToListAsync();
            foreach (var notification in notifications)
            {
                notification.Status = status;
            }
            _context.UpdateRange(notifications);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
        }


        /// <summary>
        /// Delete Notification
        /// </summary>
        /// <param name="notificationId">if empty deletes all read notifications</param>
        /// <param name="userId"></param>    
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteNotification(Guid notificationId, Guid userId)
        {
            if (notificationId == Guid.Empty)
            {
                var notifications = await _context.Notifications.Where(n => n.UserId == userId && n.Status == NotificationStatus.Read).ToListAsync();
                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();
            }
            else
            {
                RUserNotification notification =
                            await _context.Notifications.Where(n => n.Id == notificationId && n.UserId == userId).SingleAsync();
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

            }
            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// Get User Notifications
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="notificationType"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNotificationViewModel[]>> GetUserNotifications(Guid userId, NotificationType notificationType)
        {
            return new RServiceResult<RUserNotificationViewModel[]>
                (
                await _context.Notifications
                .Where(notification => notification.UserId == userId && (notificationType == NotificationType.All || notification.NotificationType == notificationType))
                .OrderByDescending(notification => notification.DateTime)
                .Select(notification =>
                 new RUserNotificationViewModel()
                 {
                     Id = notification.Id,
                     DateTime = notification.DateTime,
                     Status = notification.Status,
                     Subject = notification.Subject,
                     HtmlText = notification.HtmlText,
                     NotificationType = notification.NotificationType,
                 }
                )
                .ToArrayAsync()
                );
        }

        /// <summary>
        /// Get User Notifications (paginated version)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <param name="notificationType"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RUserNotificationViewModel[] Items)>> GetUserNotificationsPaginated(PagingParameterModel paging, Guid userId, NotificationType notificationType)
        {
            var source = _context.Notifications
                .Where(notification => notification.UserId == userId && (notificationType == NotificationType.All || notification.NotificationType == notificationType))
                .OrderByDescending(notification => notification.DateTime)
                .Select(notification =>
                 new RUserNotificationViewModel()
                 {
                     Id = notification.Id,
                     DateTime = notification.DateTime,
                     Status = notification.Status,
                     Subject = notification.Subject,
                     HtmlText = notification.HtmlText,
                     NotificationType = notification.NotificationType,
                 });

            (PaginationMetadata PagingMeta, RUserNotificationViewModel[] Items) paginatedResult =
                await QueryablePaginator<RUserNotificationViewModel>.Paginate(source, paging);

            return new RServiceResult<(PaginationMetadata PagingMeta, RUserNotificationViewModel[] Items)>
                (
               paginatedResult
                );
        }

        /// <summary>
        /// Get Unread User Notifications Count
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="notificationType"></param>
        /// <returns></returns>
        public async Task<RServiceResult<int>> GetUnreadUserNotificationsCount(Guid userId, NotificationType notificationType)
        {
            return new RServiceResult<int>
                (
                await _context.Notifications
                .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread && (notificationType == NotificationType.All || n.NotificationType == notificationType))
                .CountAsync()
                );
        }

        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RSecurityDbContext<RAppUser, RAppRole, Guid> _context;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        public RNotificationService(RSecurityDbContext<RAppUser, RAppRole, Guid> context)
        {
            _context = context;
        }

    }
}
