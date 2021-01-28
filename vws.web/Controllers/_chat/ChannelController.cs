using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._department;
using vws.web.Domain._project;
using vws.web.Domain._team;
using vws.web.Models;
using vws.web.Models._chat;

namespace vws.web.Controllers._chat
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class ChannelController : BaseController
    {
        private readonly IStringLocalizer<ChannelController> localizer;
        private readonly IVWS_DbContext vwsDbContext;
        private readonly UserManager<ApplicationUser> userManager;

        public ChannelController(IStringLocalizer<ChannelController> _localizer,
                                 IVWS_DbContext _vwsDbContext, UserManager<ApplicationUser> _userManager)
        {
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
            userManager = _userManager;
        }

        [HttpGet]
        [Authorize]
        [Route("getAll")]
        public async Task<IActionResult> GetAll()
        {
            //todo: improve performance (user task&thread for concurrency)
            List<ChannelResponseModel> channelResponseModels = new List<ChannelResponseModel>();

            List<Team> userTeams = vwsDbContext.GetUserTeams(LoggedInUserId.Value).ToList();
            List<Project> userProjects = vwsDbContext.GetUserProjects(LoggedInUserId.Value).ToList();
            List<Department> userDepartments = vwsDbContext.GetUserDepartments(LoggedInUserId.Value).ToList();
            List<UserProfile> userTeamMates = vwsDbContext.TeamMembers
                .Where(teamMember => userTeams.Select(userTeam => userTeam.Id).Contains(teamMember.TeamId))
                .Select(teamMember => teamMember.UserProfile).Distinct().ToList();
            userTeamMates.Remove(await vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value));

            foreach(var userTeamMate in userTeamMates)
            {
                channelResponseModels.Add(new ChannelResponseModel
                {
                    Guid = userTeamMate.UserId,
                    ChannelTypeId = 1,
                    LogoUrl = "http://app.seventask.com/assets/Images/Chat/DefaultAvatars/User.jpg",
                    Title = (await userManager.FindByIdAsync(userTeamMate.UserId.ToString())).UserName
                });
            }

            channelResponseModels.AddRange(userTeams.Select(userTeam => new ChannelResponseModel
            {
                Guid = userTeam.Guid,
                ChannelTypeId = 2,
                LogoUrl = "http://app.seventask.com/assets/Images/Chat/DefaultAvatars/Team.jpg",
                Title = userTeam.Name
            }));

            channelResponseModels.AddRange(userProjects.Select(userProject => new ChannelResponseModel
            {
                Guid = userProject.Guid,
                ChannelTypeId = 3,
                LogoUrl = "http://app.seventask.com/assets/Images/Chat/DefaultAvatars/Project.jpg",
                Title = userProject.Name
            }));

            channelResponseModels.AddRange(userDepartments.Select(userDepartment => new ChannelResponseModel
            {
                Guid = userDepartment.Guid,
                ChannelTypeId = 4,
                LogoUrl = "http://app.seventask.com/assets/Images/Chat/DefaultAvatars/Department.jpg",
                Title = userDepartment.Name
            }));


            return Ok(new ResponseModel<List<ChannelResponseModel>>(channelResponseModels));

        }

    }
}
