using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._task
{
    [Table("Task_TaskCheckList")]
    public class TaskCheckList
    {
        public TaskCheckList()
        {
            TaskCheckListItems = new HashSet<TaskCheckListItem>();
        }

        [Key]
        public long Id { get; set; }

        public long GeneralTaskId { get; set; }

        [MaxLength(250)]
        public string Title { get; set; }

        public Guid CreatedBy { get; set; }

        public Guid ModifiedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }

        public bool IsDeleted { get; set; }

        public virtual GeneralTask GeneralTask { get; set; }

        public virtual ICollection<TaskCheckListItem> TaskCheckListItems { get; set; }

    }
}
