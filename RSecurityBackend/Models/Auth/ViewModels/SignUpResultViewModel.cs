namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// response of the signup endpoint. Replaces the previous plain "verify" string response
    /// so the client can tell the two cases apart:
    /// - Status == "verify": normal flow, an OTP code was emailed/texted; call finalizesignup
    ///   with the code the user received.
    /// - Status == "finalize": unverified signup is allowed for this channel (see
    ///   SignUp:AllowUnverified / PhoneSignUp:AllowUnverified in configuration) and no OTP was
    ///   sent (email delivery may even be down); call finalizesignup immediately with
    ///   <see cref="Secret"/> - no user interaction/code entry needed. The resulting account
    ///   is created with EmailConfirmed/PhoneNumberConfirmed left false.
    /// </summary>
    public class SignUpResultViewModel
    {
        /// <summary>
        /// "verify" (normal flow - prompt the user for the code they received) or "finalize"
        /// (unverified signup - call finalizesignup immediately with Secret, no prompt needed)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// only populated when Status == "finalize" - the OTP secret to submit to
        /// finalizesignup immediately, without asking the user for anything (no code was
        /// ever sent, so there's nothing for them to enter)
        /// </summary>
        public string Secret { get; set; }
    }
}
