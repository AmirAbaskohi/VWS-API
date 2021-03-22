using System;
using System.Collections.Generic;

namespace vws.web.Core
{
    public class SignalRUser
    {
        public SignalRUser()
        {
            ConnectionIds = new List<string>();
        }

        public List<string> ConnectionIds { get; set; }

        public DateTime ConnectionStart { get; set; }

        public DateTime LatestTransaction { get; set; }

        public string NickName { get; set; }
    }
}
