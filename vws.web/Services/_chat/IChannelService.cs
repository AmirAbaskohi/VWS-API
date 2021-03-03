using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Models._chat;

namespace vws.web.Services._chat
{
    public interface IChannelService
    {
        public Task<List<ChannelResponseModel>> GetUserChannels(Guid userId);

        public bool HasUserAccessToChannel(Guid userId, Guid channelId, byte channelTypeId);

        public bool DoesChannelExist(Guid channelId, byte channelTypeId);
    }
}
