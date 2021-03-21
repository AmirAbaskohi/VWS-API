using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._account
{
    public class LoginRegisterModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class LoginRegisterResponseModel
    {
        public bool EmailConfirmed { get; set; }

        public bool HasNickName { get; set; }

        // if user was registered and his email is confirmed and has nickName, Then JwtToken(Token, RefreshToken, ValidTo) has valid values

        public JwtTokenModel JwtToken { get; set; }

    }
}
