using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._task
{
    [Table("Task_GeneralTask")]
    public class GeneralTask
    {

        public GeneralTask()
        {
            TaskReminders = new HashSet<TaskReminder>();
        }

        public long Id { get; set; }

        public Guid Guid { get; set; }

        [Required, MaxLength(500, ErrorMessage = "Max allowed length is 500 char")]
        public string Title { get; set; }

        [MaxLength(2000, ErrorMessage = "Max allowed length is 2000 char")]
        public string Description { get; set; }

        public bool IsArchived { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public byte? TaskScheduleTypeId { get; set; }

        public Guid CreatedBy { get; set; }

        public Guid ModifiedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }

        public virtual TaskScheduleType TaskScheduleType { get; set; }

        public virtual ICollection<TaskReminder> TaskReminders { get; set; }

    }
}
