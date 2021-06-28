using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._base;

namespace vws.web.Domain._task
{
    [Table("Task_TimeTrackPause")]
    public class TimeTrackPause
    {
        [Key]
        [ForeignKey("TimeTrack")]
        public long TimeTrackId { get; set; }

        public Guid UserProfileId { get; set; } 

        public long GeneralTaskId { get; set; }

        public double TotalTimeInMinutes { get; set; }

        public virtual TimeTrack TimeTrack { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        public virtual GeneralTask GeneralTask { get; set; }
    }
}
