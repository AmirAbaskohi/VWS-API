using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._base;

namespace vws.web.Domain._calendar
{
    [Table("Calender_EventUser")]
    public class EventUser
    {
        public Guid UserProfileId { get; set; }

        public UserProfile UserProfile { get; set; }

        public int EventId { get; set; }

        public Event Event { get; set; }
    }
}
