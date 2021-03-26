using System;
using System.ComponentModel.DataAnnotations;

namespace vws.web.Models._account
{
    public class NickNameModel
    {
        [Required]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "NickName length must be 3-100 chars.")]
        public string NickName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public Guid NickNameSecurityStamp { get; set; }
    }
}
