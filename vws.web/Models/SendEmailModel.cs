using System;
using System.Net;

namespace vws.web.Models
{
    public class SendEmailModel
    {
        public string FromEmail { get; set; }

        public string ToEmail { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public NetworkCredential Credential { get; set; }

        public bool IsBodyHtml { get; set; }
    }
}
