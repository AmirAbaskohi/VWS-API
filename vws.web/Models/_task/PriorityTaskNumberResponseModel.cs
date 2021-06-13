using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class PriorityTaskNumberResponseModel
    {
        public byte PriorityId { get; set; }
        public string PriorityName { get; set; }
        public long TaskNumber { get; set; }
    }
}
