namespace RSecurityBackend.Models.Cloud.ViewModels
{
    /// <summary>
    /// new workspace model
    /// </summary>
    public class NewWorkspaceModel
    {
        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// every user has access to it (Users is ignored)
        /// </summary>
        public bool IsPublic { get; set; }
    }
}
