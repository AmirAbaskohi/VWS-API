using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Core;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._chat;
using vws.web.Enums;
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
        private readonly UserManager<ApplicationUser> userManager;

        public ChatHub(IVWS_DbContext _vwsDbContext, IChannelService _channelService,
                        ILogger<ChatHub> _logger, UserManager<ApplicationUser> _userManager)
        {
            vwsDbContext = _vwsDbContext;
            channelService = _channelService;
            logger = _logger;
            userManager = _userManager;
        }

        private string LoggedInUserName
        {
            get { return Context.User.Claims.FirstOrDefault(c => c.Type == "UserName").Value; }
        }

        private Guid LoggedInUserId
        {
            get { return Guid.Parse(Context.User.Claims.FirstOrDefault(c => c.Type == "UserId").Value); }
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
            string userIdToString = LoggedInUserId.ToString();

            if (!UserHandler.ConnectedIds.ContainsKey(userIdToString))
            {
                try
                {
                    UserHandler.ConnectedIds.Add(userIdToString, new SignalRUser
                    {
                        ConnectionIds = new List<string>() { connectionId },
                        ConnectionStart = DateTime.Now,
                        LatestTransaction = DateTime.Now,
                        UserName = LoggedInUserName
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
                if (!UserHandler.ConnectedIds[userIdToString].ConnectionIds.Contains(connectionId))
                    UserHandler.ConnectedIds[userIdToString].ConnectionIds.Add(connectionId);
                UserHandler.ConnectedIds[userIdToString].LatestTransaction = DateTime.Now;
            }
        }

        public static string CombineTwoGuidsInOrder(Guid firstGuid, Guid secondGuid)
        {
            return firstGuid.CompareTo(secondGuid) <= 0 ?
                   firstGuid.ToString() + secondGuid.ToString() :
                   secondGuid.ToString() + firstGuid.ToString();
        }

        private async Task AddUserGroups()
        {
            var userChannels = await channelService.GetUserChannels(LoggedInUserId);

            foreach (var userChannel in userChannels)
            {
                if (userChannel.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
                    await Groups.AddToGroupAsync(Context.ConnectionId, CombineTwoGuidsInOrder(LoggedInUserId, userChannel.Guid));
                else
                    await Groups.AddToGroupAsync(Context.ConnectionId, userChannel.Guid.ToString());
            }
        }

        private void UpdateChannelTransaction(Guid channelId, byte channelTypeId, Guid userId, DateTime transactionTime)
        {
            ChannelTransaction channelTransaction;

            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
                channelTransaction = vwsDbContext.ChannelTransactions.FirstOrDefault(transaction => transaction.ChannelTypeId == channelTypeId &&
                                                                                                    transaction.ChannelId == channelId &&
                                                                                                    transaction.UserProfileId == userId);

            else
                channelTransaction = vwsDbContext.ChannelTransactions.FirstOrDefault(transaction => transaction.ChannelTypeId == channelTypeId &&
                                                                                                    transaction.ChannelId == channelId);

            if (channelTransaction == null)
            {
                var newChannelTransaction = new ChannelTransaction()
                {
                    ChannelId = channelId,
                    ChannelTypeId = channelTypeId,
                    UserProfileId = (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private) ? (Guid?)userId : null,
                    LastTransactionDateTime = transactionTime
                };
                vwsDbContext.AddChannelTransaction(newChannelTransaction);
            }
            else
                channelTransaction.LastTransactionDateTime = transactionTime;

            vwsDbContext.Save();
        }

        //public async Task SendMessage(string message, byte channelTypeId, Guid channelId, byte messageTypeId, long? replyTo = null)
        //{
        //    if (message.Length > 4000)
        //        return;

        //    #region check reply message existance and access
        //    if (replyTo != null)
        //    {
        //        var repliedMessage = vwsDbContext.Messages.FirstOrDefault(message => message.Id == replyTo);
        //        if (repliedMessage == null || repliedMessage.IsDeleted || repliedMessage.ChannelTypeId != channelTypeId)
        //            return;
        //        if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
        //        {
        //            if (repliedMessage.ChannelId != channelId && repliedMessage.ChannelId != LoggedInUserId)
        //                return;
        //        }
        //        else
        //        {
        //            if (repliedMessage.ChannelId != channelId)
        //                return;
        //        }
        //    } 
        //    #endregion

        //    if ()

        //    var newMessage = new Domain._chat.Message
        //    {
        //        Body = message,
        //        SendOn = DateTime.Now,
        //        ChannelId = channelId,
        //        ChannelTypeId = channelTypeId,
        //        FromUserName = LoggedInUserName,
        //        MessageTypeId = messageTypeId,
        //        ReplyTo = replyTo,
        //    };
        //    vwsDbContext.AddMessage(newMessage);
        //    vwsDbContext.Save();

        //    UpdateChannelTransaction(channelId, channelTypeId, LoggedInUserId, newMessage.SendOn);

        //    if (newMessage.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
        //    {
        //        var groupName = CombineTwoGuidsInOrder(LoggedInUserId, newMessage.ChannelId);
        //        await Clients.OthersInGroup(groupName).ReceiveMessage(newMessage.Id, newMessage.Body, newMessage.MessageTypeId,
        //                                                                        false, newMessage.ChannelTypeId, LoggedInUserId,
        //                                                                        newMessage.SendOn, newMessage.FromUserName, newMessage.ReplyTo);

        //    }
        //    else
        //        await Clients.OthersInGroup(channelId.ToString()).ReceiveMessage(newMessage.Id, newMessage.Body, newMessage.MessageTypeId,
        //                                                                        false, newMessage.ChannelTypeId, newMessage.ChannelId,
        //                                                                        newMessage.SendOn, newMessage.FromUserName, newMessage.ReplyTo);

        //    await Clients.Caller.ReceiveMessage(newMessage.Id, newMessage.Body, newMessage.MessageTypeId,
        //                                           true, newMessage.ChannelTypeId, newMessage.ChannelId,
        //                                           newMessage.SendOn, newMessage.FromUserName, newMessage.ReplyTo);
        //}

        //public async Task DeleteMessage(long messageId)
        //{
        //    var selectedMessage = vwsDbContext.Messages.FirstOrDefault(message => message.Id == messageId);

        //    if (selectedMessage == null || selectedMessage.IsDeleted || selectedMessage.FromUserName != LoggedInUserName)
        //        return;

        //    selectedMessage.IsDeleted = true;
        //    vwsDbContext.Save();

        //    if (selectedMessage.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
        //    {
        //        var groupName = CombineTwoGuidsInOrder(LoggedInUserId, selectedMessage.ChannelId);
        //        await Clients.Caller.ReceiveDeleteMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId);
        //        await Clients.OthersInGroup(groupName).ReceiveDeleteMessage(messageId, LoggedInUserId, selectedMessage.ChannelTypeId);
        //    }
        //    else
        //    {
        //        await Clients.Group(selectedMessage.ChannelId.ToString()).ReceiveDeleteMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId);
        //    }
        //}

        public async Task PinMessage(long messageId)
        {
            var selectedMessage = vwsDbContext.Messages.FirstOrDefault(message => message.Id == messageId);

            if (selectedMessage == null || selectedMessage.IsDeleted || selectedMessage.IsPinned)
                return;

            List<Message> pinnedMessages = new List<Message>();
            if (selectedMessage.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var directMessageContactUser = await userManager.FindByIdAsync(selectedMessage.ChannelId.ToString());
                pinnedMessages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == selectedMessage.ChannelTypeId &&
                                                                        ((message.ChannelId == selectedMessage.ChannelId && message.FromUserName == LoggedInUserName) ||
                                                                        (message.ChannelId == LoggedInUserId && message.FromUserName == directMessageContactUser.UserName)) &&
                                                                        !message.IsDeleted &&
                                                                        message.IsPinned).ToList();
            }
            else
            {
                pinnedMessages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == selectedMessage.ChannelTypeId &&
                                                                        message.ChannelId == selectedMessage.ChannelId &&
                                                                        !message.IsDeleted && message.IsPinned)
                                                       .ToList();
            }

            int lastPinOrder = 0;
            pinnedMessages = pinnedMessages.OrderByDescending(message => message.PinEvenOrder).ToList();
            if (pinnedMessages.Count != 0)
                lastPinOrder = (int)pinnedMessages[pinnedMessages.Count - 1].PinEvenOrder;

            selectedMessage.IsPinned = true;
            selectedMessage.PinEvenOrder = lastPinOrder + 2;

            vwsDbContext.Save();

            if (selectedMessage.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var groupName = CombineTwoGuidsInOrder(LoggedInUserId, selectedMessage.ChannelId);
                await Clients.Caller.ReceivePinMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId);
                await Clients.OthersInGroup(groupName).ReceivePinMessage(messageId, LoggedInUserId, selectedMessage.ChannelTypeId);
            }
            else
            {
                await Clients.Group(selectedMessage.ChannelId.ToString()).ReceivePinMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId);
            }
        }

        public async Task UnpinMessage(long messageId)
        {
            var selectedMessage = vwsDbContext.Messages.FirstOrDefault(message => message.Id == messageId);

            if (selectedMessage == null || selectedMessage.IsDeleted || !selectedMessage.IsPinned)
                return;

            selectedMessage.IsPinned = false;
            selectedMessage.PinEvenOrder = null;

            vwsDbContext.Save();

            if (selectedMessage.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var groupName = CombineTwoGuidsInOrder(LoggedInUserId, selectedMessage.ChannelId);
                await Clients.Caller.ReceiveUnpinMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId);
                await Clients.OthersInGroup(groupName).ReceiveUnpinMessage(messageId, LoggedInUserId, selectedMessage.ChannelTypeId);
            }
            else
            {
                await Clients.Group(selectedMessage.ChannelId.ToString()).ReceiveUnpinMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId);
            }
        }

        public async Task EditMessage(long messageId, string newBody)
        {
            if (newBody.Length > 4000)
                return;

            var selectedMessage = vwsDbContext.Messages.FirstOrDefault(message => message.Id == messageId);

            if (selectedMessage == null || selectedMessage.IsDeleted ||
                selectedMessage.FromUserName != LoggedInUserName ||
                selectedMessage.MessageTypeId != (byte)SeedDataEnum.MessageTypes.Text)
                return;

            var newMessageEdit = new Domain._chat.MessageEdit
            {
                ChannelId = selectedMessage.ChannelId,
                ChannelTypeId = selectedMessage.ChannelTypeId,
                MessageId = selectedMessage.Id,
                NewBody = newBody,
                OldBody = selectedMessage.Body,
                UserProfileId = LoggedInUserId
            };

            vwsDbContext.AddMessageEdit(newMessageEdit);

            selectedMessage.IsEdited = true;
            selectedMessage.Body = newBody;

            vwsDbContext.Save();

            if (selectedMessage.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var groupName = CombineTwoGuidsInOrder(LoggedInUserId, selectedMessage.ChannelId);
                await Clients.Caller.ReceiveEditMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId, newBody);
                await Clients.OthersInGroup(groupName).ReceiveEditMessage(messageId, LoggedInUserId, selectedMessage.ChannelTypeId, newBody);
            }
            else
            {
                await Clients.Group(selectedMessage.ChannelId.ToString()).ReceiveEditMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId, newBody);
            }
        }

        public async Task MarkMessageAsRead(long messageId)
        {
            if (vwsDbContext.MessageReads.Any(messageRead => messageRead.MessageId == messageId && messageRead.ReadBy == LoggedInUserId))
                return;

            var selectedMessage = vwsDbContext.Messages.FirstOrDefault(message => message.Id == messageId);

            if (selectedMessage == null || selectedMessage.IsDeleted)
                return;

            vwsDbContext.AddMessageRead(new MessageRead()
            {
                ChannelId = selectedMessage.ChannelId,
                ChannelTypeId = selectedMessage.ChannelTypeId,
                MessageId = selectedMessage.Id,
                ReadBy = LoggedInUserId
            });

            vwsDbContext.Save();

            if (selectedMessage.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var groupName = CombineTwoGuidsInOrder(LoggedInUserId, selectedMessage.ChannelId);
                await Clients.Caller.ReceiveReadMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId);
                await Clients.OthersInGroup(groupName).ReceiveReadMessage(messageId, LoggedInUserId, selectedMessage.ChannelTypeId);
            }
            else
            {
                await Clients.Group(selectedMessage.ChannelId.ToString()).ReceiveReadMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId);
            }
        }

        public async Task MarkMessageAsDeliver(long messageId)
        {
            if (vwsDbContext.MessageDelivers.Any(messageRead => messageRead.MessageId == messageId && messageRead.ReadBy == LoggedInUserId))
                return;

            var selectedMessage = vwsDbContext.Messages.FirstOrDefault(message => message.Id == messageId);

            if (selectedMessage == null || selectedMessage.IsDeleted)
                return;

            vwsDbContext.AddMessageDeliver(new MessageDeliver()
            {
                ChannelId = selectedMessage.ChannelId,
                ChannelTypeId = selectedMessage.ChannelTypeId,
                MessageId = selectedMessage.Id,
                ReadBy = LoggedInUserId
            });

            vwsDbContext.Save();

            if (selectedMessage.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var groupName = CombineTwoGuidsInOrder(LoggedInUserId, selectedMessage.ChannelId);
                await Clients.Caller.ReceiveDeliverMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId);
                await Clients.OthersInGroup(groupName).ReceiveDeliverMessage(messageId, LoggedInUserId, selectedMessage.ChannelTypeId);
            }
            else
            {
                await Clients.Group(selectedMessage.ChannelId.ToString()).ReceiveDeliverMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId);
            }
        }
    }
}
