using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models
{
    public class JwtTokenModel
    {
        public string Token { get; set; }

        public string RefreshToken { get; set; }

        public DateTime? ValidTo { get; set; }
    }
}
