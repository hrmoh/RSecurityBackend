using RSecurityBackend.Models.Auth.Db;
using System;
using System.ComponentModel.DataAnnotations;

namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// user informtaion
    /// </summary>
    /// <remarks>
    /// a safe subset of RAppUser
    /// </remarks>
    public class PublicRAppUser
    {
        /// <summary>
        /// Id
        /// </summary>
        /// 
        public Guid? Id { get; set; }

        /// <summary>
        /// User Name
        /// </summary>
        /// <example>test</example>
        [MinLength(4)]
        public string Username { get; set; }

        /// <summary>
        /// User Email
        /// </summary>
        /// <example>email@domain.com</example>
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// User Mobile Phone Number
        /// </summary>
        /// <example>+989121234567</example>
        [Phone]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// First Name
        /// </summary>
        /// <example>Hamid Reza</example>
        [MinLength(1)]
        public string FirstName { get; set; }

        /// <summary>
        /// SurName
        /// </summary>
        /// <example>Mohammadi</example>
        [MinLength(1)]
        public string SurName { get; set; }

        /// <summary>
        /// user status
        /// </summary>
        /// <example>1</example>
        [Required]
        public RAppUserStatus Status { get; set; }

        /// <summary>
        /// user image
        /// </summary>
        public Guid? RImageId { get; set; }

        /// <summary>
        /// nick name
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// biography
        /// </summary>
        public string Bio { get; set; }

        /// <summary>
        /// web site
        /// </summary>
        public string Website { get; set; }

        /// <summary>
        /// email verified
        /// </summary>
        public bool EmailConfirmed { get; set; }

        /// <summary>
        /// phone number verified (populated by user information and user list queries)
        /// </summary>
        public bool PhoneNumberConfirmed { get; set; }

        /// <summary>
        /// create date
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// true if the user is currently locked out (either purposely by an administrator or
        /// automatically due to too many failed login attempts) - populated by user information
        /// and user list queries
        /// </summary>
        public bool LockedOut { get; set; }

        /// <summary>
        /// when the current lockout ends (null if the user is not locked out)
        /// </summary>
        public DateTimeOffset? LockoutEnd { get; set; }

        /// <summary>
        /// the message given when the user was locked out purposely (null for automatic lockouts)
        /// </summary>
        public string LockoutMessage { get; set; }


    }
}
