﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Hubs
{
    public interface IChatHub
    {
        Task ReceiveMessage(long messageId, string message, byte messageTypeId,
                           bool isSentFromMe, byte channelTypeId, Guid channelId,
                           DateTime sentOn, string senderUserName, long? replyTo);
        Task InformUserIsOnline(Guid userId);

        Task UnmuteChannel(Guid channelId, byte channelTypeId);

        Task ReceiveDeleteMessage(long messageId, Guid channelId, byte channelTypeId);

        Task ReceivePinMessage(long messageId, Guid channelId, byte channelTypeId);

        Task ReceiveUnpinMessage(long messageId, Guid channelId, byte channelTypeId);
    }
}
