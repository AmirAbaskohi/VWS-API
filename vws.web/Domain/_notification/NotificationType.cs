using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._notification
{
    [Table("Notification_NotificationType")]
    public class NotificationType
    {
        public byte Id { get; set; }

        public string Name { get; set; }
    }
}
