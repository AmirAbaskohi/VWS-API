using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Models;

namespace vws.admin.Models._user
{
    public class UserModelWithInfo
    {
        public UserModel User { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime JoinDate { get; set; }
    }
}
