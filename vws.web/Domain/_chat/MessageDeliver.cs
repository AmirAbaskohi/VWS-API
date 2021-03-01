using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._chat
{
    [Table("Chat_MessageDeliver")]
    public class MessageDeliver
    {
        public long Id { get; set; }

        public long MessageId { get; set; }

        public Guid ReadBy { get; set; }

        public Guid ChannelId { get; set; }

        public byte ChannelTypeId { get; set; }

        public virtual Message Message { get; set; }
    }
}
