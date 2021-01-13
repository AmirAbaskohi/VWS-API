using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations.Schema;


namespace vws.web.Domain._base
{
    [Table("Base_ApplicationUser")]
    public class ApplicationUser : IdentityUser
    {

        public string EmailVerificationCode { get; set; }

        public DateTime EmailVerificationSendTime { get; set; }

        public string ResetPasswordCode { get; set; }

        public DateTime ResetPasswordSendTime { get; set; }

        public bool ResetPasswordCodeIsValid { get; set; }

    }
}
