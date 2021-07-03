using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.admin.Models._user;
using vws.web.Controllers;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Models;
using vws.web.Services;

namespace vws.admin.Controllers._user
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
        private readonly IStringLocalizer<UserController> _localizer;
        #endregion

        #region Ctor
        public UserController(UserManager<ApplicationUser> userManager, IVWS_DbContext vwsDbContext,
            RoleManager<IdentityRole> roleManager, IUserService userService, IStringLocalizer<UserController> localizer)
        {
            _userManager = userManager;
            _vwsDbContext = vwsDbContext;
            _roleManager = roleManager;
            _userService = userService;
            _localizer = localizer;
        }
        #endregion

        [HttpGet]
        [Route("getUsers")]
        public async Task<IEnumerable<UserModelWithInfo>> GetUsers()
        {
            var allUsers = _userManager.Users;
            var result = new List<UserModelWithInfo>();

            foreach (var user in allUsers)
            {
                DateTime joinDate;
                var userModel = _userService.GetUserWithJoinDate(new Guid(user.Id), out joinDate);
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserModelWithInfo
                {
                    User = userModel,
                    Email = user.Email,
                    Roles = new List<string>(roles),
                    EmailConfirmed = user.EmailConfirmed,
                    IsAdmin = roles.Contains("Admin"),
                    JoinDate = joinDate
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

        [HttpPost]
        [Route("addUserToRole")]
        public async Task<IActionResult> AddUserToRole(Guid userId, string roleName)
        {
            var response = new ResponseModel();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                response.Message = "User not found";
                response.AddError(_localizer["User not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var roles = _roleManager.Roles.Select(role => role.Name).ToList();
            if (!roles.Contains(roleName))
            {
                response.Message = "Invalid role";
                response.AddError(_localizer["Invalid role."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                response.Message = "User already in role";
                response.AddError(_localizer["User already is in wanted role."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            await _userManager.AddToRoleAsync(user, roleName);
            _vwsDbContext.Save();

            response.Message = "User added to wanted role successfully!";
            return Ok(response);
        }
    }
}
