using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using vws.web.Models;

namespace vws.web.Repositories
{
    public interface IEmailSender
    {
        public Task SendEmailAsync(SendEmailModel emailModel, out string errorMessage);
    }
}
