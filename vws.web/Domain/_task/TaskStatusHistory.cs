using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._base;

namespace vws.web.Domain._task
{
    [Table("Task_TaskStatusHistory")]
    public class TaskStatusHistory
    {
        public long Id { get; set; }

        public long GeneralTaskId { get; set; }

        [ForeignKey("ChangedBy")]
        public Guid ChangeById { get; set; }

        [ForeignKey("LastStatus")]
        public int LastStatusId { get; set; }

        [ForeignKey("NewStatus")]
        public int NewStatusId { get; set; }

        public virtual GeneralTask GeneralTask { get; set; }

        public virtual UserProfile ChangedBy { get; set; }

        public virtual TaskStatus LastStatus { get; set; }

        public virtual TaskStatus NewStatus { get; set; }
    }
}
