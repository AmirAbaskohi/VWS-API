using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models
{
    public class NotificationModel
    {
        public long Id { get; set; }
        public string Message { get; set; }
        public byte NotificationType { get; set; }
        public string NotifiedOnName { get; set; }
        public long NotifiedOnId { get; set; }
        public bool IsSeen { get; set; }
        public DateTime NotificationTime { get; set; }
        public List<string> Parameters { get; set; }
        public List<byte> ParameterTypes { get; set; }
    }
}
