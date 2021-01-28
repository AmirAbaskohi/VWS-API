using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._account
{
    public class EmailModel
    {
        [Required]
        public string Email { get; set; }
    }
}
