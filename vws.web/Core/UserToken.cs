using System;

namespace vws.web.Core
{
    public class UserToken
    {
        public string Token { get; set; }

        public DateTime ValidUntil { get; set; }
    }
}
