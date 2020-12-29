using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace vws.web.Repositories
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string toEmail, string subject, string message, out string errorMessage, bool isMessageHtml = false)
        {
            errorMessage = "";
            try
            {
                using (var client = new SmtpClient())
                {
                    var credentials = new NetworkCredential()
                    {
                        UserName = "seventasktest", // without @gmail.com
                        Password = "@dmin123321"
                    };

                    client.Credentials = credentials;
                    client.Host = "smtp.gmail.com";
                    client.Port = 587;
                    client.EnableSsl = true;

                    using var emailMessage = new MailMessage()
                    {
                        To = { new MailAddress(toEmail) },
                        From = new MailAddress("seventasktest@gmail.com"),
                        Subject = subject,
                        Body = message,
                        IsBodyHtml = isMessageHtml
                    };
                    client.Send(emailMessage);
                }
            }
            catch(Exception ex)
            {
                errorMessage = "Problem happened in sending email.";
            }
            return Task.CompletedTask;
        }
    }
}
