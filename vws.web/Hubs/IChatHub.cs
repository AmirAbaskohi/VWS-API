using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Models;
using vws.web.Models._task;

namespace vws.web.Hubs
{
    public interface IChatHub
    {
        Task ReceiveMessage(long messageId, string message, byte messageTypeId,
                           bool isSentFromMe, byte channelTypeId, Guid channelId,
                           DateTime sentOn, string senderNickName, Guid senderUserId,
                           Guid? senderImage, long? replyTo);

        Task InformUserIsOnline(Guid userId);

        Task UnmuteChannel(Guid channelId, byte channelTypeId);

        Task ReceiveNotification(NotificationResponseModel notification);

        Task ReceiveDeleteMessage(long messageId, Guid channelId, byte channelTypeId);

        Task ReceivePinMessage(long messageId, Guid channelId, byte channelTypeId);

        Task ReceiveUnpinMessage(long messageId, Guid channelId, byte channelTypeId);

        Task ReceiveEditMessage(long messageId, Guid channelId, byte channelTypeId, string newBody);

        Task ReceiveReadMessage(long messageId, Guid channelId, byte channelTypeId);

        Task ReceiveDeliverMessage(long messageId, Guid channelId, byte channelTypeId);

        Task ReceiveStartTime(FullRunningTaskResponseModel startedTask);

        Task ReceivePauseTime(long taskId, DateTime startDate, DateTime endDate, double totalMinutes, double totalTimePausedTillNow);

        Task ReceiveStopTime(long taskId, DateTime startDate, DateTime endDate, double totalMinutes);
    }
}
