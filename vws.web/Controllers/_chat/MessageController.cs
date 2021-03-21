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
        private readonly IVWS_DbContext vwsDbContext;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IChannelService channelService;
        private readonly IStringLocalizer<MessageController> localizer;

        public MessageController(IVWS_DbContext _vwsDbContext, UserManager<ApplicationUser> _userManager,
                                 IChannelService _channelService, IStringLocalizer<MessageController> _localizer)
        {
            vwsDbContext = _vwsDbContext;
            userManager = _userManager;
            channelService = _channelService;
            localizer = _localizer;
        }

        private List<MessageResponseModel> FillMessageResponseModel(IQueryable<Message> messages)
        {
            List<MessageResponseModel> messageResponseModels = new List<MessageResponseModel>();
            foreach (var message in messages)
            {
                messageResponseModels.Add(new MessageResponseModel
                {
                    Id = message.Id,
                    Body = message.Body,
                    SendOn = message.SendOn,
                    FromUserName = message.FromUserName,
                    //SendFromMe = message.FromUserName == LoggedInUserName ? true : false, // todo: username
                    ReplyTo = message.ReplyTo,
                    IsEdited = message.IsEdited,
                    IsPinned = message.IsPinned,
                    MessageTypeId = message.MessageTypeId
                }); ;
            }
            return messageResponseModels;
        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public async Task<IActionResult> GetMessages(Guid channelId, byte channelTypeId, int pageIndex, int pageSize)
        {
            var response = new ResponseModel<List<MessageResponseModel>>();
            var userId = LoggedInUserId.Value;

            List<MessageResponseModel> messageResponseModels = new List<MessageResponseModel>();

            if (!channelService.DoesChannelExist(channelId, channelTypeId))
            {
                response.AddError(localizer["There is no channel with given information."]);
                response.Message = "Channel not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!channelService.HasUserAccessToChannel(userId, channelId, channelTypeId))
            {
                response.AddError(localizer["You do not have access to this channel."]);
                response.Message = "Channel access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var directMessageContactUser = await userManager.FindByIdAsync(channelId.ToString());
                //((message.ChannelId == channelId && message.FromUserName == LoggedInUserName) ||
                var privateMessages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId &&
                                                                            ((message.ChannelId == channelId  /* && message.FromUserName == LoggedInUserName */ ) ||
                                                                            (message.ChannelId == LoggedInUserId && message.FromUserName == directMessageContactUser.UserName)) &&
                                                                            !message.IsDeleted);
                messageResponseModels = FillMessageResponseModel(privateMessages);
            }
            else
            {
                var publicMessages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId && message.ChannelId == channelId && !message.IsDeleted);
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

            if (!channelService.DoesChannelExist(channelId, channelTypeId))
            {
                response.AddError(localizer["There is no channel with given information."]);
                response.Message = "Channel not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!channelService.HasUserAccessToChannel(userId, channelId, channelTypeId))
            {
                response.AddError(localizer["You do not have access to this channel."]);
                response.Message = "Channel access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var directMessageContactUser = await userManager.FindByIdAsync(channelId.ToString());
                // todo: username
                var privateMessages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId &&
                                                                            ((message.ChannelId == channelId /* && message.FromUserName == LoggedInUserName */ ) ||
                                                                            (message.ChannelId == LoggedInUserId && message.FromUserName == directMessageContactUser.UserName)) &&
                                                                            !message.IsDeleted && message.IsPinned);
                privateMessages = privateMessages.OrderByDescending(message => message.PinEvenOrder);
                messageResponseModels = FillMessageResponseModel(privateMessages);
            }
            else
            {
                var publicMessages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId && message.ChannelId == channelId && !message.IsDeleted && message.IsPinned);
                publicMessages = publicMessages.OrderByDescending(message => message.PinEvenOrder);
                messageResponseModels = FillMessageResponseModel(publicMessages);
            }

            response.Value = messageResponseModels;
            response.Message = "Pinned messages returned successfully!";
            return Ok(response);
        }

    }
}
