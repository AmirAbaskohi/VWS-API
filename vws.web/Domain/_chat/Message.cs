using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._chat
{
    [Table("Chat_Message")]
    public class Message
    {
        [Key]
        public long Id { get; set; }

        [MaxLength(4000)]
        public string Body { get; set; }

        public byte MessageTypeId { get; set; }

        public byte ChannelTypeId { get; set; }

        public Guid ChannelId { get; set; }

        [MaxLength(256)]
        public string FromUserName { get; set; }

        public long? ReplyTo { get; set; }

        public long? EditTo { get; set; }

        public DateTime SendOn { get; set; }

        public bool IsDeleted { get; set; }

        public virtual MessageType MessageType { get; set; }

        public virtual ChannelType ChannelType { get; set; }

       
    }
}
