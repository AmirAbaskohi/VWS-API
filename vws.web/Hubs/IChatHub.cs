using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Hubs
{
    public interface IChatHub
    {
        Task ReciveMessage(long messageId, string message, byte messageTypeId,
                           bool isSentFromMe, byte channelTypeId, Guid channelId,
                           DateTime sentOn, string senderUserName, long? replyTo);
        Task InformUserIsOnline(Guid userId);

        Task UnmuteChannel(Guid channelId, byte channelTypeId);

        Task ReciveDeleteMessage(long messageId, Guid channelId, byte channelTypeId);

        Task RecivePinMessage(long messageId, Guid channelId, byte channelTypeId);

        Task ReciveUnpinMessage(long messageId, Guid channelId, byte channelTypeId);
    }
}
