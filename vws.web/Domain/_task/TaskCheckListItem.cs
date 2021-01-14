using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._task
{
    [Table("Task_TaskCheckListItem")]
    public class TaskCheckListItem
    {
        [Key]
        public long Id { get; set; }

        public long TaskCheckListId { get; set; }

        [MaxLength(500)]
        public string Title { get; set; }

        public Guid CreatedBy { get; set; }

        public Guid ModifiedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }

        public virtual TaskCheckList TaskCheckList { get; set; }


    }
}
