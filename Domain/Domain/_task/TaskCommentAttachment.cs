using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._task
{
    [Table("Task_TaskCommentAttachment")]
    public class TaskCommentAttachment
    {
        public int FileContainerId { get; set; }

        public Guid FileContainerGuid { get; set; }

        public long TaskCommentId { get; set; }

        public virtual _file.FileContainer FileContainer { get; set; }

        public virtual TaskComment TaskComment { get; set; }
    }
}
