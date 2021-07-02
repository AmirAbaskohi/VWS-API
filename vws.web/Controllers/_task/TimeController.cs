using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using vws.web.Domain;
using vws.web.Domain._task;
using vws.web.Enums;
using vws.web.Hubs;
using vws.web.Models;
using vws.web.Models._task;
using vws.web.Services;
using vws.web.Services._task;

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
        private readonly ITaskManagerService _taskManager;
        private readonly IUserService _userService;
        #endregion

        #region Ctor
        public TimeController(IVWS_DbContext vwsDbContext, IPermissionService permissionService,
                              IStringLocalizer<TimeController> localizer, IHubContext<ChatHub, IChatHub> hub,
                              ITaskManagerService taskManager, IUserService userService)
        {
            _vwsDbContext = vwsDbContext;
            _permissionService = permissionService;
            _localizer = localizer;
            _hub = hub;
            _taskManager = taskManager;
            _userService = userService;
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

        private void DeletePausedTimeTrackSpentTime(long taskId)
        {
            var selectedPausedTimeTrackSpentTime = _vwsDbContext.TimeTrackPausedSpentTimes.FirstOrDefault(timeTrackPauseSpentTime => timeTrackPauseSpentTime.GeneralTaskId == taskId &&
                                                                                                                            timeTrackPauseSpentTime.UserProfileId == LoggedInUserId.Value);

            if (selectedPausedTimeTrackSpentTime != null)
            {
                _vwsDbContext.DeleteTimeTrackPausedSpentTime(selectedPausedTimeTrackSpentTime);
                _vwsDbContext.Save();
            }

        }

        private double RecordPausedTimeTrackSpentAndReturn(int taskId, double totalTimeInMinutes)
        {
            var selectedPausedTimeTrackSpentTime = _vwsDbContext.TimeTrackPausedSpentTimes.FirstOrDefault(timeTrackPauseSpentTime => timeTrackPauseSpentTime.GeneralTaskId == taskId && timeTrackPauseSpentTime.UserProfileId == LoggedInUserId.Value);

            if (selectedPausedTimeTrackSpentTime == null)
            {
                _vwsDbContext.AddTimeTrackPausedSpentTime(new TimeTrackPausedSpentTime()
                {
                    GeneralTaskId = taskId,
                    UserProfileId = LoggedInUserId.Value,
                    TotalTimeInMinutes = totalTimeInMinutes
                });
                _vwsDbContext.Save();
                return totalTimeInMinutes;
            }
            else
            {
                selectedPausedTimeTrackSpentTime.TotalTimeInMinutes += totalTimeInMinutes;
                _vwsDbContext.Save();
                return selectedPausedTimeTrackSpentTime.TotalTimeInMinutes;
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
                StartDate = DateTime.UtcNow,
                EndDate = null,
                TotalTimeInMinutes = null
            };
            _vwsDbContext.AddTimeTrack(newTimeTrack);
            _vwsDbContext.Save();

            var pausedTimeTrackSpentTime = _vwsDbContext.TimeTrackPausedSpentTimes.FirstOrDefault(ttpst => ttpst.GeneralTaskId == newTimeTrack.GeneralTaskId && ttpst.UserProfileId == userId);
            if (UserHandler.ConnectedIds.Keys.Contains(userId.ToString()))
                UserHandler.ConnectedIds[userId.ToString()]
                           .ConnectionIds
                           .ForEach(async connectionId => _hub.Clients.Client(connectionId)
                                                                .ReceiveStartTime(new FullRunningTaskResponseModel() 
                                                                {
                                                                    Id = selectedTask.Id,
                                                                    Title = selectedTask.Title,
                                                                    Description = selectedTask.Description,
                                                                    StartDate = selectedTask.StartDate,
                                                                    EndDate = selectedTask.EndDate,
                                                                    CreatedOn = selectedTask.CreatedOn,
                                                                    ModifiedOn = selectedTask.ModifiedOn,
                                                                    CreatedBy = _userService.GetUser(selectedTask.CreatedBy),
                                                                    ModifiedBy = _userService.GetUser(selectedTask.ModifiedBy),
                                                                    Guid = selectedTask.Guid,
                                                                    PriorityId = selectedTask.TaskPriorityId,
                                                                    PriorityTitle = _localizer[((SeedDataEnum.TaskPriority)selectedTask.TaskPriorityId).ToString()],
                                                                    UsersAssignedTo = _taskManager.GetAssignedTo(selectedTask.Id),
                                                                    ProjectId = selectedTask.ProjectId,
                                                                    TeamId = selectedTask.TeamId,
                                                                    TeamName = selectedTask.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == selectedTask.TeamId).Name,
                                                                    ProjectName = selectedTask.ProjectId == null ? null : _vwsDbContext.Projects.FirstOrDefault(project => project.Id == selectedTask.ProjectId).Name,
                                                                    StatusId = selectedTask.TaskStatusId,
                                                                    StatusTitle = _vwsDbContext.TaskStatuses.FirstOrDefault(statuse => statuse.Id == selectedTask.TaskStatusId).Title,
                                                                    CheckLists = _taskManager.GetCheckLists(selectedTask.Id),
                                                                    Tags = _taskManager.GetTaskTags(selectedTask.Id),
                                                                    Comments = await _taskManager.GetTaskComments(selectedTask.Id),
                                                                    Attachments = _taskManager.GetTaskAttachments(selectedTask.Id),
                                                                    IsUrgent = selectedTask.IsUrgent,
                                                                    TimeTrackStartDate = newTimeTrack.StartDate,
                                                                    IsPaused = false,
                                                                    TotalTimeInMinutes = pausedTimeTrackSpentTime == null ? 0 : pausedTimeTrackSpentTime.TotalTimeInMinutes
                                                                }));

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

            unfinishedTimeTrack.EndDate = DateTime.UtcNow;
            var totalTimeInMinutes = (unfinishedTimeTrack.EndDate.Value - unfinishedTimeTrack.StartDate).TotalMinutes;
            unfinishedTimeTrack.TotalTimeInMinutes = totalTimeInMinutes;

            var newTimeTrackPause = new TimeTrackPause()
            {
                GeneralTaskId = unfinishedTimeTrack.GeneralTaskId,
                TimeTrackId = unfinishedTimeTrack.Id,
                UserProfileId = unfinishedTimeTrack.UserProfileId,
                TotalTimeInMinutes = RecordPausedTimeTrackSpentAndReturn(taskId, totalTimeInMinutes)
            };
            _vwsDbContext.AddTimeTrackPause(newTimeTrackPause);
            _vwsDbContext.Save();

            if (UserHandler.ConnectedIds.Keys.Contains(userId.ToString()))
                UserHandler.ConnectedIds[userId.ToString()]
                           .ConnectionIds
                           .ForEach(connectionId => _hub.Clients.Client(connectionId)
                                                                .ReceivePauseTime(unfinishedTimeTrack.GeneralTaskId, unfinishedTimeTrack.StartDate, unfinishedTimeTrack.EndDate.Value, unfinishedTimeTrack.TotalTimeInMinutes.Value, newTimeTrackPause.TotalTimeInMinutes));

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
                unfinishedTimeTrack.EndDate = DateTime.UtcNow;
                unfinishedTimeTrack.TotalTimeInMinutes = (unfinishedTimeTrack.EndDate.Value - unfinishedTimeTrack.StartDate).TotalMinutes;
                _vwsDbContext.Save();
            }
            DeletePausedTimeTrack(taskId);
            DeletePausedTimeTrackSpentTime(taskId);

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

        #region WorkLoadAPIS
        [HttpGet]
        [Authorize]
        [Route("getWorkLoad")]
        public IActionResult GetWorkLoad(long taskId)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel<List<UserSpentTimeModel>>();
            var result = new List<UserSpentTimeModel>();

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

            var taskUsers = _taskManager.GetAssignedTo(taskId);
            taskUsers.Add(_userService.GetUser(selectedTask.CreatedBy));
            taskUsers = taskUsers.Distinct().ToList();

            foreach (var taskUser in taskUsers)
            {
                double totalTime = 0;
                var times = _vwsDbContext.TimeTracks.Where(timeTrack => timeTrack.GeneralTaskId == taskId);
                bool isActive = true;
                foreach (var time in times)
                {
                    if (time.TotalTimeInMinutes != null)
                        totalTime += (double)time.TotalTimeInMinutes;
                    else
                    {
                        totalTime += (DateTime.UtcNow - time.StartDate).TotalMinutes;
                        isActive = false;
                    }
                }

                result.Add(new UserSpentTimeModel()
                {
                    IsFinished = isActive,
                    TotalTimeInMinutes = totalTime,
                    User = taskUser
                });
            }

            response.Value = result;
            response.Message = "Returned workload";
            return Ok(response);
        }
        #endregion
    }
}
