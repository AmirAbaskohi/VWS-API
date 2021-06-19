using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._calendar
{
    [Table("Calender_EventHistory")]
    public class EventHistory
    {
        public EventHistory()
        {
            EventHistoryParameters = new HashSet<EventHistoryParameter>();
        }

        public long Id { get; set; }

        public int EventId { get; set; }

        public string EventBody { get; set; }

        public DateTime EventTime { get; set; }

        public virtual Event Event { get; set; }

        public virtual ICollection<EventHistoryParameter> EventHistoryParameters { get; set; }
    }
}
