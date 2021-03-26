using System;
using System.ComponentModel.DataAnnotations;

namespace vws.web.Models._account
{
    public class ProfileImageModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public Guid ProfileImageSecurityStamp { get; set; }
    }
}
