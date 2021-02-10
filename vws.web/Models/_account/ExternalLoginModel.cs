using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._account
{
    public class ExternalLoginModel
    {
        [Required]
        public string ProviderName { get; set; }
        [Required]
        public string Token { get; set; }
    }
}
