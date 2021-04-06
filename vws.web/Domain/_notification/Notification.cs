using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._base;

namespace vws.web.Domain._notification
{
    [Table("Notification_Notification")]
    public class Notification
    {
        public long Id { get; set; }
        
        public Guid UserProfileId { get; set; }
        
        public bool IsSeen { get; set; }

        public byte NotificationTypeId { get; set; }

        public long ActivityId { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        public virtual NotificationType NotificationType { get; set; }
    }
}
