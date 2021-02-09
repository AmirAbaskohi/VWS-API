using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._chat
{
    public class PinChannelModel
    {
        [Required]
        public Guid ChannelId { get; set; }
        [Required]
        public byte ChannelTypeId { get; set; }
    }
}
