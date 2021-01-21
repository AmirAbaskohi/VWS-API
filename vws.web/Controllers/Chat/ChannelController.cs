using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using vws.web.Domain;
using vws.web.Domain._department;
using vws.web.Domain._project;
using vws.web.Domain._team;
using vws.web.Models;
using vws.web.Models.Chat;

namespace vws.web.Controllers.Chat
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class ChannelController : BaseController
    {
        private readonly IStringLocalizer<TaskController> localizer;
        private readonly IVWS_DbContext vwsDbContext;
        public ChannelController(IStringLocalizer<TaskController> _localizer,
                                 IVWS_DbContext _vwsDbContext)
        {
            _localizer = localizer;
            _vwsDbContext = vwsDbContext;
        }

        [HttpPost]
        [Authorize]
        [Route("getAll")]
        public IActionResult GetAll()
        {
            //todo: improve performance (user task&thread for concurrency)
            List<ChannelResponseModel> channelResponseModels = new List<ChannelResponseModel>();

            List<Team> userTeams = vwsDbContext.GetUserTeams(LoggedInUserId.Value).ToList();
            List<Project> userProjects = vwsDbContext.GetUserProjects(LoggedInUserId.Value).ToList();
            List<Department> userDepartments = vwsDbContext.GetUserDepartments(LoggedInUserId.Value).ToList();

            channelResponseModels.AddRange(userTeams.Select(userTeam => new ChannelResponseModel
            {
                Id = userTeam.Id,
                ChannelTypeId = 2,
                LogoUrl = "http://app.seventask.com/assets/Images/logo.png",
                Title = userTeam.Name
            }));

            channelResponseModels.AddRange(userProjects.Select(userProject => new ChannelResponseModel
            {
                Id = userProject.Id,
                ChannelTypeId = 3,
                LogoUrl = "http://app.seventask.com/assets/Images/logo.png",
                Title = userProject.Name
            }));

            channelResponseModels.AddRange(userDepartments.Select(userDepartment => new ChannelResponseModel
            {
                Id = userDepartment.Id,
                ChannelTypeId = 4,
                LogoUrl = "http://app.seventask.com/assets/Images/logo.png",
                Title = userDepartment.Name
            }));


            return Ok(new ResponseModel<List<ChannelResponseModel>>(channelResponseModels));

        }

    }
}
