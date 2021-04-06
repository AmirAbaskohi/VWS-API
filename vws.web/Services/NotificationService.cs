using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.EmailTemplates;
using vws.web.Models;
using vws.web.Repositories;

namespace vws.web.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IStringLocalizer<NotificationService> _localizer;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public NotificationService(IStringLocalizer<NotificationService> localizer, UserManager<ApplicationUser> userManager,
                                   IVWS_DbContext vwsDbContext, IConfiguration configuration, IEmailSender emailSender)
        {
            _localizer = localizer;
            _userManager = userManager;
            _vwsDbContext = vwsDbContext;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        private string[] LocalizeList(string culture, string[] values, bool[] localizeOrNot)
        {
            List<string> result = new List<string>();

            for (int i = 0; i < values.Length; i++)
            {
                result.Add(localizeOrNot[i] ? _localizer.WithCulture(new CultureInfo(culture))[values[i]] : values[i]);
            }
            return result.ToArray();
        }

        public async Task SendMultipleEmails(int template, List<Guid> userIds, string emailMessage, string emailSubject, string[] arguments, bool[] argumentsLocalize = null)
        {
            SendEmailModel emailModel;
            string emailErrorMessage;

            List<string> emails = new List<string>();
            List<string> cultures = new List<string>();

            bool shouldLocalizeArguments = argumentsLocalize == null ? false : true;

            foreach (var userId in userIds)
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                var profile = _vwsDbContext.UserProfiles.Include(profile => profile.Culture).FirstOrDefault(profile => profile.UserId == userId);
                emails.Add(user.Email);
                cultures.Add(profile.CultureId == null ? "en-US" : profile.Culture.CultureAbbreviation);
            }
            Task.Run(async () =>
            {
                for (int i = 0; i < emails.Count; i++)
                {
                    emailModel = new SendEmailModel
                    {
                        FromEmail = _configuration["EmailSender:NotificationEmail:EmailAddress"],
                        ToEmail = emails[i],
                        Subject = emailSubject,
                        Body = EmailTemplateUtility.GetEmailTemplate(template).Replace("{0}", String.Format(_localizer.WithCulture(new CultureInfo(cultures[i]))[emailMessage],
                                                                                                                       shouldLocalizeArguments ? LocalizeList(cultures[i], arguments, argumentsLocalize) : arguments)),
                        Credential = new NetworkCredential
                        {
                            UserName = _configuration["EmailSender:NotificationEmail:UserName"],
                            Password = _configuration["EmailSender:NotificationEmail:Password"]
                        },
                        IsBodyHtml = true
                    };
                    await _emailSender.SendEmailAsync(emailModel, out emailErrorMessage);
                }
            });
        }

        public async Task SendSingleEmail(int temaplate, string emailMessage, string subject, Guid sendToUserId, string[] arguments, bool[] argumentsLocalize = null)
        {
            string userEmail = (await _userManager.FindByIdAsync(sendToUserId.ToString())).Email;
            var profile = _vwsDbContext.UserProfiles.Include(profile => profile.Culture).FirstOrDefault(profile => profile.UserId == sendToUserId); ;
            string culture = profile.CultureId == null ? "en-US" : profile.Culture.CultureAbbreviation;
            string emailErrorMessage;

            bool shouldLocalizeArguments = argumentsLocalize == null ? false : true;

            var emailModel = new SendEmailModel
            {
                FromEmail = _configuration["EmailSender:NotificationEmail:EmailAddress"],
                ToEmail = userEmail,
                Subject = subject,
                Body = EmailTemplateUtility.GetEmailTemplate(temaplate).Replace("{0}", String.Format(_localizer.WithCulture(new CultureInfo(culture))[emailMessage]
                                                                                                             , shouldLocalizeArguments ? LocalizeList(culture, arguments, argumentsLocalize) : arguments)),
                Credential = new NetworkCredential
                {
                    UserName = _configuration["EmailSender:NotificationEmail:UserName"],
                    Password = _configuration["EmailSender:NotificationEmail:Password"]
                },
                IsBodyHtml = true
            };
            Task.Run(async () => await _emailSender.SendEmailAsync(emailModel, out emailErrorMessage));
        }
    }
}
