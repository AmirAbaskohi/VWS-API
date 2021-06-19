using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._base;

namespace vws.web.Domain._calendar
{
    [Table("Calendar_EventMember")]
    public class EventMember
    {
        public int Id { get; set; }

        public Guid UserProfileId { get; set; }

        public int EventId { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedOn { get; set; }
        
        public virtual UserProfile UserProfile { get; set; }

        public virtual Event Event { get; set; }
    }
}
