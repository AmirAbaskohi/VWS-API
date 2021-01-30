using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._account
{
    public class ChangePasswordModel
    {
        [Required]
        public string LastPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
