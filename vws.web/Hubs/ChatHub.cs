using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Hubs
{
    public class ChatHub : Hub<IChatHub>
    {
        public async Task SendMessage(string message)
        {
            await Clients.Others.ReciveMessage(message);
        }
    }
}
