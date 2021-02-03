using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class AssignTaskModel
    {
        [Required]
        public long TaskId { get; set; }
        [Required]
        public string UserId { get; set; }
    }
}
