using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._chat
{
    public class MuteChannelModel
    {
        [Required]
        public byte ChannelTypeId { get; set; }
        [Required]
        public Guid ChannelId { get; set; }
        [Required]
        public bool ForEver { get; set; }
        [Required]
        public int MuteMinutes { get; set; }
    }
}
