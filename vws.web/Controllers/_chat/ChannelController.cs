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
        private readonly IChannelService channelService;
        private readonly UserManager<ApplicationUser> userManager;

        public ChannelController(IStringLocalizer<ChannelController> _localizer,
                                 IVWS_DbContext _vwsDbContext,
                                 IChannelService _channelService,
                                 UserManager<ApplicationUser> _userManager)
        {
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
            channelService = _channelService;
            userManager = _userManager;
        }

        private void SetChannelsIsMuted(ref List<ChannelResponseModel> channelResponseModels)
        {
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

        private void SetChannelIsPinned(ref List<ChannelResponseModel> channelResponseModels)
        {
            var userId = LoggedInUserId.Value;

            foreach (var channelResponseModel in channelResponseModels)
            {
                var pinnedChannel = vwsDbContext.PinnedChannels.FirstOrDefault(pChannel => pChannel.ChannelTypeId == channelResponseModel.ChannelTypeId &&
                                                                                           pChannel.ChannelId == channelResponseModel.Guid &&
                                                                                           pChannel.UserId == userId);

                if (pinnedChannel != null)
                {
                    channelResponseModel.EvenOrder = pinnedChannel.EvenOrder;
                    channelResponseModel.IsPinned = true;
                }
            }
        }

        private void SetChannelLastTransactionDateTime(ref List<ChannelResponseModel> channelResponseModels)
        {
            var userId = LoggedInUserId.Value;

            foreach (var channelResponseModel in channelResponseModels)
            {
                ChannelTransaction channelTransaction;

                if (channelResponseModel.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
                    channelTransaction = vwsDbContext.ChannelTransactions.FirstOrDefault(transaction => transaction.ChannelTypeId == channelResponseModel.ChannelTypeId &&
                                                                                                        transaction.ChannelId == channelResponseModel.Guid &&
                                                                                                        transaction.UserProfileId == userId);

                else
                    channelTransaction = vwsDbContext.ChannelTransactions.FirstOrDefault(transaction => transaction.ChannelTypeId == channelResponseModel.ChannelTypeId &&
                                                                                                        transaction.ChannelId == channelResponseModel.Guid);

                if (channelTransaction != null)
                    channelResponseModel.LastTransactionDateTime = channelTransaction.LastTransactionDateTime;
            }
        }

        private void SetChannelUnreadMessages(ref List<ChannelResponseModel> channelResponseModels, List<string> userNames)
        {
            for (int i = 0; i < channelResponseModels.Count; i++)
            {
                int readMessagesCount;
                int allMessagesCount;
                Guid channelId = channelResponseModels[i].Guid;
                
                if (channelResponseModels[i].ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
                    readMessagesCount = vwsDbContext.MessageReads.Include(messageRead => messageRead.Message)
                                                                 .Where(messageRead => messageRead.ChannelId == LoggedInUserId && messageRead.Message.FromUserName == userNames[i] && !messageRead.Message.IsDeleted)
                                                                 .ToList().Count;
                else
                    readMessagesCount = vwsDbContext.MessageReads.Include(messageRead => messageRead.Message).Where(messageRead => messageRead.ChannelId == channelId && !messageRead.Message.IsDeleted)
                                                                                                             .ToList().Count;

                if (channelResponseModels[i].ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
                    allMessagesCount = vwsDbContext.Messages.Where(message => message.ChannelId == LoggedInUserId && message.FromUserName == userNames[i] && !message.IsDeleted)
                                                             .ToList().Count;
                else
                    allMessagesCount = vwsDbContext.Messages.Where(message => message.ChannelId == channelId && !message.IsDeleted)
                                                                 .ToList().Count;

                channelResponseModels[i].NumberOfUnreadMessages = allMessagesCount - readMessagesCount;
            }
        }

        private async Task<List<string>> GetChannelUserNames(List<ChannelResponseModel> channelResponseModels)
        {
            var result = new List<string>();

            foreach (var channelResponseModel in channelResponseModels)
            {
                if (channelResponseModel.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
                    result.Add((await userManager.FindByIdAsync(channelResponseModel.Guid.ToString())).UserName);
                else
                    result.Add(null);
            }

            return result;
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

            SetChannelIsPinned(ref channelResponseModels);

            SetChannelLastTransactionDateTime(ref channelResponseModels);

            SetChannelUnreadMessages(ref channelResponseModels, await GetChannelUserNames(channelResponseModels));

            channelResponseModels = channelResponseModels.OrderByDescending(channelResponseModel => channelResponseModel.LastTransactionDateTime).ToList();
            channelResponseModels = channelResponseModels.OrderByDescending(channelResponseModel => channelResponseModel.EvenOrder).ToList();

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
        public async Task<IActionResult> UnmuteChannel([FromBody] UnmuteChannelModel model)
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

        //[HttpGet]
        //[Authorize]
        //[Route("getChannelMembers")]
        //public async Task<IActionResult> GetChannelMembers(Guid channelId, byte channelTypeId)
        //{
        //    var response = new ResponseModel<List<UserModel>>();
        //    var members = new List<UserModel>();

        //    var userId = LoggedInUserId.Value;

        //    if (!channelService.DoesChannelExist(channelId, channelTypeId))
        //    {
        //        response.AddError(localizer["There is no channel with given information."]);
        //        response.Message = "Channel not found";
        //        return StatusCode(StatusCodes.Status400BadRequest, response);
        //    }

        //    if (!channelService.HasUserAccessToChannel(userId, channelId, channelTypeId))
        //    {
        //        response.AddError(localizer["You do not have access to this channel."]);
        //        response.Message = "Channel access denied";
        //        return StatusCode(StatusCodes.Status403Forbidden, response);
        //    }

        //    List<UserProfile> users = new List<UserProfile>();

        //    if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
        //    {
        //        var userProfile = await vwsDbContext.GetUserProfileAsync(userId);
        //        var otherUserProfile = await vwsDbContext.GetUserProfileAsync(channelId);
        //        members.Add(new UserModel()
        //        {
        //            ProfileImageId = userProfile.ProfileImageId,
        //            UserId = userId,
        //            UserName = (await userManager.FindByIdAsync(userId.ToString())).UserName
        //        });
        //        members.Add(new UserModel()
        //        {
        //            ProfileImageId = otherUserProfile.ProfileImageId,
        //            UserId = channelId,
        //            UserName = (await userManager.FindByIdAsync(channelId.ToString())).UserName
        //        });

        //        response.Value = members;
        //        response.Message = "Members returned successfully!";
        //        return Ok(response);
        //    }

        //    else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Team)
        //    {
        //        var selectedTeam = vwsDbContext.Teams.FirstOrDefault(team => team.Guid == channelId);
        //        users = vwsDbContext.TeamMembers.Include(teamMember => teamMember.UserProfile)
        //                                        .Where(teamMember => teamMember.Id == selectedTeam.Id && !teamMember.IsDeleted)
        //                                        .Select(teamMember => teamMember.UserProfile).ToList();
        //    }

        //    else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Department)
        //    {
        //        var selectedDepartment = vwsDbContext.Departments.FirstOrDefault(department => department.Guid == channelId);
        //        users = vwsDbContext.DepartmentMembers.Include(departmentMember => departmentMember.UserProfile)
        //                                        .Where(departmentMember => departmentMember.Id == departmentMember.Id && !departmentMember.IsDeleted)
        //                                        .Select(departmentMember => departmentMember.UserProfile).ToList();
        //    }

        //    else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Project)
        //    {
        //        var team = vwsDbContext.Teams.FirstOrDefault(team => team.Guid == channelId);
        //        users = vwsDbContext.TeamMembers.Include(teamMember => teamMember.UserProfile)
        //                                        .Where(teamMember => teamMember.Id == team.Id && !teamMember.IsDeleted)
        //                                        .Select(teamMember => teamMember.UserProfile).ToList();
        //    }
        //}
    }
}
