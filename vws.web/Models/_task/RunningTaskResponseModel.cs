using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class RunningTaskResponseModel
    {
        public long TaskId { get; set; }

        public bool IsPaused { get; set; }

        public DateTime? StartDate { get; set; }
    }
}
