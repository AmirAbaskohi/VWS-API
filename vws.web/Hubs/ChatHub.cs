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

        private string LoggedInNickName
        {
            get { return Context.User.Claims.FirstOrDefault(c => c.Type == "NickName").Value; }
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
                        NickName = LoggedInNickName
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

        private void ReorderPinnedMessages(ref List<Message> pinnedMessages)
        {
            int evenOrder = 2;
            pinnedMessages = pinnedMessages.OrderBy(message => message.PinEvenOrder).ToList();

            foreach (var pinnedMessage in pinnedMessages)
            {
                pinnedMessage.PinEvenOrder = evenOrder;
                evenOrder += 2;
            }

            vwsDbContext.Save();
        }

        private async Task<List<Message>> GetPinnedMessages(Guid channelId, byte channelTypeId)
        {
            List<Message> pinnedMessages = new List<Message>();

            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var directMessageContactUser = await userManager.FindByIdAsync(channelId.ToString());
                pinnedMessages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId &&
                                                                      ((message.ChannelId == channelId && message.FromUserId == LoggedInUserId) ||
                                                                       (message.ChannelId == LoggedInUserId && message.FromUserId == Guid.Parse(directMessageContactUser.Id))) &&
                                                                       !message.IsDeleted &&
                                                                        message.IsPinned).ToList();
            }
            else
            {
                pinnedMessages = vwsDbContext.Messages.Where(message => message.ChannelTypeId == channelTypeId &&
                                                                        message.ChannelId == channelId &&
                                                                       !message.IsDeleted && message.IsPinned).ToList();
            }

            return pinnedMessages;
        }

        public async Task SendMessage(string message, byte channelTypeId, Guid channelId, byte messageTypeId, long? replyTo = null)
        {
            #region check channel existance and access
            if (!channelService.DoesChannelExist(channelId, channelTypeId) ||
                    !channelService.HasUserAccessToChannel(LoggedInUserId, channelId, channelTypeId))
                return;
            #endregion

            if (message.Length > 4000)
                return;

            #region check reply message existance and access
            if (replyTo != null)
            {
                var repliedMessage = vwsDbContext.Messages.FirstOrDefault(message => message.Id == replyTo);
                if (repliedMessage == null || repliedMessage.IsDeleted || repliedMessage.ChannelTypeId != channelTypeId)
                    return;
                if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
                {
                    if (repliedMessage.ChannelId != channelId && repliedMessage.ChannelId != LoggedInUserId)
                        return;
                }
                else
                {
                    if (repliedMessage.ChannelId != channelId)
                        return;
                }
            }
            #endregion

            var newMessage = new Domain._chat.Message
            {
                Body = message,
                SendOn = DateTime.Now,
                ChannelId = channelId,
                ChannelTypeId = channelTypeId,
                FromUserId = LoggedInUserId,
                MessageTypeId = messageTypeId,
                ReplyTo = replyTo,
            };
            vwsDbContext.AddMessage(newMessage);
            vwsDbContext.Save();

            UpdateChannelTransaction(channelId, channelTypeId, LoggedInUserId, newMessage.SendOn);
            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
                UpdateChannelTransaction(LoggedInUserId, channelTypeId, channelId, newMessage.SendOn);

            var fromUserProfile = await vwsDbContext.GetUserProfileAsync(newMessage.FromUserId);

            if (newMessage.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var groupName = CombineTwoGuidsInOrder(LoggedInUserId, newMessage.ChannelId);
                await Clients.OthersInGroup(groupName).ReceiveMessage(newMessage.Id, newMessage.Body, newMessage.MessageTypeId,
                                                                                false, newMessage.ChannelTypeId, LoggedInUserId,
                                                                                newMessage.SendOn, fromUserProfile.NickName, newMessage.FromUserId,
                                                                                fromUserProfile.ProfileImageGuid ,newMessage.ReplyTo);

            }
            else
                await Clients.OthersInGroup(channelId.ToString()).ReceiveMessage(newMessage.Id, newMessage.Body, newMessage.MessageTypeId,
                                                                                false, newMessage.ChannelTypeId, newMessage.ChannelId,
                                                                                newMessage.SendOn, fromUserProfile.NickName, newMessage.FromUserId,
                                                                                fromUserProfile.ProfileImageGuid, newMessage.ReplyTo);

            await Clients.Caller.ReceiveMessage(newMessage.Id, newMessage.Body, newMessage.MessageTypeId,
                                                   true, newMessage.ChannelTypeId, newMessage.ChannelId,
                                                   newMessage.SendOn, fromUserProfile.NickName, newMessage.FromUserId,
                                                   fromUserProfile.ProfileImageGuid, newMessage.ReplyTo);
        }

        public async Task DeleteMessage(long messageId)
        {
            var selectedMessage = vwsDbContext.Messages.FirstOrDefault(message => message.Id == messageId);

            if (selectedMessage == null || selectedMessage.IsDeleted || selectedMessage.FromUserId != LoggedInUserId)
                return;

            #region check channel existance and access
            if (!channelService.DoesChannelExist(selectedMessage.ChannelId, selectedMessage.ChannelTypeId) ||
                    !channelService.HasUserAccessToChannel(LoggedInUserId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId))
                return;
            #endregion

            selectedMessage.IsDeleted = true;

            if (selectedMessage.IsPinned)
            {
                selectedMessage.IsPinned = false;
                selectedMessage.PinEvenOrder = null;
                vwsDbContext.Save();

                List<Message> pinnedMessages = await GetPinnedMessages(selectedMessage.ChannelId, selectedMessage.ChannelTypeId);
                ReorderPinnedMessages(ref pinnedMessages);
            }

            vwsDbContext.Save();

            if (selectedMessage.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                var groupName = CombineTwoGuidsInOrder(LoggedInUserId, selectedMessage.ChannelId);
                await Clients.Caller.ReceiveDeleteMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId);
                await Clients.OthersInGroup(groupName).ReceiveDeleteMessage(messageId, LoggedInUserId, selectedMessage.ChannelTypeId);
            }
            else
            {
                await Clients.Group(selectedMessage.ChannelId.ToString()).ReceiveDeleteMessage(messageId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId);
            }
        }

        public async Task PinMessage(long messageId)
        {
            var selectedMessage = vwsDbContext.Messages.FirstOrDefault(message => message.Id == messageId);

            if (selectedMessage == null || selectedMessage.IsDeleted || selectedMessage.IsPinned)
                return;

            #region check channel existance and access
            if (!channelService.DoesChannelExist(selectedMessage.ChannelId, selectedMessage.ChannelTypeId) ||
                    !channelService.HasUserAccessToChannel(LoggedInUserId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId))
                return;
            #endregion

            List<Message> pinnedMessages = await GetPinnedMessages(selectedMessage.ChannelId, selectedMessage.ChannelTypeId);

            int lastPinOrder = 0;
            pinnedMessages = pinnedMessages.OrderByDescending(message => message.PinEvenOrder).ToList();
            if (pinnedMessages.Count != 0)
                lastPinOrder = (int)pinnedMessages[0].PinEvenOrder;

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

            #region check channel existance and access
            if (!channelService.DoesChannelExist(selectedMessage.ChannelId, selectedMessage.ChannelTypeId) ||
                    !channelService.HasUserAccessToChannel(LoggedInUserId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId))
                return;
            #endregion

            selectedMessage.IsPinned = false;
            selectedMessage.PinEvenOrder = null;

            vwsDbContext.Save();

            List<Message> pinnedMessages = await GetPinnedMessages(selectedMessage.ChannelId, selectedMessage.ChannelTypeId);

            ReorderPinnedMessages(ref pinnedMessages);

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
                selectedMessage.FromUserId != LoggedInUserId ||
                selectedMessage.MessageTypeId != (byte)SeedDataEnum.MessageTypes.Text)
                return;

            #region check channel existance and access
            if (!channelService.DoesChannelExist(selectedMessage.ChannelId, selectedMessage.ChannelTypeId) ||
                    !channelService.HasUserAccessToChannel(LoggedInUserId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId))
                return;
            #endregion

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

            #region check channel existance and access
            Guid channelId = (selectedMessage.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private && selectedMessage.FromUserId != LoggedInUserId) ? 
                              selectedMessage.FromUserId : 
                              selectedMessage.ChannelId;

            if (!channelService.DoesChannelExist(selectedMessage.ChannelId, selectedMessage.ChannelTypeId) ||
                    !channelService.HasUserAccessToChannel(LoggedInUserId , channelId,
                    selectedMessage.ChannelTypeId))
                return;
            #endregion

            var markedMessages = vwsDbContext.MarkMessagesAsRead(messageId, LoggedInUserId).ToList();
            foreach (var markedMessage in markedMessages)
            {
                if (markedMessage.ChannelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
                {
                    var groupName = CombineTwoGuidsInOrder(LoggedInUserId, markedMessage.ChannelId);
                    await Clients.Caller.ReceiveReadMessage(messageId, markedMessage.ChannelId, markedMessage.ChannelTypeId);
                    await Clients.OthersInGroup(groupName).ReceiveReadMessage(messageId, LoggedInUserId, markedMessage.ChannelTypeId);
                }
                else
                {
                    await Clients.Group(markedMessage.ChannelId.ToString()).ReceiveReadMessage(messageId, markedMessage.ChannelId, markedMessage.ChannelTypeId);
                }
            }
        }

        public async Task MarkMessageAsDeliver(long messageId)
        {
            if (vwsDbContext.MessageDelivers.Any(messageRead => messageRead.MessageId == messageId && messageRead.ReadBy == LoggedInUserId))
                return;

            var selectedMessage = vwsDbContext.Messages.FirstOrDefault(message => message.Id == messageId);

            if (selectedMessage == null || selectedMessage.IsDeleted)
                return;

            #region check channel existance and access
            if (!channelService.DoesChannelExist(selectedMessage.ChannelId, selectedMessage.ChannelTypeId) ||
                    !channelService.HasUserAccessToChannel(LoggedInUserId, selectedMessage.ChannelId, selectedMessage.ChannelTypeId))
                return;
            #endregion

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
