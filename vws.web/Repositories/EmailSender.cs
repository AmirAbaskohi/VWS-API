using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using vws.web.Models;

namespace vws.web.Repositories
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly IConfiguration _configuration;

        public EmailSender(ILogger<EmailSender> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public Task SendEmailAsync(SendEmailModel emailModel, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                using (var client = new SmtpClient())
                {
                    client.Credentials = emailModel.Credential;
                    client.Host = _configuration["EmailSender:SMTPHost"];
                    client.Port = 587;
                    client.EnableSsl = true;

                    using var emailMessage = new MailMessage()
                    {
                        To = { new MailAddress(emailModel.ToEmail) },
                        From = new MailAddress(emailModel.FromEmail),
                        Subject = emailModel.Subject,
                        Body = emailModel.Body,
                        IsBodyHtml = emailModel.IsBodyHtml
                    };
                    client.Send(emailMessage);
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Problem happened in sending email.";
            }
            return Task.CompletedTask;
        }
    }
}
