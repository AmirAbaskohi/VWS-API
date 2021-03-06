using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models
{
    public class UserModel
    {
        public string UserName { get; set; }
        public Guid UserId { get; set; }
        public Guid? ProfileImageGuid { get; set; }
    }
}
