using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._base;

namespace vws.web.Domain._task
{
    [Table("Task_TimeTrackPausedSpentTime")]
    public class TimeTrackPausedSpentTime
    {
        public Guid UserProfileId { get; set; }

        public long GeneralTaskId { get; set; }

        public double TotalTimeInMinutes { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        public virtual GeneralTask GeneralTask { get; set; }
    }
}