using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._chat
{
    [Table("Chat_MutedChannel")]
    public class MutedChannel
    {
        public int Id { get; set; }

        public Guid ChannelId { get; set; }

        public Guid UserId { get; set; }

        public DateTime MuteUntil { get; set; }

        public byte ChannelTypeId { get; set; }

        public bool IsMuted { get; set; }

        public bool ForEver { get; set; }
    }
}
