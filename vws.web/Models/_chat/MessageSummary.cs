using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._chat
{
    public class MessageSummary
    {
        public string FromNickName { get; set; }

        public Guid FromUserId { get; set; }

        public string Body { get; set; }

        public DateTime SendOn { get; set; }

        public byte MessageTypeId { get; set; }
    }
}
