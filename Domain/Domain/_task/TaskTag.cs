using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._task
{
    [Table("Task_TaskTag")]
    public class TaskTag
    {
        public int TagId { get; set; }
        
        public long GeneralTaskId { get; set; }

        public Tag Tag { get; set; }
        
        public GeneralTask GeneralTask { get; set; }
    }
}
