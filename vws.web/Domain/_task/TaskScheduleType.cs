using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._task
{
    [Table("Task_TaskScheduleType")]
    public class TaskScheduleType
    {
        public TaskScheduleType()
        {
            GeneralTasks = new HashSet<GeneralTask>();
        }

        [Key]
        public byte Id { get; set; }

        [MaxLength(1000)]
        public string NameMultiLang { get; set; }

        public virtual ICollection<GeneralTask> GeneralTasks { get; set; }

    }
}