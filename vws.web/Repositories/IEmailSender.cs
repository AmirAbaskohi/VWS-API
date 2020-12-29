using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Repositories
{
    public interface IEmailSender
    {
        public Task SendEmailAsync(string toEmail, string subject, string message, out string errorMessage, bool isMessageHtml = false);
    }
}
