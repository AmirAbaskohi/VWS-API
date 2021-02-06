using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._chat;
using vws.web.Domain._department;
using vws.web.Domain._project;
using vws.web.Domain._team;
using vws.web.Enums;
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
                .Include(teamMember => teamMember.UserProfile)
                .Where(teamMember => userTeams.Select(userTeam => userTeam.Id).Contains(teamMember.TeamId) && !teamMember.HasUserLeft)
                .Select(teamMember => teamMember.UserProfile).Distinct().ToList();
            userTeamMates.Remove(await vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value));

            foreach(var userTeamMate in userTeamMates)
            {
                channelResponseModels.Add(new ChannelResponseModel
                {
                    Guid = userTeamMate.UserId,
                    ChannelTypeId = (byte)SeedDataEnum.ChannelTypes.Private,
                    LogoUrl = "http://app.seventask.com/assets/Images/Chat/DefaultAvatars/User.jpg",
                    Title = (await userManager.FindByIdAsync(userTeamMate.UserId.ToString())).UserName
                });
            }

            channelResponseModels.AddRange(userTeams.Select(userTeam => new ChannelResponseModel
            {
                Guid = userTeam.Guid,
                ChannelTypeId = (byte)SeedDataEnum.ChannelTypes.Team,
                LogoUrl = "http://app.seventask.com/assets/Images/Chat/DefaultAvatars/Team.jpg",
                Title = userTeam.Name
            }));

            channelResponseModels.AddRange(userProjects.Select(userProject => new ChannelResponseModel
            {
                Guid = userProject.Guid,
                ChannelTypeId = (byte)SeedDataEnum.ChannelTypes.Project,
                LogoUrl = "http://app.seventask.com/assets/Images/Chat/DefaultAvatars/Project.jpg",
                Title = userProject.Name
            }));

            channelResponseModels.AddRange(userDepartments.Select(userDepartment => new ChannelResponseModel
            {
                Guid = userDepartment.Guid,
                ChannelTypeId = (byte)SeedDataEnum.ChannelTypes.Department,
                LogoUrl = "http://app.seventask.com/assets/Images/Chat/DefaultAvatars/Department.jpg",
                Title = userDepartment.Name
            }));


            return Ok(new ResponseModel<List<ChannelResponseModel>>(channelResponseModels));

        }

        [HttpPost]
        [Authorize]
        [Route("muteChannel")]
        public async Task<IActionResult> MuteChannel([FromBody] MuteChannelModel model)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var muteUntil = DateTime.Now.AddMinutes(model.MuteMinutes);
            
            if(model.ChannelTypeId < 1 || model.ChannelTypeId > 4)
            {
                response.AddError(localizer["Channel type Id is not valid."]);
                response.Message = "Invalid channel type id";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            switch(model.ChannelTypeId)
            {
                case (byte)SeedDataEnum.ChannelTypes.Private:
                    var user = await vwsDbContext.GetUserProfileAsync(model.ChannelId);
                    if(user == null)
                    {
                        response.AddError(localizer["There is no user with such Id."]);
                        response.Message = "User not found";
                        return StatusCode(StatusCodes.Status400BadRequest, response);
                    }
                    break;
                case (byte)SeedDataEnum.ChannelTypes.Team:
                    var selectedTeam = vwsDbContext.Teams.FirstOrDefault(team => team.Guid == model.ChannelId);
                    if (selectedTeam == null || selectedTeam.IsDeleted)
                    {
                        response.AddError(localizer["There is no team with such Id."]);
                        response.Message = "Team not found";
                        return StatusCode(StatusCodes.Status400BadRequest, response);
                    }
                    break;
                case (byte)SeedDataEnum.ChannelTypes.Project:
                    var selectedProject = vwsDbContext.Projects.FirstOrDefault(project => project.Guid == model.ChannelId);
                    if (selectedProject == null || selectedProject.IsDeleted)
                    {
                        response.AddError(localizer["There is no project with such Id."]);
                        response.Message = "Project not found";
                        return StatusCode(StatusCodes.Status400BadRequest, response);
                    }
                    break;
                case (byte)SeedDataEnum.ChannelTypes.Department:
                    var selectedDepartment = vwsDbContext.Departments.FirstOrDefault(department => department.Guid == model.ChannelId);
                    if (selectedDepartment == null || selectedDepartment.IsDeleted)
                    {
                        response.AddError(localizer["There is no department with such Id."]);
                        response.Message = "Department not found";
                        return StatusCode(StatusCodes.Status400BadRequest, response);
                    }
                    break;
                default:
                    break;
            }

            var selectedMutedChannel = vwsDbContext.MutedChannels.FirstOrDefault(mutedChannels => mutedChannels.ChannelId == model.ChannelId &&
                                                                                                  mutedChannels.UserId == userId &&
                                                                                                  mutedChannels.ChannelTypeId == model.ChannelTypeId);

            if(selectedMutedChannel == null)
            {
                selectedMutedChannel.ForEver = model.ForEver;
                selectedMutedChannel.IsMuted = true;
                selectedMutedChannel.MuteUntil = muteUntil;
            }
            else
            {
                var newMutedChannel = new MutedChannel()
                {
                    ChannelId = model.ChannelId,
                    ChannelTypeId = model.ChannelTypeId,
                    ForEver = model.ForEver,
                    IsMuted = true,
                    UserId = userId,
                    MuteUntil = muteUntil
                };
                await vwsDbContext.AddMutedChannelAsync(newMutedChannel);
            }
            vwsDbContext.Save();

            response.Message = "Channel muted successfully!";
            return Ok(response);
        }
    }
}
