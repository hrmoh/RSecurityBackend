namespace RSecurityBackend.Services
{
    /// <summary>
    /// Secret Generator
    /// </summary>
    public interface ISecretGenerator
    {
        /// <summary>
        /// Generates a secret
        /// </summary>
        /// <returns></returns>
        string Generate();
    }
}
