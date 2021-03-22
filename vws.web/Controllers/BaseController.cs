using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace vws.web.Controllers
{
    public class BaseController : ControllerBase
    {
        public BaseController()
        {
        }

        private Claim UserId { get => User.Claims.FirstOrDefault(c => c.Type == "UserId"); }

        private Claim NickName { get => User.Claims.FirstOrDefault(c => c.Type == "NickName"); }

        public Guid? LoggedInUserId
        {
            get
            {
                if (UserId != null)
                    return Guid.Parse(UserId.Value);
                else return null;
            }
        }

        public string LoggedInNickName
        {
            get
            {
                if (NickName != null)
                    return NickName.Value;
                else return string.Empty;
            }
        }


    }
}
