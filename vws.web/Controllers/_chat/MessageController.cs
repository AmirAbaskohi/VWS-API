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
    public class MessageController : BaseController
    {
        private readonly IStringLocalizer<MessageController> localizer;
        private readonly IVWS_DbContext vwsDbContext;
        private readonly UserManager<ApplicationUser> userManager;

        public MessageController(IStringLocalizer<MessageController> _localizer,
                                 IVWS_DbContext _vwsDbContext, UserManager<ApplicationUser> _userManager)
        {
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
            userManager = _userManager;
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
                    SendFromMe = message.FromUserName == LoggedInUserName ? true : false,
                    ReplyTo = message.ReplyTo
                }); ;
            }
            return messageResponseModels;
        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public async Task<IActionResult> GetMessages(Guid channelId, byte channelTypeId, int pageIndex, int pageSize)
        {
            List<MessageResponseModel> messageResponseModels = new List<MessageResponseModel>();

            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var directMessageContactUser = await userManager.FindByIdAsync(channelId.ToString());
                var privateMessages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId &&
                                                                            ((message.ChannelId == channelId && message.FromUserName == LoggedInUserName) ||
                                                                            (message.ChannelId == LoggedInUserId && message.FromUserName == directMessageContactUser.UserName)) &&
                                                                            !message.IsDeleted);
                messageResponseModels = FillMessageResponseModel(privateMessages);
            }
            else
            {
                var publicMessages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId && message.ChannelId == channelId && !message.IsDeleted);
                messageResponseModels = FillMessageResponseModel(publicMessages);
            }

            return Ok(new ResponseModel<List<MessageResponseModel>>(messageResponseModels));

        }

        [HttpGet]
        [Authorize]
        [Route("getPinnedMessages")]
        public async Task<IActionResult> GetPinnedMessages(Guid channelId, byte channelTypeId)
        {
            List<MessageResponseModel> messageResponseModels = new List<MessageResponseModel>();

            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var directMessageContactUser = await userManager.FindByIdAsync(channelId.ToString());
                var privateMessages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId &&
                                                                            ((message.ChannelId == channelId && message.FromUserName == LoggedInUserName) ||
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

            return Ok(new ResponseModel<List<MessageResponseModel>>(messageResponseModels));
        }

    }
}
