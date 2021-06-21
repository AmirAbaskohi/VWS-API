using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Domain._calendar;
using vws.web.Domain._project;
using vws.web.Domain._team;
using vws.web.Enums;
using vws.web.Models;
using vws.web.Models._calender;
using vws.web.Models._project;
using vws.web.Models._team;
using vws.web.Services;
using vws.web.Services._calender;
using vws.web.Services._task;
using static vws.web.EmailTemplates.EmailTemplateTypes;

namespace vws.web.Controllers._calender
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class CalendarController : BaseController
    {
        #region Feilds
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly ICalendarManagerService _calenderManager;
        private readonly IPermissionService _permissionService;
        private readonly ITaskManagerService _taskManager;
        private readonly IStringLocalizer<CalendarController> _localizer;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        #endregion

        #region Ctor
        public CalendarController(IVWS_DbContext vwsDbContext, ICalendarManagerService calenderManager, IStringLocalizer<CalendarController> localizer,
            IPermissionService permissionService, ITaskManagerService taskManager, INotificationService notificationService,
            IUserService userService)
        {
            _vwsDbContext = vwsDbContext;
            _calenderManager = calenderManager;
            _localizer = localizer;
            _permissionService = permissionService;
            _taskManager = taskManager;
            _notificationService = notificationService;
            _userService = userService;
        }
        #endregion

        #region PrivateMethods
        private void AddEventProjects(int eventId, List<int> projectIds)
        {
            foreach (var project in projectIds)
            {
                _vwsDbContext.AddEventProject(new EventProject()
                {
                    EventId = eventId,
                    ProjectId = project
                });
            }
            _vwsDbContext.Save();
        }

        private void AddEventUsers(int eventId, List<Guid> userIds)
        {
            foreach (var user in userIds)
            {
                _vwsDbContext.AddEventUser(new EventMember()
                {
                    EventId = eventId,
                    UserProfileId = user,
                    IsDeleted = false,
                    DeletedOn = null
                });
            }
            _vwsDbContext.Save();
        }

        private void AddTeamProjectsEventCreationHistory(TeamSummaryResponseModel team, List<ProjectSummaryResponseModel> projects, DateTime time, string title)
        {
            if (team == null)
                return;

            if (projects.Count != 0)
            {
                foreach (var project in projects)
                {
                    var newHistory = new ProjectHistory()
                    {
                        EventBody = "{0} created event {1} under this project.",
                        EventTime = time,
                        ProjectId = project.Id
                    };
                    _vwsDbContext.AddProjectHistory(newHistory);
                    _vwsDbContext.Save();
                    _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
                    {
                        ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                        Body = JsonConvert.SerializeObject(_userService.GetUser(LoggedInUserId.Value)),
                        ProjectHistoryId = newHistory.Id
                    });
                    _vwsDbContext.Save();
                    _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
                    {
                        ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                        Body = title,
                        ProjectHistoryId = newHistory.Id
                    });
                    _vwsDbContext.Save();
                }
            }
            else
            {
                var newHistory = new TeamHistory()
                {
                    EventBody = "{0} created event {1} under this team.",
                    EventTime = time,
                    TeamId = team.Id
                };
                _vwsDbContext.AddTeamHistory(newHistory);
                _vwsDbContext.Save();
                _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                    Body = JsonConvert.SerializeObject(_userService.GetUser(LoggedInUserId.Value)),
                    TeamHistoryId = newHistory.Id
                });
                _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                    Body = title,
                    TeamHistoryId = newHistory.Id
                });
                _vwsDbContext.Save();
            }
        }
        #endregion

        #region CalenderAPIS

        [HttpPost]
        [Authorize]
        [Route("createEvent")]
        public async Task<IActionResult> CreateEvent(EventModel model)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel<EventResponseModel>();

            if (model.ProjectIds.Count != 0 && model.TeamId != null)
                model.TeamId = null;

            #region CheckModel
            if (String.IsNullOrEmpty(model.Title) || model.Title.Length > 500)
            {
                response.Message = "Model has problem.";
                response.AddError(_localizer["Title can not be empty or have more than 500 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (!String.IsNullOrEmpty(model.Description) && model.Description.Length > 2500)
            {
                response.Message = "Model has problem.";
                response.AddError(_localizer["Description can not have more than 2500 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (model.StartTime > model.EndTime)
            {
                response.Message = "Model has problem.";
                response.AddError(_localizer["Start time should be before end time."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            foreach (var user in model.Users)
            {
                if (!_vwsDbContext.UserProfiles.Any(profile => profile.UserId == user))
                {
                    response.Message = "Model has problem.";
                    response.AddError(_localizer["Invalid users."]);
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }
            }
            if (model.TeamId != null)
            {
                if (!_permissionService.HasAccessToTeam(userId, (int)model.TeamId))
                {
                    response.Message = "Model has problem.";
                    response.AddError(_localizer["You do not have access to selected team."]);
                    return StatusCode(StatusCodes.Status403Forbidden, response);
                }
                foreach (var user in model.Users)
                {
                    if (!_permissionService.HasAccessToTeam(user, (int)model.TeamId))
                    {
                        response.Message = "Model has problem.";
                        response.AddError(_localizer["Invalid users."]);
                        return StatusCode(StatusCodes.Status400BadRequest, response);
                    }
                }
            }
            else
            {
                foreach (var project in model.ProjectIds)
                {
                    if (!_permissionService.HasAccessToProject(userId, project))
                    {
                        response.Message = "Model has problem.";
                        response.AddError(_localizer["You do not have access to project."]);
                        return StatusCode(StatusCodes.Status403Forbidden, response);
                    }
                }
                foreach (var project in model.ProjectIds)
                {
                    foreach (var user in model.Users)
                    {
                        if (!_permissionService.HasAccessToProject(user, project))
                        {
                            response.Message = "Model has problem.";
                            response.AddError(_localizer["Invalid users."]);
                            return StatusCode(StatusCodes.Status400BadRequest, response);
                        }
                    }
                }
            }

            int? selectedTeamId = null;
            bool isFirstTime = true;
            foreach (var projectId in model.ProjectIds)
            {
                var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == projectId);
                if (isFirstTime)
                    selectedTeamId = selectedProject.TeamId;
                else if (selectedProject.TeamId != selectedTeamId)
                {
                    response.Message = "Model has problem.";
                    response.AddError(_localizer["Invalid projects."]);
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }

            }
            #endregion

            var creationTime = DateTime.UtcNow;
            var newEvent = new Event()
            {
                Title = model.Title,
                Description = model.Description,
                CreatedBy = userId,
                CreatedOn = creationTime,
                ModifiedBy = userId,
                ModifiedOn = creationTime,
                IsAllDay = model.IsAllDay,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                TeamId = model.ProjectIds.Count == 0 ? model.TeamId : selectedTeamId,
                Guid = Guid.NewGuid(),
                IsDeleted = false,
            };
            _vwsDbContext.AddEvent(newEvent);
            _vwsDbContext.Save();

            AddEventProjects(newEvent.Id, model.ProjectIds);
            
            AddEventUsers(newEvent.Id, model.Users);

            #region History
            var newHistory = new EventHistory()
            {
                EventId = newEvent.Id,
                EventTime = newEvent.CreatedOn,
                EventBody = "Event created by {0}."
            };
            _vwsDbContext.AddEventHistory(newHistory);
            _vwsDbContext.Save();

            var creator = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = creator.NickName,
                    ProfileImageGuid = creator.ProfileImageGuid,
                    UserId = creator.UserId
                }),
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            AddTeamProjectsEventCreationHistory(_calenderManager.GetEventTeam(newEvent.Id), _calenderManager.GetEventProjects(newEvent.Id), newEvent.CreatedOn, newEvent.Title);

            var users = _permissionService.GetUsersHasAccessToEvent(newEvent.Id);
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> invited you to <b>«{1}»</b> event.";
            string[] arguments = { LoggedInNickName, newEvent.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Event Create", arguments);

            response.Value = new EventResponseModel()
            {
                Id = newEvent.Id,
                Title = newEvent.Title,
                Description = newEvent.Description,
                IsAllDay = newEvent.IsAllDay,
                CreatedBy = _userService.GetUser(newEvent.CreatedBy),
                CreatedOn = newEvent.CreatedOn,
                ModifiedBy = _userService.GetUser(newEvent.CreatedBy),
                ModifiedOn = newEvent.ModifiedOn,
                StartTime = newEvent.StartTime,
                EndTime = newEvent.EndTime,
                Team = _calenderManager.GetEventTeam(newEvent.Id),
                Projects = _calenderManager.GetEventProjects(newEvent.Id),
                Users = await _calenderManager.GetEventUsers(newEvent.Id)
            };
            response.Message = "Event created successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateTitle")]
        public async Task<IActionResult> UpdateTitle(int id,[FromBody] StringModel model)
        {
            string newTitle = model.Value;
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            if (String.IsNullOrEmpty(newTitle) || newTitle.Length > 500)
            {
                response.Message = "Model has problem.";
                response.AddError(_localizer["Title can not be empty or have more than 500 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedEvent = _vwsDbContext.Events.FirstOrDefault(_event => _event.Id == id);
            if (selectedEvent == null || selectedEvent.IsDeleted)
            {
                response.Message = "Event not found.";
                response.AddError(_localizer["Event not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToEvent(userId, id))
            {
                response.Message = "Access denied.";
                response.AddError(_localizer["You do not have access to this event."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var lastTitle = selectedEvent.Title;
            selectedEvent.Title = newTitle;
            selectedEvent.ModifiedBy = userId;
            selectedEvent.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newHistory = new EventHistory()
            {
                EventId = selectedEvent.Id,
                EventTime = selectedEvent.CreatedOn,
                EventBody = "{0} updated title from {1} to {2}."
            };
            _vwsDbContext.AddEventHistory(newHistory);
            _vwsDbContext.Save();

            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(_userService.GetUser(userId)),
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastTitle,
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedEvent.Title,
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = _permissionService.GetUsersHasAccessToEvent(selectedEvent.Id);
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated title of event <b>«{1}»</b>, to <b>«{2}»</b>.";
            string[] arguments = { LoggedInNickName, lastTitle, selectedEvent.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Event Update", arguments);

            response.Message = "Event title updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateDescription")]
        public async Task<IActionResult> UpdateDescription(int id, [FromBody] StringModel model)
        {
            string newDescription = model.Value;
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            if (!String.IsNullOrEmpty(newDescription) && newDescription.Length > 2500)
            {
                response.Message = "Model has problem.";
                response.AddError(_localizer["Description can not have more than 2500 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedEvent = _vwsDbContext.Events.FirstOrDefault(_event => _event.Id == id);
            if (selectedEvent == null || selectedEvent.IsDeleted)
            {
                response.Message = "Event not found.";
                response.AddError(_localizer["Event not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToEvent(userId, id))
            {
                response.Message = "Access denied.";
                response.AddError(_localizer["You do not have access to this event."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var lastDescription = selectedEvent.Description;
            selectedEvent.Title = newDescription;
            selectedEvent.ModifiedBy = userId;
            selectedEvent.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newHistory = new EventHistory()
            {
                EventId = selectedEvent.Id,
                EventTime = selectedEvent.CreatedOn,
                EventBody = "{0} updated description to {1}."
            };
            _vwsDbContext.AddEventHistory(newHistory);
            _vwsDbContext.Save();

            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(_userService.GetUser(userId)),
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedEvent.Description,
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = _permissionService.GetUsersHasAccessToEvent(selectedEvent.Id);
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated description of your event with title <b>«{1}»</b> from <b>«{2}»</b> to <b>«{3}»</b>.";
            string[] arguments = { LoggedInNickName, selectedEvent.Title, lastDescription, selectedEvent.Description };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Event Update", arguments);

            response.Message = "Event description updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateStartTime")]
        public async Task<IActionResult> UpdateStartTime(int id, DateTime newStartTime)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            var selectedEvent = _vwsDbContext.Events.FirstOrDefault(_event => _event.Id == id);
            if (selectedEvent == null || selectedEvent.IsDeleted)
            {
                response.Message = "Event not found.";
                response.AddError(_localizer["Event not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToEvent(userId, id))
            {
                response.Message = "Access denied.";
                response.AddError(_localizer["You do not have access to this event."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedEvent.EndTime < newStartTime)
            {
                response.Message = "Model has problem.";
                response.AddError(_localizer["Start time should be before end time."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var lastStartTime = selectedEvent.StartTime;
            selectedEvent.StartTime = newStartTime;
            selectedEvent.ModifiedBy = userId;
            selectedEvent.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newHistory = new EventHistory()
            {
                EventId = selectedEvent.Id,
                EventTime = selectedEvent.CreatedOn,
                EventBody = "{0} updated start time from {1} to {2}."
            };
            _vwsDbContext.AddEventHistory(newHistory);
            _vwsDbContext.Save();

            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(_userService.GetUser(userId)),
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastStartTime.ToString(),
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedEvent.StartTime.ToString(),
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = _permissionService.GetUsersHasAccessToEvent(selectedEvent.Id);
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated start time of your event with title <b>«{1}»</b> from <b>«{2}»</b> to <b>«{3}»</b>.";
            string[] arguments = { LoggedInNickName, selectedEvent.Title, lastStartTime.ToString(), selectedEvent.StartTime.ToString() };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Event Update", arguments);

            response.Message = "Event start time updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateEndTime")]
        public async Task<IActionResult> UpdateEndTime(int id, DateTime newEndTime)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            var selectedEvent = _vwsDbContext.Events.FirstOrDefault(_event => _event.Id == id);
            if (selectedEvent == null || selectedEvent.IsDeleted)
            {
                response.Message = "Event not found.";
                response.AddError(_localizer["Event not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToEvent(userId, id))
            {
                response.Message = "Access denied.";
                response.AddError(_localizer["You do not have access to this event."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedEvent.StartTime > newEndTime)
            {
                response.Message = "Model has problem.";
                response.AddError(_localizer["Start time should be before end time."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var lastEndTime = selectedEvent.EndTime;
            selectedEvent.EndTime = newEndTime;
            selectedEvent.ModifiedBy = userId;
            selectedEvent.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newHistory = new EventHistory()
            {
                EventId = selectedEvent.Id,
                EventTime = selectedEvent.CreatedOn,
                EventBody = "{0} updated end time from {1} to {2}."
            };
            _vwsDbContext.AddEventHistory(newHistory);
            _vwsDbContext.Save();

            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(_userService.GetUser(userId)),
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastEndTime.ToString(),
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedEvent.EndTime.ToString(),
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = _permissionService.GetUsersHasAccessToEvent(selectedEvent.Id);
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated end time of your event with title <b>«{1}»</b> from <b>«{2}»</b> to <b>«{3}»</b>.";
            string[] arguments = { LoggedInNickName, selectedEvent.Title, lastEndTime.ToString(), selectedEvent.EndTime.ToString() };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Event Update", arguments);

            response.Message = "Event end time updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateIsAllDay")]
        public async Task<IActionResult> UpdateIsAllDay(int id, bool newIsAllDay)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            var selectedEvent = _vwsDbContext.Events.FirstOrDefault(_event => _event.Id == id);
            if (selectedEvent == null || selectedEvent.IsDeleted)
            {
                response.Message = "Event not found.";
                response.AddError(_localizer["Event not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToEvent(userId, id))
            {
                response.Message = "Access denied.";
                response.AddError(_localizer["You do not have access to this event."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedEvent.IsAllDay = newIsAllDay;
            selectedEvent.ModifiedBy = userId;
            selectedEvent.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newHistory = new EventHistory()
            {
                EventId = selectedEvent.Id,
                EventTime = selectedEvent.CreatedOn,
                EventBody = "{0}" + (newIsAllDay ? " enabled" : " disabled") + " event for all days."
            };
            _vwsDbContext.AddEventHistory(newHistory);
            _vwsDbContext.Save();

            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(_userService.GetUser(userId)),
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = _permissionService.GetUsersHasAccessToEvent(selectedEvent.Id);
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b>" + (newIsAllDay ? " enabled" : " disabled") + " for all days in your event with title <b>«{1}»</b>.";
            string[] arguments = { LoggedInNickName, selectedEvent.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Event Update", arguments);

            response.Message = "Event IsAllDay updated successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getAll")]
        public async Task<IEnumerable<EventResponseModel>> GetAll()
        {
            var result = new List<EventResponseModel>();

            var userId = LoggedInUserId.Value;

            var userEvents = _vwsDbContext.EventUsers.Include(eventUser => eventUser.Event)
                                                     .Where(eventUser => eventUser.UserProfileId == userId && !eventUser.Event.IsDeleted && !eventUser.IsDeleted)
                                                     .Select(eventUser => eventUser.Event)
                                                     .ToList();

            userEvents.AddRange(_vwsDbContext.Events.Where(_event => _event.CreatedBy == userId && !_event.IsDeleted));

            userEvents = userEvents.Distinct().ToList();

            foreach (var userEvent in userEvents)
            {
                result.Add(new EventResponseModel()
                {
                    Id = userEvent.Id,
                    Title = userEvent.Title,
                    Description = userEvent.Description,
                    IsAllDay = userEvent.IsAllDay,
                    CreatedBy = _userService.GetUser(userEvent.CreatedBy),
                    CreatedOn = userEvent.CreatedOn,
                    ModifiedBy = _userService.GetUser(userEvent.ModifiedBy),
                    ModifiedOn = userEvent.ModifiedOn,
                    StartTime = userEvent.StartTime,
                    EndTime = userEvent.EndTime,
                    Team = _calenderManager.GetEventTeam(userEvent.Id),
                    Projects = _calenderManager.GetEventProjects(userEvent.Id),
                    Users = await _calenderManager.GetEventUsers(userEvent.Id)
                });
            }

            return result;
        }

        [HttpGet]
        [Authorize]
        [Route("getNumberOfTaksAndEvent")]
        public IActionResult GetNumberOfTaksAndEvent(DateTime from, DateTime to)
        {
            var response = new ResponseModel<Object>();
            var userId = LoggedInUserId.Value;

            if (from > to)
            {
                response.Message = "Invalid period";
                response.AddError(_localizer["From should be before To."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTasks = _vwsDbContext.GeneralTasks.Where(task => ((task.StartDate != null && task.StartDate > from && task.StartDate < to) ||
                                                                         (task.EndDate != null && task.EndDate > from && task.EndDate < to)) &&
                                                                         !task.IsDeleted && !task.IsArchived)
                                                          .ToList();
            var userTasks = _taskManager.GetUserTasks(userId);

            var selectedEventsCount = _vwsDbContext.EventUsers.Include(eventUser => eventUser.Event)
                                                              .Where(eventUser => eventUser.UserProfileId == userId &&
                                                                                  eventUser.Event.CreatedBy != userId &&
                                                                                  !eventUser.Event.IsDeleted && !eventUser.IsDeleted &&
                                                                                  eventUser.Event.StartTime > from && eventUser.Event.StartTime < to &&
                                                                                  eventUser.Event.EndTime > from && eventUser.Event.EndTime < to)
                                                              .Count();

            selectedEventsCount += _vwsDbContext.Events.Where(_event => _event.CreatedBy == userId &&
                                                                        !_event.IsDeleted && !_event.IsDeleted &&
                                                                        _event.StartTime > from && _event.StartTime < to &&
                                                                        _event.EndTime > from && _event.EndTime < to)
                                                        .Count();

            response.Value = new { NumberOfTasks = selectedTasks.Intersect(userTasks).Count(), NumberOfEvents = selectedEventsCount };
            response.Message = "Number of task and event in wanted period are returned.";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("GetEventsInPeriod")]
        public async Task<IActionResult> GetEventsInPeriod(DateTime from, DateTime to)
        {
            var response = new ResponseModel<List<EventResponseModel>>();
            var result = new List<EventResponseModel>();
            var userId = LoggedInUserId.Value;

            if (from > to)
            {
                response.Message = "Invalid period";
                response.AddError(_localizer["From should be before To."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var userEvents = _vwsDbContext.EventUsers.Include(eventUser => eventUser.Event)
                                                     .Where(eventUser => eventUser.UserProfileId == userId && !eventUser.Event.IsDeleted && !eventUser.IsDeleted &&
                                                                         eventUser.Event.StartTime > from && eventUser.Event.StartTime < to &&
                                                                         eventUser.Event.EndTime > from && eventUser.Event.EndTime < to)
                                                     .Select(eventUser => eventUser.Event)
                                                     .ToList();

            userEvents.AddRange(_vwsDbContext.Events.Where(_event => _event.CreatedBy == userId && !_event.IsDeleted &&
                                                                     _event.StartTime > from && _event.StartTime < to &&
                                                                     _event.EndTime > from && _event.EndTime < to));

            userEvents = userEvents.Distinct().ToList();

            foreach (var userEvent in userEvents)
            {
                result.Add(new EventResponseModel()
                {
                    Id = userEvent.Id,
                    Title = userEvent.Title,
                    Description = userEvent.Description,
                    IsAllDay = userEvent.IsAllDay,
                    CreatedBy = _userService.GetUser(userEvent.CreatedBy),
                    CreatedOn = userEvent.CreatedOn,
                    ModifiedBy = _userService.GetUser(userEvent.ModifiedBy),
                    ModifiedOn = userEvent.ModifiedOn,
                    StartTime = userEvent.StartTime,
                    EndTime = userEvent.EndTime,
                    Team = _calenderManager.GetEventTeam(userEvent.Id),
                    Projects = _calenderManager.GetEventProjects(userEvent.Id),
                    Users = await _calenderManager.GetEventUsers(userEvent.Id)
                });
            }

            response.Value = result;
            response.Message = "Number of task and event in wanted period are returned.";
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("deleteEvent")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            var selectedEvent = _vwsDbContext.Events.FirstOrDefault(_event => _event.Id == id);
            if (selectedEvent == null || selectedEvent.IsDeleted)
            {
                response.Message = "Event not found.";
                response.AddError(_localizer["Event not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToEvent(userId, id))
            {
                response.Message = "Access denied.";
                response.AddError(_localizer["You do not have access to this event."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedEvent.IsDeleted = true;
            selectedEvent.ModifiedBy = userId;
            selectedEvent.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newHistory = new EventHistory()
            {
                EventId = selectedEvent.Id,
                EventTime = selectedEvent.CreatedOn,
                EventBody = "{0} deleted event."
            };
            _vwsDbContext.AddEventHistory(newHistory);
            _vwsDbContext.Save();

            _vwsDbContext.AddEventHistoryParameter(new EventHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(_userService.GetUser(userId)),
                EventHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = _permissionService.GetUsersHasAccessToEvent(selectedEvent.Id);
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> deleted <b>«{1}»</b> event.";
            string[] arguments = { LoggedInNickName, selectedEvent.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Event Update", arguments);

            response.Message = "Event IsAllDay updated successfully!";
            return Ok(response);
        }
        #endregion
    }
}
