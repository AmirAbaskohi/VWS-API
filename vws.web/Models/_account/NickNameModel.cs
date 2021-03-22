using System;
using System.ComponentModel.DataAnnotations;

namespace vws.web.Models._account
{
    public class NickNameModel
    {
        [Required]
        public string NickName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public Guid NickNameSecurityStamp { get; set; }
    }
}
