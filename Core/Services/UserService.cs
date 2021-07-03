using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Models;

namespace vws.web.Services
{
    public class UserService : IUserService
    {
        private readonly IVWS_DbContext _vwsDbContext;
        public UserService(IVWS_DbContext vwsDbContext)
        {
            _vwsDbContext = vwsDbContext;
        }

        public UserModel GetUser(Guid guid)
        {
            var profile = _vwsDbContext.UserProfiles.FirstOrDefault(profile => profile.UserId == guid);
            if (profile == null)
                return null;
            return new UserModel()
            {
                NickName = profile.NickName,
                ProfileImageGuid = profile.ProfileImageGuid,
                UserId = profile.UserId
            };
        }

        public UserModel GetUserWithJoinDate(Guid guid, out DateTime joinDate)
        {
            var profile = _vwsDbContext.UserProfiles.FirstOrDefault(profile => profile.UserId == guid);
            if (profile == null)
            {
                joinDate = new DateTime();
                return null;
            }
            joinDate = profile.CreatedOn;
            return new UserModel()
            {
                NickName = profile.NickName,
                ProfileImageGuid = profile.ProfileImageGuid,
                UserId = profile.UserId
            };
        }
    }
}
