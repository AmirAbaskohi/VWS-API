using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._task
{
    [Table("Task_TaskReminder")]
    public class TaskReminder
    {
        public TaskReminder()
        {
            TaskReminderLinkedUsers = new HashSet<TaskReminderLinkedUser>();
        }

        [Key]
        public long Id { get; set; }

        public long GeneralTaskId { get; set; }

        public virtual GeneralTask GeneralTask { get; set; }

        public virtual ICollection<TaskReminderLinkedUser> TaskReminderLinkedUsers { get; set; }

    }
}
