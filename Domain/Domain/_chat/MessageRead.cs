using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._chat
{
    [Table("Chat_MessageRead")]
    public class MessageRead
    {
        public long Id { get; set; }

        public long MessageId { get; set; }

        public Guid ReadBy { get; set; }

        public Guid ChannelId { get; set; }

        public byte ChannelTypeId { get; set; }

        public virtual Message Message { get; set; }
    }
}
