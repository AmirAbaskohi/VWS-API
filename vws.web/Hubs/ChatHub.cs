using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Extensions;

namespace vws.web.Hubs
{
    public class ChatHub : Hub<IChatHub>
    {
        private IVWS_DbContext _db;

        public ChatHub(IVWS_DbContext db)
        {
            _db = db;
        }

        private string LoggedInUserName
        {
            get { return Context.User.Identity.Name; }
        }

        public async Task SendMessage(string message)
        {
            var User = LoggedInUserName;
            var m = new Domain._chat.Message
            {
                Body = message,
                SendOn = DateTime.Now,
                ChannelId = 1,
                ChannelTypeId = 1,
                FromUserName = "masan",
                MessageTypeId = 1,
                //ReplyTo
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
