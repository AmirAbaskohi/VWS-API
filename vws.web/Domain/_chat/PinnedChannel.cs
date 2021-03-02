using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._chat
{
    [Table("Chat_PinnedChannel")]
    public class PinnedChannel
    {
        public int Id { get; set; }

        public Guid ChannelId { get; set; }

        public byte ChannelTypeId { get; set; }

        public Guid UserId { get; set; }

        public int EvenOrder { get; set; }
    }
}
