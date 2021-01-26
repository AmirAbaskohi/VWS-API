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
using vws.web.Models.Chat;

namespace vws.web.Controllers.Chat
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

            var messages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId && message.ChannelId == channelId);

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

            return Ok(new ResponseModel<List<MessageResponseModel>>(MessageResponseModels));

        }

    }
}
