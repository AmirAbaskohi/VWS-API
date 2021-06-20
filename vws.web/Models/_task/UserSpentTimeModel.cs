using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class UserSpentTimeModel
    {
        public UserModel User { get; set; }
        public bool IsFinished { get; set; }
        public double TotalTimeInMinutes { get; set; }
    }
}
