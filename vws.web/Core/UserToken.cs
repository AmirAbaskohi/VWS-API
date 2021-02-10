using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Core
{
    public class UserToken
    {
        public string Token { get; set; }
        public DateTime ValidUntil { get; set; }
    }
}
