using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._task
{
    [Table("Task_TaskComment")]
    public class TaskComment
    {
        public long Id { get; set; }
        
        public long GeneralTaskId { get; set; }
        
        [MaxLength(1000)]
        public string Body { get; set; }

        public DateTime CommentedOn { get; set; }

        public DateTime ModifiedOn { get; set; }

        public Guid CommentedBy { get; set; }

        public virtual GeneralTask GeneralTask { get; set; } 

        public virtual ICollection<TaskCommentAttachment> Attachments { get; set; }
    }
}
