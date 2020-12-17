using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "ایمیل یا نام کاربری الزامی است")]
        public string UsernameOrEmail { get; set; }
        [Required(ErrorMessage = "رمز عبور الزامی است")]
        public string Password { get; set; }
    }
}
