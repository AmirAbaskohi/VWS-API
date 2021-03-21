using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._account
{
    public class LoginModel
    {
        [Required]
        public string UsernameOrEmail { get; set; }

        [Required]
        public string Password { get; set; }

        public bool? RememberMe { get; set; }
    }

    public class LoginResponseModel
    {
        public string Token { get; set; }

        public string RefreshToken { get; set; }
        
        public DateTime ValidTo{ get; set; }

    }
}
