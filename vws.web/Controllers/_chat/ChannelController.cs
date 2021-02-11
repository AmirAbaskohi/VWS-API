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
using vws.web.Services._chat;

namespace vws.web.Controllers._chat
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class ChannelController : BaseController
    {
        private readonly IStringLocalizer<ChannelController> localizer;
        private readonly IVWS_DbContext vwsDbContext;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IChannelService channelService;

        public ChannelController(IStringLocalizer<ChannelController> _localizer,
                                 IVWS_DbContext _vwsDbContext, UserManager<ApplicationUser> _userManager,
                                 IChannelService _channelService)
        {
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
            userManager = _userManager;
            channelService = _channelService;
        }

        private void SetChannelsIsMuted(ref List<ChannelResponseModel> channelResponseModels)
        {
            // TODO
            var userId = LoggedInUserId.Value;

            foreach (var channelResponseModel in channelResponseModels)
            {
                var mutedChannel = vwsDbContext.MutedChannels.FirstOrDefault(mChannel => mChannel.ChannelTypeId == channelResponseModel.ChannelTypeId &&
                                                                                    mChannel.ChannelId == channelResponseModel.Guid &&
                                                                                    mChannel.UserId == userId);

                if (mutedChannel != null && mutedChannel.IsMuted)
                {
                    if (mutedChannel.ForEver || mutedChannel.MuteUntil >= DateTime.Now)
                        channelResponseModel.IsMuted = true;
                    else
                        mutedChannel.IsMuted = false;
                }
            }

            vwsDbContext.Save();
        }

        [HttpGet]
        [Authorize]
        [Route("getAll")]
        public async Task<IActionResult> GetAll()
        {
            List<ChannelResponseModel> channelResponseModels = new List<ChannelResponseModel>();

            var userId = LoggedInUserId.Value;

            channelResponseModels = await channelService.GetUserChannels(userId);

            SetChannelsIsMuted(ref channelResponseModels);

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

            if (model.ChannelTypeId < 1 || model.ChannelTypeId > 4)
            {
                response.AddError(localizer["Channel type Id is not valid."]);
                response.Message = "Invalid channel type id";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            switch (model.ChannelTypeId)
            {
                case (byte)SeedDataEnum.ChannelTypes.Private:
                    var user = await vwsDbContext.GetUserProfileAsync(model.ChannelId);
                    if (user == null)
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

            if (selectedMutedChannel != null)
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

        [HttpPost]
        [Authorize]
        [Route("unmuteChannel")]
        public async Task<IActionResult> UmuteChannel([FromBody] UnmuteChannelModel model)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();

            if (model.ChannelTypeId < 1 || model.ChannelTypeId > 4)
            {
                response.AddError(localizer["Channel type Id is not valid."]);
                response.Message = "Invalid channel type id";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            switch (model.ChannelTypeId)
            {
                case (byte)SeedDataEnum.ChannelTypes.Private:
                    var user = await vwsDbContext.GetUserProfileAsync(model.ChannelId);
                    if (user == null)
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

            if (selectedMutedChannel == null)
            {
                response.AddError(localizer["Channel is not muted."]);
                response.Message = "Channel is not muted";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedMutedChannel.IsMuted = false;
            selectedMutedChannel.ForEver = false;
            vwsDbContext.Save();

            response.Message = "Channel unmuted successfully!";
            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        [Route("pinChannel")]
        public async Task<IActionResult> PinChannel([FromBody] PinChannelModel model)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();

            if (model.ChannelTypeId < 1 || model.ChannelTypeId > 4)
            {
                response.AddError(localizer["Channel type Id is not valid."]);
                response.Message = "Invalid channel type id";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            switch (model.ChannelTypeId)
            {
                case (byte)SeedDataEnum.ChannelTypes.Private:
                    var user = await vwsDbContext.GetUserProfileAsync(model.ChannelId);
                    if (user == null)
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

            var selectedPinnedChannel = vwsDbContext.PinnedChannels.FirstOrDefault(pinnedChannel => pinnedChannel.ChannelId == model.ChannelId &&
                                                                                                    pinnedChannel.ChannelTypeId == model.ChannelTypeId &&
                                                                                                    pinnedChannel.UserId == userId);

            if(selectedPinnedChannel != null)
            {
                response.AddError(localizer["Channel is already pinned."]);
                response.Message = "Channel is already pinned";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var userPinnedChannels = vwsDbContext.PinnedChannels.Where(pinnedChannel => pinnedChannel.UserId == userId)
                                                                .OrderByDescending(userPinnedChannel => userPinnedChannel.EvenOrder).
                                                                ToList();

            int last = 0;
            if (userPinnedChannels.Count != 0)
                last = userPinnedChannels[0].EvenOrder;

            var newPinnedChannel = new PinnedChannel()
            {
                ChannelId = model.ChannelId,
                ChannelTypeId = model.ChannelTypeId,
                EvenOrder = last + 2,
                UserId = userId
            };

            vwsDbContext.AddPinnedChannel(newPinnedChannel);
            vwsDbContext.Save();

            response.Message = "Channel pinned successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("unpinChannel")]
        public async Task<IActionResult> UnpinChannel([FromBody] PinChannelModel model)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();

            if (model.ChannelTypeId < 1 || model.ChannelTypeId > 4)
            {
                response.AddError(localizer["Channel type Id is not valid."]);
                response.Message = "Invalid channel type id";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            switch (model.ChannelTypeId)
            {
                case (byte)SeedDataEnum.ChannelTypes.Private:
                    var user = await vwsDbContext.GetUserProfileAsync(model.ChannelId);
                    if (user == null)
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

            var selectedPinnedChannel = vwsDbContext.PinnedChannels.FirstOrDefault(pinnedChannel => pinnedChannel.ChannelId == model.ChannelId &&
                                                                                                    pinnedChannel.ChannelTypeId == model.ChannelTypeId &&
                                                                                                    pinnedChannel.UserId == userId);

            if (selectedPinnedChannel == null)
            {
                response.AddError(localizer["Channel have not been pinned."]);
                response.Message = "Channel have not been pinned.";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            vwsDbContext.DeletePinnedChannel(selectedPinnedChannel);
            vwsDbContext.Save();

            response.Message = "Channel unpinned successfully!";
            return Ok(response);
        }
    }
}
