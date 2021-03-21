using Microsoft.Extensions.Logging;
using System;
using System.Net.Mail;
using System.Threading.Tasks;
using vws.web.Models;

namespace vws.web.Repositories
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(SendEmailModel emailModel, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                using (var client = new SmtpClient())
                {
                    client.Credentials = emailModel.Credential;
                    client.Host = "smtp.zoho.com";
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
