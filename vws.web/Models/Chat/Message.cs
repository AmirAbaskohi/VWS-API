using System;
namespace vws.web.Models.Chat
{
    public class MessageResponseModel
    {
        public long Id { get; set; }

        public string FromUserName { get; set; }

        public string Body { get; set; }

        public DateTime SendOn { get; set; }

        public bool SendFromMe { get; set; }

    }
}
