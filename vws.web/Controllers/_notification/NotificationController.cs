using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Controllers._project;
using vws.web.Controllers._task;
using vws.web.Controllers._team;
using vws.web.Domain;
using vws.web.Enums;
using vws.web.Models;
using vws.web.Services;

namespace vws.web.Controllers._notification
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class NotificationController : BaseController
    {
        #region Feilds
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly INotificationService _notificationService;
        private readonly IStringLocalizer<NotificationController> _localizer;
        #endregion

        #region Ctor
        public NotificationController(IVWS_DbContext vwsDbContext, INotificationService notificationService,
            IStringLocalizer<NotificationController> localizer)
        {
            _vwsDbContext = vwsDbContext;
            _notificationService = notificationService;
            _localizer = localizer;
        }
        #endregion

        #region NotificationAPIS
        [HttpPut]
        [Authorize]
        [Route("markAsSeen")]
        public IActionResult MarkNotificationAsSeen(long id)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var selectedNotification = _vwsDbContext.Notifications.FirstOrDefault(notification => notification.Id == id);

            if (selectedNotification == null)
            {
                response.Message = "Notification not found!";
                response.AddError(_localizer["There is no notification with such id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedNotification.UserProfileId != userId)
            {
                response.Message = "Notification access denied!";
                response.AddError(_localizer["You do not have access to this notification."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedNotification.IsSeen = true;
            _vwsDbContext.Save();

            response.Message = "Notification marked as seen.";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("markAsReceived")]
        public IActionResult MarkAsReceived()
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var allNonReceivedNotifs = _vwsDbContext.Notifications.Where(notif => notif.UserProfileId == userId && !notif.IsReceived);
            foreach (var nonReceivedNotif in allNonReceivedNotifs)
            {
                nonReceivedNotif.IsReceived = true;
            }
            _vwsDbContext.Save();

            response.Message = "Notifications marked as received.";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getNumberOfNonReceived")]
        public long GetNumberOfNonReceivedNotifications()
        {
            var userId = LoggedInUserId.Value;

            return _vwsDbContext.Notifications.Where(notif => notif.UserProfileId == userId && !notif.IsReceived).LongCount();
        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public Object GetNotfications()
        {
            var userId = LoggedInUserId.Value;
            var selectedUser = _vwsDbContext.UserProfiles.Include(userProfile => userProfile.Culture).FirstOrDefault(userProfile => userProfile.UserId == userId);
            var userCulture = selectedUser.Culture != null ? selectedUser.Culture.CultureAbbreviation : "en-US";
            var userNotifs = _vwsDbContext.Notifications.Where(notification => notification.UserProfileId == userId);
            var seenNotifications = new List<NotificationResponseModel>();
            var unSeenNotifications = new List<NotificationResponseModel>();

            string message;
            List<string> parameters;
            List<bool> parametersShouldBeLocalized;
            List<byte> parametersType;
            DateTime eventTime;
            long notifiedOnId;
            string notifiedOnName;
            foreach (var userNotif in userNotifs)
            {
                if (userNotif.NotificationTypeId == (byte)SeedDataEnum.NotificationTypes.Project)
                {
                    var history = _vwsDbContext.ProjectHistories.Include(projectHistory => projectHistory.ProjectHistoryParameters).Include(projectHistory => projectHistory.Project).FirstOrDefault(projectHistory => projectHistory.Id == userNotif.ActivityId);
                    message = history.EventBody;
                    parameters = history.ProjectHistoryParameters.Select(parameter => parameter.Body).ToList();
                    parametersShouldBeLocalized = history.ProjectHistoryParameters.Select(parameter => parameter.ShouldBeLocalized).ToList();
                    parametersType = history.ProjectHistoryParameters.Select(parameter => parameter.ActivityParameterTypeId).ToList();
                    notifiedOnId = history.ProjectId;
                    notifiedOnName = history.Project.Name;
                    eventTime = history.EventTime;
                }
                else if (userNotif.NotificationTypeId == (byte)SeedDataEnum.NotificationTypes.Team)
                {
                    var history = _vwsDbContext.TeamHistories.Include(teamHistory => teamHistory.TeamHistoryParameters).Include(teamHistory => teamHistory.Team).FirstOrDefault(teamHistory => teamHistory.Id == userNotif.ActivityId);
                    message = history.EventBody;
                    parameters = history.TeamHistoryParameters.Select(parameter => parameter.Body).ToList();
                    parametersShouldBeLocalized = history.TeamHistoryParameters.Select(parameter => parameter.ShouldBeLocalized).ToList();
                    parametersType = history.TeamHistoryParameters.Select(parameter => parameter.ActivityParameterTypeId).ToList();
                    notifiedOnId = history.TeamId;
                    notifiedOnName = history.Team.Name;
                    eventTime = history.EventTime;
                }
                else
                {
                    var history = _vwsDbContext.TaskHistories.Include(taskHistory => taskHistory.TaskHistoryParameters).Include(taskHistory => taskHistory.GeneralTask).FirstOrDefault(taskHistory => taskHistory.Id == userNotif.ActivityId);
                    message = history.EventBody;
                    parameters = history.TaskHistoryParameters.Select(parameter => parameter.Body).ToList();
                    parametersShouldBeLocalized = history.TaskHistoryParameters.Select(parameter => parameter.ShouldBeLocalized).ToList();
                    parametersType = history.TaskHistoryParameters.Select(parameter => parameter.ActivityParameterTypeId).ToList();
                    notifiedOnId = history.GeneralTaskId;
                    notifiedOnName = history.GeneralTask.Title;
                    eventTime = history.EventTime;
                }
                var notifModel = new NotificationResponseModel()
                {
                    Id = userNotif.Id,
                    Message = _notificationService.LocalizeActivityByType(message, userCulture, userNotif.NotificationTypeId),
                    NotificationTime = eventTime,
                    NotificationType = userNotif.NotificationTypeId,
                    NotifiedOnId = notifiedOnId,
                    NotifiedOnName = notifiedOnName,
                    Parameters = _notificationService.LocalizeActivityParametersByType(parameters, parametersShouldBeLocalized, userCulture, userNotif.NotificationTypeId),
                    ParameterTypes = parametersType,
                    IsSeen = userNotif.IsSeen,
                    IsReceived = userNotif.IsReceived
                };
                if (userNotif.IsSeen)
                    seenNotifications.Add(notifModel);
                else
                    unSeenNotifications.Add(notifModel);
            }

            seenNotifications = seenNotifications.OrderByDescending(notifModel => notifModel.NotificationTime).ToList();
            unSeenNotifications = unSeenNotifications.OrderByDescending(notifModel => notifModel.NotificationTime).ToList();

            return new { SeenNotifs = seenNotifications, UnSeenNotifs = unSeenNotifications };
        }
        #endregion
    }
}
