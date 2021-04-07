using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._base;

namespace vws.web.Domain._task
{
    [Table("Task_TimeTrack")]
    public class TimeTrack
    {
        public TimeTrack()
        {
            TimeTrackPauses = new HashSet<TimeTrackPause>();
        }
        public long Id { get; set; }

        public Guid UserProfileId { get; set; }

        public long GeneralTaskId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public double? TotalTimeInMinutes { get; set; }

        public virtual GeneralTask GeneralTask { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        public virtual ICollection<TimeTrackPause> TimeTrackPauses { get; set; }
    }
}
