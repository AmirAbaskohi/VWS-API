using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._task
{
    [Table("Task_TaskPriority")]
    public class TaskPriority
    {
        public byte Id { get; set; }

        public string Name { get; set; }
    }
}
