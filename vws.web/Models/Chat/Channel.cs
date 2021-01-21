using System;
namespace vws.web.Models.Chat
{
    public class ChannelResponseModel
    {
        public Guid Guid { get; set; }

        public string Title { get; set; }

        public string LogoUrl { get; set; }

        public byte ChannelTypeId { get; set; }

    }
}
