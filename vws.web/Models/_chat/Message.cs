using System;
namespace vws.web.Models._chat
{
    public class MessageResponseModel
    {
        public long Id { get; set; }

        public string FromUserName { get; set; }

        public string Body { get; set; }

        public DateTime SendOn { get; set; }

        public bool SendFromMe { get; set; }

        public bool IsEdited { get; set; }

        public long? ReplyTo { get; set; }
    }
}
