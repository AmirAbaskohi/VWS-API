using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain.dbo
{
    public class ApplicationUser : IdentityUser
    {
        public string EmailVerificationCode { get; set; }
        public DateTime EmailVerificationSendTime { get; set; }
        public string ResetPasswordCode { get; set; }
        public DateTime ResetPasswordSendTime { get; set; }
        public bool ResetPasswordCodeIsValid { get; set; }
    }
}
