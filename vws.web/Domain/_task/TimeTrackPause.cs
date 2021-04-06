using System;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._base;

namespace vws.web.Domain._task
{
    [Table("Task_TimeTrackPause")]
    public class TimeTrackPause
    {
        public long Id { get; set; }

        public Guid UserProfileId { get; set; }

        public long GeneralTaskId { get; set; }

        public long TotalTimeInMinutes { get; set; }

        public virtual GeneralTask GeneralTask { get; set; }

        public virtual UserProfile UserProfile { get; set; }
    }
}
