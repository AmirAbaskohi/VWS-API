using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._project;
using vws.web.Domain._team;

namespace vws.web.Domain._task
{
    [Table("Task_GeneralTask")]
    public class GeneralTask
    {

        public GeneralTask()
        {
            TaskReminders = new HashSet<TaskReminder>();
            TaskAssigns = new HashSet<TaskAssign>();
            TaskTags = new HashSet<TaskTag>();
            TaskComments = new HashSet<TaskComment>();
        }

        public long Id { get; set; }

        public Guid Guid { get; set; }

        [Required, MaxLength(500, ErrorMessage = "Max allowed length is 500 char")]
        public string Title { get; set; }

        [MaxLength(2000, ErrorMessage = "Max allowed length is 2000 char")] // TODO: Get and Localize
        public string Description { get; set; }

        public byte TaskPriorityId { get; set; }

        public bool IsArchived { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public byte? TaskScheduleTypeId { get; set; }

        public Guid CreatedBy { get; set; }

        public Guid ModifiedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }

        public int? TeamId { get; set; }

        public int? ProjectId { get; set; }
        
        public int TaskStatusId { get; set; }

        public virtual TaskPriority TaskPriority { get; set; }

        public virtual TaskScheduleType TaskScheduleType { get; set; }

        public virtual Team Team { get; set; }

        public virtual Project Project { get; set; }

        public virtual TaskStatus Status { get; set; }

        public virtual ICollection<TaskReminder> TaskReminders { get; set; }

        public virtual ICollection<TaskAssign> TaskAssigns { get; set; }

        public virtual ICollection<TaskCheckList> TaskChecklist { get; set; }

        public virtual ICollection<TaskTag> TaskTags { get; set; }

        public ICollection<TaskComment> TaskComments { get; set; }
    }
}
