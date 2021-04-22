using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._calender
{
    public class EventModel
    {
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsAllDay { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int? TeamId { get; set; }
        [Required]
        public List<int> ProjectIds { get; set; }
        [Required]
        public List<Guid> Users { get; set; }
    }
}
