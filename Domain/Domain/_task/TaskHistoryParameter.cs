using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._task
{
    [Table("Task_TaskHistoryParameter")]
    public class TaskHistoryParameter
    {
        public long Id { get; set; }
        
        public byte ActivityParameterTypeId { get; set; }
        
        public long TaskHistoryId { get; set; }

        public string Body { get; set; }

        public bool ShouldBeLocalized { get; set; }

        public virtual ActivityParameterType ActivityParameterType { get; set; }

        public virtual TaskHistory TaskHistory { get; set; }
    }
}
