using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using vws.web.Controllers._project;
using vws.web.Controllers._task;
using vws.web.Controllers._team;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._notification;
using vws.web.EmailTemplates;
using vws.web.Enums;
using vws.web.Hubs;
using vws.web.Models;
using vws.web.Repositories;

namespace vws.web.Services
{
    public class NotificationService : INotificationService
    {
        #region Feilds
        private readonly IStringLocalizer<NotificationService> _localizer;
        private readonly IStringLocalizer<ProjectController> _projectLocalizer;
        private readonly IStringLocalizer<TaskController> _taskLocalizer;
        private readonly IStringLocalizer<TeamController> _teamLocalizer;
        private readonly IHubContext<ChatHub, IChatHub> _hub;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        #endregion

        #region Ctor
        public NotificationService(IStringLocalizer<NotificationService> localizer, UserManager<ApplicationUser> userManager,
                                   IVWS_DbContext vwsDbContext, IConfiguration configuration, IEmailSender emailSender,
                                   IStringLocalizer<ProjectController> projectLocalizer, IStringLocalizer<TaskController> taskLocalizer,
                                   IStringLocalizer<TeamController> teamLocalizer, IHubContext<ChatHub, IChatHub> hub)
        {
            _localizer = localizer;
            _userManager = userManager;
            _vwsDbContext = vwsDbContext;
            _configuration = configuration;
            _emailSender = emailSender;
            _teamLocalizer = teamLocalizer;
            _taskLocalizer = taskLocalizer;
            _projectLocalizer = projectLocalizer;
            _hub = hub;
        }
        #endregion

        #region PrivateMethods
        private string[] LocalizeList(string culture, string[] values, bool[] localizeOrNot)
        {
            List<string> result = new List<string>();

            for (int i = 0; i < values.Length; i++)
            {
                result.Add(localizeOrNot[i] ? _localizer.WithCulture(new CultureInfo(culture))[values[i]] : values[i]);
            }
            return result.ToArray();
        }
        #endregion

        public string LocalizeActivityByType(string message, string culture, byte type)
        {
            if (type == (byte)SeedDataEnum.NotificationTypes.Project)
                return _projectLocalizer.WithCulture(new CultureInfo(culture))[message];
            else if (type == (byte)SeedDataEnum.NotificationTypes.Team)
                return _teamLocalizer.WithCulture(new CultureInfo(culture))[message];
            else if (type == (byte)SeedDataEnum.NotificationTypes.Task)
                return _taskLocalizer.WithCulture(new CultureInfo(culture))[message];
            else
                return message;
        }

        public List<string> LocalizeActivityParametersByType(List<string> parameters, List<bool> parametersShouldBeLocalized, string culture, byte type)
        {
            var result = new List<string>();

            if (!parametersShouldBeLocalized.Contains(true))
                return parameters;

            if (type == (byte)SeedDataEnum.NotificationTypes.Project)
            {
                for (int i = 0; i < parameters.Count; i++)
                    result.Add(parametersShouldBeLocalized[i] ? _projectLocalizer.WithCulture(new CultureInfo(culture))[parameters[i]] : parameters[i]);
            }
            else if (type == (byte)SeedDataEnum.NotificationTypes.Team)
            {
                for (int i = 0; i < parameters.Count; i++)
                    result.Add(parametersShouldBeLocalized[i] ? _teamLocalizer.WithCulture(new CultureInfo(culture))[parameters[i]] : parameters[i]);
            }
            else if (type == (byte)SeedDataEnum.NotificationTypes.Task)
            {
                for (int i = 0; i < parameters.Count; i++)
                    result.Add(parametersShouldBeLocalized[i] ? _taskLocalizer.WithCulture(new CultureInfo(culture))[parameters[i]] : parameters[i]);
            }
            else
                return parameters;

            return result;
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

        public void SendMultipleNotification(List<Guid> userIds, byte notificationTypeId, long activityId)
        {
            _vwsDbContext.AddNotifications(userIds.Select(userId => new Notification { ActivityId = activityId, IsSeen = false, NotificationTypeId = notificationTypeId, UserProfileId = userId }).ToList());
            _vwsDbContext.Save();

            List<string> cultures = new List<string>();
            List<long> notificationIds = new List<long>();
            foreach (var userId in userIds)
            {
                var profile = _vwsDbContext.UserProfiles.Include(profile => profile.Culture).FirstOrDefault(profile => profile.UserId == userId);
                cultures.Add(profile.CultureId == null ? "en-US" : profile.Culture.CultureAbbreviation);
                notificationIds.Add(_vwsDbContext.Notifications.FirstOrDefault(notif => notif.NotificationTypeId == notificationTypeId && notif.UserProfileId == userId && notif.ActivityId == activityId).Id);
            }

            string message;
            List<string> parameters;
            List<bool> parametersShouldBeLocalized;
            List<byte> parametersType;
            DateTime eventTime;
            long notifiedOnId;
            string notifiedOnName;

            if (notificationTypeId == (byte)SeedDataEnum.NotificationTypes.Project)
            {
                var history = _vwsDbContext.ProjectHistories.Include(projectHistory => projectHistory.ProjectHistoryParameters).Include(projectHistory => projectHistory.Project).FirstOrDefault(projectHistory => projectHistory.Id == activityId);
                message = history.Event;
                parameters = history.ProjectHistoryParameters.Select(parameter => parameter.Body).ToList();
                parametersShouldBeLocalized = history.ProjectHistoryParameters.Select(parameter => parameter.ShouldBeLocalized).ToList();
                parametersType = history.ProjectHistoryParameters.Select(parameter => parameter.ActivityParameterTypeId).ToList();
                notifiedOnId = history.ProjectId;
                notifiedOnName = history.Project.Name;
                eventTime = history.EventTime;
            }
            else if (notificationTypeId == (byte)SeedDataEnum.NotificationTypes.Team)
            {
                var history = _vwsDbContext.TeamHistories.Include(teamHistory => teamHistory.TeamHistoryParameters).Include(teamHistory => teamHistory.Team).FirstOrDefault(teamHistory => teamHistory.Id == activityId);
                message = history.Event;
                parameters = history.TeamHistoryParameters.Select(parameter => parameter.Body).ToList();
                parametersShouldBeLocalized = history.TeamHistoryParameters.Select(parameter => parameter.ShouldBeLocalized).ToList();
                parametersType = history.TeamHistoryParameters.Select(parameter => parameter.ActivityParameterTypeId).ToList();
                notifiedOnId = history.TeamId;
                notifiedOnName = history.Team.Name;
                eventTime = history.EventTime;
            }
            else
            {
                var history = _vwsDbContext.TaskHistories.Include(taskHistory => taskHistory.TaskHistoryParameters).Include(taskHistory => taskHistory.GeneralTask).FirstOrDefault(taskHistory => taskHistory.Id == activityId);
                message = history.Event;
                parameters = history.TaskHistoryParameters.Select(parameter => parameter.Body).ToList();
                parametersShouldBeLocalized = history.TaskHistoryParameters.Select(parameter => parameter.ShouldBeLocalized).ToList();
                parametersType = history.TaskHistoryParameters.Select(parameter => parameter.ActivityParameterTypeId).ToList();
                notifiedOnId = history.GeneralTaskId;
                notifiedOnName = history.GeneralTask.Title;
                eventTime = history.EventTime;
            }

            Task.Run(() =>
            {
                for (int i = 0; i < userIds.Count; i++)
                {
                    if (UserHandler.ConnectedIds.Keys.Contains(userIds[i].ToString()))
                        UserHandler.ConnectedIds[userIds[i].ToString()]
                                   .ConnectionIds
                                   .ForEach(connectionId => _hub.Clients.Client(connectionId)
                                                                        .ReceiveNotification(new NotificationResponseModel()
                                                                        {
                                                                            Id = notificationIds[i],
                                                                            Message = LocalizeActivityByType(message, cultures[i], notificationTypeId),
                                                                            NotificationTime = eventTime,
                                                                            NotificationType = notificationTypeId,
                                                                            NotifiedOnId = notifiedOnId,
                                                                            NotifiedOnName = notifiedOnName,
                                                                            Parameters = LocalizeActivityParametersByType(parameters, parametersShouldBeLocalized, cultures[i], notificationTypeId),
                                                                            ParameterTypes = parametersType,
                                                                            IsSeen = false
                                                                        }));
                }
            });
        }
    }
}
