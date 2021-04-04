using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._task
{
    [Table("Task_TaskAttachment")]
    public class TaskAttachment
    {
        public int FileContainerId { get; set; }

        public Guid FileContainerGuid { get; set; }

        public long GeneralTaskId { get; set; }

        public virtual _file.FileContainer FileContainer { get; set; }

        public virtual GeneralTask GeneralTask { get; set; }
    }
}
