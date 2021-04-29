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
using vws.web.Domain._chat;
using vws.web.Enums;
using vws.web.Models;
using vws.web.Models._chat;
using vws.web.Services._chat;

namespace vws.web.Controllers._chat
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class MessageController : BaseController
    {
        #region Feilds
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IChannelService _channelService;
        private readonly IStringLocalizer<MessageController> _localizer;
        #endregion

        #region Ctor
        public MessageController(IVWS_DbContext vwsDbContext, UserManager<ApplicationUser> userManager,
                                 IChannelService channelService, IStringLocalizer<MessageController> localizer)
        {
            _vwsDbContext = vwsDbContext;
            _userManager = userManager;
            _channelService = channelService;
            _localizer = localizer;
        }
        #endregion

        #region PrivateMethods
        private List<MessageResponseModel> FillMessageResponseModel(IQueryable<Message> messages)
        {
            List<MessageResponseModel> messageResponseModels = new List<MessageResponseModel>();
            HashSet<Guid> messageSenderUserIds = messages.Select(message => message.FromUserId).ToHashSet();
            Dictionary<Guid, UserProfile> messageSenderNickNames = new Dictionary<Guid, UserProfile>();
            foreach (var messageSenderUserId in messageSenderUserIds)
            {
                var userProfile = _vwsDbContext.UserProfiles.FirstOrDefault(profile => profile.UserId == messageSenderUserId);
                messageSenderNickNames.Add(messageSenderUserId, userProfile);
            }
            foreach (var message in messages)
            {
                messageResponseModels.Add(new MessageResponseModel
                {
                    Id = message.Id,
                    Body = message.Body,
                    SendOn = message.SendOn,
                    FromNickName = messageSenderNickNames[message.FromUserId].NickName,
                    SendFromMe = message.FromUserId == LoggedInUserId ? true : false,
                    ReplyTo = message.ReplyTo,
                    IsEdited = message.IsEdited,
                    IsPinned = message.IsPinned,
                    MessageTypeId = message.MessageTypeId,
                    FromUserId = message.FromUserId,
                    FromUserImageId = messageSenderNickNames[message.FromUserId].ProfileImageGuid
                }); ;
            }
            return messageResponseModels;
        }
        #endregion

        #region MessageAPIS
        [HttpGet]
        [Authorize]
        [Route("get")]
        public async Task<IActionResult> GetMessages(Guid channelId, byte channelTypeId, int pageIndex, int pageSize)
        {
            var response = new ResponseModel<List<MessageResponseModel>>();
            var userId = LoggedInUserId.Value;

            List<MessageResponseModel> messageResponseModels = new List<MessageResponseModel>();

            if (!_channelService.DoesChannelExist(channelId, channelTypeId))
            {
                response.AddError(_localizer["There is no channel with given information."]);
                response.Message = "Channel not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_channelService.HasUserAccessToChannel(userId, channelId, channelTypeId))
            {
                response.AddError(_localizer["You do not have access to this channel."]);
                response.Message = "Channel access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var directMessageContactUser = await _userManager.FindByIdAsync(channelId.ToString());
                var privateMessages = _vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId &&
                                                                            ((message.ChannelId == channelId && message.FromUserId == LoggedInUserId) ||
                                                                            (message.ChannelId == LoggedInUserId && message.FromUserId == Guid.Parse(directMessageContactUser.Id))) &&
                                                                            !message.IsDeleted);
                messageResponseModels = FillMessageResponseModel(privateMessages);
            }
            else
            {
                var publicMessages = _vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId && message.ChannelId == channelId && !message.IsDeleted);
                messageResponseModels = FillMessageResponseModel(publicMessages);
            }

            response.Value = messageResponseModels;
            response.Message = "Messages returned successfully!";
            return Ok(response);

        }

        [HttpGet]
        [Authorize]
        [Route("getPinnedMessages")]
        public async Task<IActionResult> GetPinnedMessages(Guid channelId, byte channelTypeId)
        {
            var response = new ResponseModel<List<MessageResponseModel>>();
            var userId = LoggedInUserId.Value;

            List<MessageResponseModel> messageResponseModels = new List<MessageResponseModel>();

            if (!_channelService.DoesChannelExist(channelId, channelTypeId))
            {
                response.AddError(_localizer["There is no channel with given information."]);
                response.Message = "Channel not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_channelService.HasUserAccessToChannel(userId, channelId, channelTypeId))
            {
                response.AddError(_localizer["You do not have access to this channel."]);
                response.Message = "Channel access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var directMessageContactUser = await _userManager.FindByIdAsync(channelId.ToString());
                var privateMessages = _vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId &&
                                                                            ((message.ChannelId == channelId && message.FromUserId == LoggedInUserId) ||
                                                                            (message.ChannelId == LoggedInUserId && message.FromUserId == Guid.Parse(directMessageContactUser.Id))) &&
                                                                            !message.IsDeleted && message.IsPinned);
                privateMessages = privateMessages.OrderByDescending(message => message.PinEvenOrder);
                messageResponseModels = FillMessageResponseModel(privateMessages);
            }
            else
            {
                var publicMessages = _vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId && message.ChannelId == channelId && !message.IsDeleted && message.IsPinned);
                publicMessages = publicMessages.OrderByDescending(message => message.PinEvenOrder);
                messageResponseModels = FillMessageResponseModel(publicMessages);
            }

            response.Value = messageResponseModels;
            response.Message = "Pinned messages returned successfully!";
            return Ok(response);
        }
        #endregion

    }
}
