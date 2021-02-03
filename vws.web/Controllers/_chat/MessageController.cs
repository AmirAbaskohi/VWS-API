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

        [HttpGet]
        [Authorize]
        [Route("get")]
        public async Task<IActionResult> GET(Guid channelId, byte channelTypeId, int pageIndex, int pageSize)
        {
            List<MessageResponseModel> MessageResponseModels = new List<MessageResponseModel>();

            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var directMessageContactUser = await userManager.FindByIdAsync(channelId.ToString());
                var privateMessages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId && (message.ChannelId == channelId || directMessageContactUser.UserName == message.FromUserName) && channelTypeId == 1);
                MessageResponseModels = FillMessageResponseModel(privateMessages);
            }
            else
            {
                var publicMessages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId && message.ChannelId == channelId && channelTypeId != (byte)SeedDataEnum.ChannelTypes.Private);
                MessageResponseModels = FillMessageResponseModel(publicMessages);
            }

            return Ok(new ResponseModel<List<MessageResponseModel>>(MessageResponseModels));

        }

        [Authorize]
        private List<MessageResponseModel> FillMessageResponseModel(IQueryable<Message> messages)
        {
            List<MessageResponseModel> MessageResponseModels = new List<MessageResponseModel>();
            foreach (var message in messages)
            {
                MessageResponseModels.Add(new MessageResponseModel
                {
                    Id = message.Id,
                    Body = message.Body,
                    SendOn = message.SendOn,
                    FromUserName = message.FromUserName,
                    SendFromMe = message.FromUserName == LoggedInUserName ? true : false
                }); ;
            }
            return MessageResponseModels;
        }

    }
}
