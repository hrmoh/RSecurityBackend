namespace RSecurityBackend.Models.Cloud.ViewModels
{
    /// <summary>
    /// invite member view model
    /// </summary>
    public class InviteMemberViewModel
    {
        /// <summary>
        /// email
        /// </summary>
        public string UserEmail { get; set; }

        /// <summary>
        /// notify
        /// </summary>
        public bool Notify { get; set; }
    }
}
