using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._account
{
    public class RefreshTokenModel
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
}
