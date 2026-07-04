namespace SampleProject.Sms
{
    /// <summary>
    /// supported sms providers in this sample (add your own as needed)
    /// </summary>
    public enum SmsProviderName
    {
        /// <summary>
        /// no-op provider that just logs the message (default, safe for local dev)
        /// </summary>
        Mock = 0,
        /// <summary>
        /// https://kavenegar.com
        /// </summary>
        Kavenegar = 1,
        /// <summary>
        /// https://ghasedak.me
        /// </summary>
        Ghasedak = 2,
        /// <summary>
        /// https://ippanel.com
        /// </summary>
        Ippanel = 3,
    }
}
