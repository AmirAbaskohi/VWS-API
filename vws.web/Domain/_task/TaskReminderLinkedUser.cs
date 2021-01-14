using System;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._base;

namespace vws.web.Domain._task
{
    [Table("Task_TaskReminderLinkedUser")]
    public class TaskReminderLinkedUser
    {
        public long Id { get; set; }

        public long TaskReminderId { get; set; }

        public Guid RemindUserId { get; set; }

        public TaskReminder TaskReminder { get; set; }

        public virtual UserProfile RemindUser { get; set; }
    }
}
