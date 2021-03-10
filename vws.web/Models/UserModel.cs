using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models
{
    public class UserModel : IEquatable<UserModel>
    {
        public string UserName { get; set; }
        public Guid UserId { get; set; }
        public Guid? ProfileImageGuid { get; set; }

        public bool Equals(UserModel other)
        {
            if (UserId == other.UserId)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return UserId.GetHashCode();
        }
    }
}
