using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class TaskModel
    {
        [Required]
        public string Title { get; set; }
        public int? StatusId { get; set; }
        public string Description { get; set; }
        public byte PriorityId { get; set; }
        public int? TeamId { get; set; }
        public int? ProjectId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [Required]
        public List<Guid> Users { get; set; }
        public List<CheckListModel> CheckLists { get; set; }
    }
}
