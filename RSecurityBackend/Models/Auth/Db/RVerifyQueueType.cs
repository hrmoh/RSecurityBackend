namespace RSecurityBackend.Models.Auth.Db
{
    /// <summary>
    /// Verify Queue Type
    /// </summary>
    public enum RVerifyQueueType
    {
        /// <summary>
        /// Sign up by email
        /// </summary>
        SignUp = 0,
        /// <summary>
        /// Forgot Password by email
        /// </summary>
        ForgotPassword = 1,
        /// <summary>
        /// delete user by himself/hersef
        /// </summary>
        UserSelfDelete = 2,
        /// <summary>
        /// kick out user
        /// </summary>
        KickOutUser = 3,
        /// <summary>
        /// change (or first-time link) either email or phone number, whichever
        /// channel is detected in the requested new contact value (see
        /// <see cref="Services.IAppUserService.RequestChangeContact"/>)
        /// </summary>
        ChangeContact = 4,
        /// <summary>
        /// email or phone number changed (sent to the OLD contact value as a
        /// security notice, only when there was a previous value to notify)
        /// </summary>
        ContactChanged = 5,
    }
}
