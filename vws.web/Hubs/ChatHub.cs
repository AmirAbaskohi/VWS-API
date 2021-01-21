using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Extensions;

namespace vws.web.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatHub : Hub<IChatHub>
    {
        private IVWS_DbContext _db;

        public ChatHub(IVWS_DbContext db)
        {
            _db = db;
        }

        private string LoggedInUserName
        {
            get { return Context.User.Claims.FirstOrDefault(c => c.Type == "UserName").Value; }
        }

        private Guid LoggedInUserId
        {
            get { return Guid.Parse(Context.User.Claims.FirstOrDefault(c => c.Type == "UserId").Value); }
        }


        public async Task SendMessage(string message, byte channelTypeId, Guid channelId, byte messageTypeId, long? replyTo = null)
        {
            var m = new Domain._chat.Message
            {
                Body = message,
                SendOn = DateTime.Now,
                ChannelId = channelId,
                ChannelTypeId = channelTypeId,
                FromUserName = LoggedInUserName,
                MessageTypeId = messageTypeId,
                ReplyTo = replyTo,
            };
            _db.AddMessage(m);
            _db.Save();
            await Clients.Others.ReciveMessage(message);
        }

        public async Task DeleteMessage(int messageId)
        {
            await Clients.Others.ReciveMessage(String.Format("Message with Id {0} Deleted.", messageId));
        }

        public async Task MakeMessageSeen(int messageId)
        {
            await Clients.Others.ReciveMessage(String.Format("Message with Id {0} have been saw.", messageId));
        }
    }
}
