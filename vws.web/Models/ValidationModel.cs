using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models
{
    public class ValidationModel
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string ValidationCode { get; set; }
    }
}
