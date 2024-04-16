namespace RSecurityBackend.Models.Notification
{
    /// <summary>
    /// notifiction type
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// No action required
        /// </summary>
        NoActionRequired = 0,

        /// <summary>
        /// Action Required
        /// </summary>
        ActionRequired = 1,

        /// <summary>
        /// warning
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Error
        /// </summary>
        Error = 3,

        /// <summary>
        /// all
        /// </summary>
        All = 4,

    }
}
