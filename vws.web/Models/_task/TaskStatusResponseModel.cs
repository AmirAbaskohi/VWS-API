using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class TaskStatusResponseModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int? TeamId { get; set; }
        public int? ProjectId { get; set; }
        public Guid? UserProfileId { get; set; }
    }
}
