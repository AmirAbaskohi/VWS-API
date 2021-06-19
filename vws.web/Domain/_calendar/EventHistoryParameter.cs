using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._calendar
{
    [Table("Calender_EventHistoryParameter")]
    public class EventHistoryParameter
    {
        public long Id { get; set; }

        public byte ActivityParameterTypeId { get; set; }

        public long EventHistoryId { get; set; }

        public string Body { get; set; }

        public bool ShouldBeLocalized { get; set; }

        public virtual ActivityParameterType ActivityParameterType { get; set; }

        public virtual EventHistory EventHistory { get; set; }
    }
}
