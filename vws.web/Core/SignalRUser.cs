using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public string UserName { get; set; }
    }
}
