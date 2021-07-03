using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Controllers;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Models;
using vws.web.Services;

namespace vws.admin.Controllers
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UserController : BaseController
    {
        #region Feilds
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserService _userService;
        #endregion

        #region Ctor
        public UserController(UserManager<ApplicationUser> userManager, IVWS_DbContext vwsDbContext,
            RoleManager<IdentityRole> roleManager, IUserService userService)
        {
            _userManager = userManager;
            _vwsDbContext = vwsDbContext;
            _roleManager = roleManager;
            _userService = userService;
        }
        #endregion

        [HttpGet]
        [Route("getUsers")]
        public async Task<IEnumerable<Object>> GetUsers()
        {
            var allUsers = _userManager.Users;
            var result = new List<Object>();

            foreach (var user in allUsers)
            {
                result.Add(new
                {
                    User = _userService.GetUser(new Guid(user.Id)),
                    Email = user.Email,
                    Roles = await _userManager.GetRolesAsync(user)
                });
            }

            return result;
        }

        [HttpGet]
        [Route("getRoles")]
        public IEnumerable<string> GetRoles()
        {
            return _roleManager.Roles.Select(role => role.Name).ToList();
        }
    }
}
