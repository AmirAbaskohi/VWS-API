using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._chat
{
    [Table("Channel_ChannelTransaction")]
    public class ChannelTransaction
    {
        public int Id { get; set; }

        public Guid ChannelId { get; set; }

        public byte ChannelTypeId { get; set; }

        public DateTime LastTransaction { get; set; }

        public Guid? UserProfileId { get; set; }
    }
}
