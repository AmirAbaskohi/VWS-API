using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using vws.web.Domain;
using vws.web.Domain._task;
using vws.web.Hubs;
using vws.web.Models;
using vws.web.Services;

namespace vws.web.Controllers._task
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class TimeController : BaseController
    {
        #region Feilds
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly IPermissionService _permissionService;
        private readonly IStringLocalizer<TimeController> _localizer;
        private readonly IHubContext<ChatHub, IChatHub> _hub;
        #endregion

        #region Ctor
        public TimeController(IVWS_DbContext vwsDbContext, IPermissionService permissionService,
                              IStringLocalizer<TimeController> localizer, IHubContext<ChatHub, IChatHub> hub)
        {
            _vwsDbContext = vwsDbContext;
            _permissionService = permissionService;
            _localizer = localizer;
            _hub = hub;
        }
        #endregion

        #region PrivateMethods
        private void DeletePausedTimeTrack(long taskId)
        {
            var selectedPausedTimeTrack = _vwsDbContext.TimeTrackPauses.FirstOrDefault(timeTrackPause => timeTrackPause.GeneralTaskId == taskId &&
                                                                                                         timeTrackPause.UserProfileId == LoggedInUserId.Value);

            if (selectedPausedTimeTrack != null)
            {
                _vwsDbContext.DeleteTimeTrackPause(selectedPausedTimeTrack);
                _vwsDbContext.Save();
            }
        }
        #endregion

        #region TimeTrackAPIS
        [HttpPost]
        [Authorize]
        [Route("start")]
        public IActionResult StartTimeTrack(long taskId)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();

            var selectedTask = _vwsDbContext.GeneralTasks.FirstOrDefault(task => task.Id == taskId);
            if (selectedTask == null || selectedTask.IsDeleted || selectedTask.IsArchived)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, taskId))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            bool hasUnfinishedTimeTrack = _vwsDbContext.TimeTracks.Any(timeTrack => timeTrack.GeneralTaskId == taskId && timeTrack.UserProfileId == userId &&
                                                                                    timeTrack.EndDate == null);
            if (hasUnfinishedTimeTrack)
            {
                response.Message = "User has already started time track.";
                return Ok(response);
            }

            DeletePausedTimeTrack(taskId);

            var newTimeTrack = new TimeTrack()
            {
                GeneralTaskId = taskId,
                UserProfileId = userId,
                StartDate = DateTime.Now,
                EndDate = null,
                TotalTimeInMinutes = null
            };
            _vwsDbContext.AddTimeTrack(newTimeTrack);
            _vwsDbContext.Save();

            if (UserHandler.ConnectedIds.Keys.Contains(userId.ToString()))
                UserHandler.ConnectedIds[userId.ToString()]
                           .ConnectionIds
                           .ForEach(connectionId => _hub.Clients.Client(connectionId)
                                                                .ReceiveStartTime(newTimeTrack.GeneralTaskId, newTimeTrack.StartDate));

            response.Message = "Time tracking started";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("pause")]
        public IActionResult PauseTimeTrack(int taskId)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();

            var selectedTask = _vwsDbContext.GeneralTasks.FirstOrDefault(task => task.Id == taskId);
            if (selectedTask == null || selectedTask.IsDeleted || selectedTask.IsArchived)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, taskId))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var unfinishedTimeTrack = _vwsDbContext.TimeTracks.FirstOrDefault(timeTrack => timeTrack.GeneralTaskId == taskId &&
                                                                                           timeTrack.UserProfileId == userId &&
                                                                                           timeTrack.EndDate == null);
            if (unfinishedTimeTrack == null)
            {
                response.Message = "User does not have started time track.";
                return Ok(response);
            }

            DeletePausedTimeTrack(taskId);

            unfinishedTimeTrack.EndDate = DateTime.Now;
            unfinishedTimeTrack.TotalTimeInMinutes = (unfinishedTimeTrack.EndDate.Value - unfinishedTimeTrack.StartDate).TotalMinutes;

            var newTimeTrackPause = new TimeTrackPause()
            {
                GeneralTaskId = unfinishedTimeTrack.GeneralTaskId,
                TimeTrackId = unfinishedTimeTrack.Id,
                UserProfileId = unfinishedTimeTrack.UserProfileId
            };
            _vwsDbContext.AddTimeTrackPause(newTimeTrackPause);
            _vwsDbContext.Save();

            if (UserHandler.ConnectedIds.Keys.Contains(userId.ToString()))
                UserHandler.ConnectedIds[userId.ToString()]
                           .ConnectionIds
                           .ForEach(connectionId => _hub.Clients.Client(connectionId)
                                                                .ReceivePauseTime(unfinishedTimeTrack.GeneralTaskId, unfinishedTimeTrack.StartDate, unfinishedTimeTrack.EndDate.Value, unfinishedTimeTrack.TotalTimeInMinutes.Value));

            response.Message = "Time tracking paused";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("stop")]
        public IActionResult StopTimeTrack(int taskId)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();

            var selectedTask = _vwsDbContext.GeneralTasks.FirstOrDefault(task => task.Id == taskId);
            if (selectedTask == null || selectedTask.IsDeleted || selectedTask.IsArchived)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, taskId))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var unfinishedTimeTrack = _vwsDbContext.TimeTracks.FirstOrDefault(timeTrack => timeTrack.GeneralTaskId == taskId &&
                                                                                           timeTrack.UserProfileId == userId &&
                                                                                           timeTrack.EndDate == null);

            var pausedTimeTrack = _vwsDbContext.TimeTrackPauses.Include(timeTrackPause => timeTrackPause.TimeTrack)
                                                               .FirstOrDefault(timeTrackPause => timeTrackPause.GeneralTaskId == taskId &&
                                                                                                 timeTrackPause.UserProfileId == LoggedInUserId.Value);

            if (pausedTimeTrack == null && unfinishedTimeTrack == null)
            {
                response.Message = "Nothing to stop";
                response.AddError(_localizer["Nothing to stop."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (unfinishedTimeTrack != null)
            {
                unfinishedTimeTrack.EndDate = DateTime.Now;
                unfinishedTimeTrack.TotalTimeInMinutes = (unfinishedTimeTrack.EndDate.Value - unfinishedTimeTrack.StartDate).TotalMinutes;
                _vwsDbContext.Save();
            }
            DeletePausedTimeTrack(taskId);

            var wantedTimeTrack = unfinishedTimeTrack == null ? pausedTimeTrack.TimeTrack : unfinishedTimeTrack;

            if (UserHandler.ConnectedIds.Keys.Contains(userId.ToString()))
                UserHandler.ConnectedIds[userId.ToString()]
                           .ConnectionIds
                           .ForEach(connectionId => _hub.Clients.Client(connectionId)
                                                                .ReceiveStopTime(wantedTimeTrack.GeneralTaskId, wantedTimeTrack.StartDate, wantedTimeTrack.EndDate.Value, wantedTimeTrack.TotalTimeInMinutes.Value));

            response.Message = "Time tracking stoped";
            return Ok(response);
        }
        #endregion
    }
}
