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

        //private Claim UserName { get => User.Claims.FirstOrDefault(c => c.Type == "UserName"); }

        public Guid? LoggedInUserId
        {
            get
            {
                if (UserId != null)
                    return Guid.Parse(UserId.Value);
                else return null;
            }
        }

        //public string LoggedInUserName
        //{
        //    get
        //    {
        //        if (UserName != null)
        //            return UserName.Value;
        //        else return string.Empty;
        //    }
        //}


    }
}
