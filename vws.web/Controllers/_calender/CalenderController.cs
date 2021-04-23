using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Domain._calendar;
using vws.web.Domain._team;
using vws.web.Models;
using vws.web.Models._calender;
using vws.web.Services;
using vws.web.Services._calender;

namespace vws.web.Controllers._calender
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class CalenderController : BaseController
    {
        #region Feilds
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly ICalenderManagerService _calenderManager;
        private readonly IPermissionService _permissionService;
        private readonly IStringLocalizer<CalenderController> _localizer;
        #endregion

        #region Ctor
        public CalenderController(IVWS_DbContext vwsDbContext, ICalenderManagerService calenderManager, IStringLocalizer<CalenderController> localizer, IPermissionService permissionService)
        {
            _vwsDbContext = vwsDbContext;
            _calenderManager = calenderManager;
            _localizer = localizer;
            _permissionService = permissionService;
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

            var creationTime = DateTime.Now;
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
                DeletedOn = null
            };
            _vwsDbContext.AddEvent(newEvent);
            _vwsDbContext.Save();

            foreach (var project in model.ProjectIds)
            {
                _vwsDbContext.AddEventProject(new EventProject()
                {
                    EventId = newEvent.Id,
                    ProjectId = project
                });
            }
            _vwsDbContext.Save();

            foreach (var user in model.Users)
            {
                _vwsDbContext.AddEventUser(new EventUser()
                {
                    EventId = newEvent.Id,
                    UserProfileId = user
                });
            }
            _vwsDbContext.Save();

            response.Value = new EventResponseModel()
            {
                Id = newEvent.Id,
                Title = newEvent.Title,
                Description = newEvent.Description,
                IsAllDay = newEvent.IsAllDay,
                CreatedBy = (await _vwsDbContext.GetUserProfileAsync(newEvent.CreatedBy)).NickName,
                CreatedOn = newEvent.CreatedOn,
                ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(newEvent.ModifiedBy)).NickName,
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
        public IActionResult UpdateTitle(int id, string newTitle)
        {
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
                response.AddError(_localizer["You do not have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedEvent.Title = newTitle;
            _vwsDbContext.Save();

            response.Message = "Event title updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateDescription")]
        public IActionResult UpdateDescription(int id, string newDescription)
        {
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
                response.AddError(_localizer["You do not have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedEvent.Title = newDescription;
            _vwsDbContext.Save();

            response.Message = "Event description updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateStartTime")]
        public IActionResult UpdateStartTime(int id, DateTime newStartTime)
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
                response.AddError(_localizer["You do not have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedEvent.EndTime < newStartTime)
            {
                response.Message = "Model has problem.";
                response.AddError(_localizer["Start time should be before end time."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedEvent.StartTime = newStartTime;
            _vwsDbContext.Save();

            response.Message = "Event start time updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateEndTime")]
        public IActionResult UpdateEndTime(int id, DateTime newEndTime)
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
                response.AddError(_localizer["You do not have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedEvent.StartTime > newEndTime)
            {
                response.Message = "Model has problem.";
                response.AddError(_localizer["Start time should be before end time."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedEvent.EndTime = newEndTime;
            _vwsDbContext.Save();

            response.Message = "Event end time updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateIsAllDay")]
        public IActionResult UpdateIsAllDay(int id, bool newIsAllDay)
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
                response.AddError(_localizer["You do not have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedEvent.IsAllDay = newIsAllDay;
            _vwsDbContext.Save();

            response.Message = "Event IsAllDay updated successfully!";
            return Ok(response);
        }

        //[HttpPut]
        //[Authorize]
        //[Route("updateTeamAndProject")]
        //public IActionResult UpdateTeamAndProject([FromBody] UpdateTeamAndProjectModel model)
        #endregion
    }
}
