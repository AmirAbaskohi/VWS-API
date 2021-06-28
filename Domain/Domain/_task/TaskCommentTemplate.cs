using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._task
{
    [Table("Task_TaskCommentTemplate")]
    public class TaskCommentTemplate
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(2000)]
        public string Name { get; set; }
    }
}
