using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._project;

namespace vws.web.Domain._calendar
{
    [Table("Calender_EventProject")]
    public class EventProject
    {
        public int ProjectId { get; set; }

        public Project Project { get; set; }

        public int EventId { get; set; }

        public Event Event { get; set; }
    }
}
