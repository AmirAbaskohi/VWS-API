using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Core;
using vws.web.Domain;
using vws.web.Extensions;
using vws.web.Models._chat;
using vws.web.Services._chat;

namespace vws.web.Hubs
{
    public static class UserHandler
    {
        public static Dictionary<string, SignalRUser> ConnectedIds = new Dictionary<string, SignalRUser>();
    }

    [Authorize]
    public class ChatHub : Hub<IChatHub>
    {
        private readonly IVWS_DbContext vwsDbContext;
        private readonly IChannelService channelService;
        private readonly ILogger<ChatHub> logger;

        public ChatHub(IVWS_DbContext _vwsDbContext, IChannelService _channelService,
                        ILogger<ChatHub> _logger)
        {
            vwsDbContext = _vwsDbContext;
            channelService = _channelService;
            logger = _logger;
        }

        private string LoggedInUserName
        {
            get { return Context.User.Claims.FirstOrDefault(c => c.Type == "UserName").Value; }
        }

        private Guid LoggedInUserId
        {
            get { return Guid.Parse(Context.User.Claims.FirstOrDefault(c => c.Type == "UserId").Value); }
        }

        public void UnmuteChannel(string connectionId)
        {
            Clients.All.UnmuteChannel(Guid.NewGuid(), 2);
        }

        public override async Task OnConnectedAsync()
        {
            AddUserToConnectedUsers();
            await AddUserGroups();
            await base.OnConnectedAsync();
        }

        private void AddUserToConnectedUsers()
        {
            string connectionId = Context.ConnectionId;
            string userName = LoggedInUserName;

            if (!UserHandler.ConnectedIds.ContainsKey(userName))
            {
                try
                {
                    UserHandler.ConnectedIds.Add(userName, new SignalRUser
                    {
                        ConnectionIds = new List<string>() { connectionId },
                        ConnectionStart = DateTime.Now,
                        LatestTransaction = DateTime.Now,
                        UserName = userName
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError("Error in chathub");
                    logger.LogError(ex.Message);
                }
            }
            else
            {
                if (!UserHandler.ConnectedIds[userName].ConnectionIds.Contains(connectionId))
                    UserHandler.ConnectedIds[userName].ConnectionIds.Add(connectionId);
                UserHandler.ConnectedIds[userName].LatestTransaction = DateTime.Now;
            }
        }

        private async Task AddUserGroups()
        {
            List<Guid> userChannels = (await channelService.GetUserChannels(LoggedInUserId)).Select(channelResponse => channelResponse.Guid).ToList();

            foreach (Guid userChannel in userChannels)
                await Groups.AddToGroupAsync(Context.ConnectionId, userChannel.ToString());
        }
        public async Task SendMessage(string message, byte channelTypeId, Guid channelId, byte messageTypeId, long? replyTo = null)
        {
            var newMessage = new Domain._chat.Message
            {
                Body = message,
                SendOn = DateTime.Now,
                ChannelId = channelId,
                ChannelTypeId = channelTypeId,
                FromUserName = LoggedInUserName,
                MessageTypeId = messageTypeId,
                ReplyTo = replyTo,
            };
            vwsDbContext.AddMessage(newMessage);
            vwsDbContext.Save();

            await Clients.OthersInGroup(channelId.ToString()).ReciveMessage(newMessage.Id, newMessage.Body, newMessage.MessageTypeId,
                                                                            false, newMessage.ChannelTypeId, newMessage.ChannelId,
                                                                            newMessage.SendOn, newMessage.FromUserName, newMessage.ReplyTo);

            await Clients.Caller.ReciveMessage(newMessage.Id, newMessage.Body, newMessage.MessageTypeId,
                                               true, newMessage.ChannelTypeId, newMessage.ChannelId,
                                               newMessage.SendOn, newMessage.FromUserName, newMessage.ReplyTo);
        }
    }
}
