namespace RSecurityBackend.Models.Notification.ViewModels
{
    /// <summary>
    /// new notification view model
    /// </summary>
    public class NewNotificationViewModel
    {
        /// <summary>
        /// Subject
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Text
        /// </summary>
        public string HtmlText { get; set; }

        /// <summary>
        /// Notification Type
        /// </summary>
        public NotificationType NotificationType { get; set; }
    }
}
