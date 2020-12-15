using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "نام کاربری الزامی است")]
        public string Username { get; set; }
        [EmailAddress]
        [Required(ErrorMessage = "ایمیل الزامی است")]
        public string Email { get; set; }
        [Required(ErrorMessage = "رمز عبور الزامی است")]
        public string Password { get; set; }
    }
}
