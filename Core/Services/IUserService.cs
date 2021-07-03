using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Models;

namespace vws.web.Services
{
    public interface IUserService
    {
        public UserModel GetUser(Guid guid);
    }
}
