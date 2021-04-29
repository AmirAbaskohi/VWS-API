using System;
namespace vws.web.Models._chat
{
    public class MessageResponseModel
    {
        public long Id { get; set; }
        public string FromNickName { get; set; }
        public Guid FromUserId { get; set; }
        public Guid? FromUserImageId { get; set; }
        public string Body { get; set; }
        public DateTime SendOn { get; set; }
        public bool SendFromMe { get; set; }
        public byte MessageTypeId { get; set; }
        public bool IsEdited { get; set; }
        public bool IsPinned { get; set; }
        public long? ReplyTo { get; set; }
    }
}
