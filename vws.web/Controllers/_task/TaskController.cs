using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._task;
using vws.web.Enums;
using vws.web.Models;
using vws.web.Models._task;
using vws.web.Services;

namespace vws.web.Controllers._task
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class TaskController : BaseController
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IStringLocalizer<TaskController> localizer;
        private readonly IVWS_DbContext vwsDbContext;
        private readonly IPermissionService permissionService;

        public TaskController(UserManager<ApplicationUser> _userManager, IStringLocalizer<TaskController> _localizer,
            IVWS_DbContext _vwsDbContext, IPermissionService _permissionService)
        {
            userManager = _userManager;
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
            permissionService = _permissionService;
        }

        private async Task<List<UserModel>> GetAssignedTo(long taskId)
        {
            var result = new List<UserModel>();
            var assignedUsers = vwsDbContext.TaskAssigns.Include(taskAssign => taskAssign.UserProfile)
                                                        .Where(taskAssign => taskAssign.GeneralTaskId == taskId && !taskAssign.IsDeleted)
                                                        .Select(taskAssign => taskAssign.UserProfile);

            foreach (var user in assignedUsers)
            {
                result.Add(new UserModel()
                {
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId,
                    UserName = (await userManager.FindByIdAsync(user.UserId.ToString())).UserName
                });
            }

            return result;
        }

        private List<GeneralTask> GetUserTasks(Guid userId)
        {
            var assignedTasks = vwsDbContext.TaskAssigns.Include(taskAssign => taskAssign.GeneralTask)
                                                        .Where(taskAssign => taskAssign.UserProfileId == userId && !taskAssign.IsDeleted && taskAssign.CreatedBy != userId)
                                                        .Select(taskAssign => taskAssign.GeneralTask)
                                                        .ToList();

            assignedTasks.AddRange(vwsDbContext.GeneralTasks.Where(task => task.CreatedBy == userId && !task.IsDeleted));

            return assignedTasks;
        }

        private async Task AddUsersToTask(long taskId, List<Guid> users)
        {
            var creationTime = DateTime.Now;
            foreach (var user in users)
            {
                await vwsDbContext.AddTaskAssignAsync(new TaskAssign()
                {
                    CreatedBy = LoggedInUserId.Value,
                    IsDeleted = false,
                    CreatedOn = creationTime,
                    UserProfileId = user,
                    Guid = Guid.NewGuid(),
                    GeneralTaskId = taskId
                });
            }
            vwsDbContext.Save();
        }

        private async Task UpdateTaskUsers(long taskId, List<Guid> users)
        {
            var taskAssigns = vwsDbContext.TaskAssigns.Where(taskAssign => taskAssign.GeneralTaskId == taskId && !taskAssign.IsDeleted)
                                                      .Select(taskAssign => taskAssign.UserProfileId).ToList();

            var actionTime = DateTime.Now;

            var shouldBeDeleted = taskAssigns.Except(users).ToList();
            var shouldBeAdded = users.Except(taskAssigns).ToList();

            foreach (var user in shouldBeDeleted)
            {
                var selectedAssignTask = vwsDbContext.TaskAssigns.FirstOrDefault(taskAssign => taskAssign.UserProfileId == user && taskAssign.GeneralTaskId == taskId && !taskAssign.IsDeleted);
                selectedAssignTask.IsDeleted = true;
                selectedAssignTask.DeletedOn = actionTime;
                selectedAssignTask.DeletedBy = LoggedInUserId.Value;
            }

            vwsDbContext.Save();

            await AddUsersToTask(taskId, shouldBeAdded.ToList());
        }

        private List<Guid> GetUsersCanBeAddedToTask(int? teamId, int? projectId)
        {
            if (teamId != null && projectId != null)
                teamId = null;

            if (teamId != null)
                return permissionService.GetUsersHaveAccessToTeam((int)teamId);
            else if (projectId != null)
                return permissionService.GetUsersHaveAccessToProject((int)projectId);

            return new List<Guid>();
        }

        private List<TaskStatusResponseModel> GetTaskStatuses(int? projectId, int? teamId)
        {
            if (teamId != null)
            {
                return vwsDbContext.TaskStatuses.Where(status => status.TeamId == teamId)
                                                          .OrderBy(status => status.EvenOrder)
                                                          .Select(status => new TaskStatusResponseModel() { Id = status.Id, Title = status.Title })
                                                          .ToList();
            }
            else if (projectId != null)
            {
                return vwsDbContext.TaskStatuses.Where(status => status.ProjectId == projectId)
                                                          .OrderBy(status => status.EvenOrder)
                                                          .Select(status => new TaskStatusResponseModel() { Id = status.Id, Title = status.Title })
                                                          .ToList();
            }
            return vwsDbContext.TaskStatuses.Where(status => status.UserProfileId == LoggedInUserId.Value)
                                                      .OrderBy(status => status.EvenOrder)
                                                      .Select(status => new TaskStatusResponseModel() { Id = status.Id, Title = status.Title })
                                                      .ToList();
        }

        [HttpPost]
        [Authorize]
        [Route("create")]
        public async Task<IActionResult> CreateTask([FromBody] TaskModel model)
        {
            var response = new ResponseModel<TaskResponseModel>();

            if (!String.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
            {
                response.Message = "Task model data has problem.";
                response.AddError(localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Title.Length > 500)
            {
                response.Message = "Task model data has problem.";
                response.AddError(localizer["Length of title is more than 500 characters."]);
            }
            if (model.StartDate.HasValue && model.EndDate.HasValue)
            {
                if (model.StartDate > model.EndDate)
                {
                    response.Message = "Task model data has problem.";
                    response.AddError(localizer["Start Date should be before End Date."]);
                }
            }
            if (!Enum.IsDefined(typeof(SeedDataEnum.TaskPriority), model.PriorityId))
            {
                response.Message = "Task model data has problem.";
                response.AddError(localizer["Priority id is not defined."]);
            }
            if (response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            if (model.TeamId != null && model.ProjectId != null)
                model.TeamId = null;

            Guid userId = LoggedInUserId.Value;

            #region CheckTeamAndProjectExistance
            if (model.ProjectId != null && !vwsDbContext.Projects.Any(p => p.Id == model.ProjectId && !p.IsDeleted))
            {
                response.Message = "Project not found";
                response.AddError(localizer["Project not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (model.TeamId != null && !vwsDbContext.Teams.Any(t => t.Id == model.TeamId && !t.IsDeleted))
            {
                response.Message = "Team not found";
                response.AddError(localizer["Team not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            #endregion

            #region CheckTeamAndProjectAccess
            if (model.ProjectId != null && !permissionService.HasAccessToProject(userId, (int)model.ProjectId))
            {
                response.Message = "Project access denied";
                response.AddError(localizer["You do not have access to project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            if (model.TeamId != null && !permissionService.HasAccessToTeam(userId, (int)model.TeamId))
            {
                response.Message = "Team access denied";
                response.AddError(localizer["You do not have access to team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            DateTime creationTime = DateTime.Now;

            if (GetUsersCanBeAddedToTask(model.TeamId, model.ProjectId).Intersect(model.Users).Count() != model.Users.Count)
            {
                response.Message = "Users do not have access";
                response.AddError(localizer["Some of users you want to add do not have access to team or project."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var statuses = GetTaskStatuses(model.ProjectId, model.TeamId).Select(status => status.Id);
            if (model.StatusId == null)
            {
                if (statuses.Count() != 0)
                    model.StatusId = statuses.First();
                else
                    model.StatusId = 0;
            }

            if (!statuses.Contains((int)model.StatusId))
            {
                response.Message = "Invalid status";
                response.AddError(localizer["Invalid status."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var newTask = new GeneralTask()
            {
                Title = model.Title,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                CreatedBy = userId,
                ModifiedBy = userId,
                CreatedOn = creationTime,
                ModifiedOn = creationTime,
                Guid = Guid.NewGuid(),
                TaskPriorityId = model.PriorityId,
                TeamId = model.TeamId,
                TaskStatusId = (int)model.StatusId
            };

            if (newTask.TeamId == null && model.ProjectId != null)
            {
                var selectedProject = vwsDbContext.Projects.FirstOrDefault(project => project.Id == model.ProjectId);
                newTask.ProjectId = model.ProjectId;
                newTask.TeamId = selectedProject.TeamId;
            }

            await vwsDbContext.AddTaskAsync(newTask);
            vwsDbContext.Save();

            await AddUsersToTask(newTask.Id, model.Users);

            var newTaskResponseModel = new TaskResponseModel()
            {
                Id = newTask.Id,
                Title = newTask.Title,
                CreatedOn = newTask.CreatedOn,
                Description = newTask.Description,
                EndDate = newTask.EndDate,
                StartDate = newTask.StartDate,
                Guid = newTask.Guid,
                ModifiedOn = newTask.ModifiedOn,
                ModifiedBy = (await userManager.FindByIdAsync(newTask.ModifiedBy.ToString())).UserName,
                CreatedBy = (await userManager.FindByIdAsync(newTask.CreatedBy.ToString())).UserName,
                PriorityId = newTask.TaskPriorityId,
                PriorityTitle = localizer[((SeedDataEnum.TaskPriority)newTask.TaskPriorityId).ToString()],
                UsersAssignedTo = await GetAssignedTo(newTask.Id),
                ProjectId = newTask.ProjectId,
                TeamId = newTask.TeamId,
                StatusId = newTask.TaskStatusId,
                StatusTitle = vwsDbContext.TaskStatuses.FirstOrDefault(statuse => statuse.Id == newTask.TaskStatusId).Title
            };

            response.Value = newTaskResponseModel;
            response.Message = "Task created successfully!";
            return Ok(response);

        }

        [HttpPut]
        [Authorize]
        [Route("update")]
        public async Task<IActionResult> UpdateTask([FromBody] UpdateTaskModel model)
        {
            var response = new ResponseModel<TaskResponseModel>();

            if (model.Description.Length > 2000)
            {
                response.Message = "Task model data has problem";
                response.AddError(localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Title.Length > 500)
            {
                response.Message = "Task model data has problem";
                response.AddError(localizer["Length of title is more than 500 characters."]);
            }
            if (model.StartDate.HasValue && model.EndDate.HasValue)
            {
                if (model.StartDate > model.EndDate)
                {
                    response.Message = "Task model data has problem";
                    response.AddError(localizer["Start Date should be before End Date."]);
                }
            }
            if (!Enum.IsDefined(typeof(SeedDataEnum.TaskPriority), model.PriorityId))
            {
                response.Message = "Task model data has problem.";
                response.AddError(localizer["Priority id is not defined."]);
            }

            if (response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            Guid userId = LoggedInUserId.Value;

            #region CheckTeamAndProjectExistance
            if (model.ProjectId != null && !vwsDbContext.Projects.Any(p => p.Id == model.ProjectId && !p.IsDeleted))
            {
                response.Message = "Project not found";
                response.AddError(localizer["Project not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (model.TeamId != null && !vwsDbContext.Teams.Any(t => t.Id == model.TeamId && !t.IsDeleted))
            {
                response.Message = "Team not found";
                response.AddError(localizer["Team not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            #endregion

            #region CheckTeamAndProjectAccess
            if (model.ProjectId != null && !permissionService.HasAccessToProject(userId, (int)model.ProjectId))
            {
                response.Message = "Project access denied";
                response.AddError(localizer["You do not have access to project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            if (model.TeamId != null && !permissionService.HasAccessToTeam(userId, (int)model.TeamId))
            {
                response.Message = "Team access denied";
                response.AddError(localizer["You do not have access to team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            if (GetUsersCanBeAddedToTask(model.TeamId, model.ProjectId).Intersect(model.Users).Count() != model.Users.Count)
            {
                response.Message = "Users do not have access";
                response.AddError(localizer["Some of users you want to add do not have access to team or project."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTask = await vwsDbContext.GetTaskAsync(model.TaskId);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (selectedTask.CreatedBy != userId)
            {
                response.Message = "Task access forbidden";
                response.AddError(localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var statuses = GetTaskStatuses(model.ProjectId, model.TeamId).Select(status => status.Id);
            if (model.StatusId == null)
            {
                if (statuses.Count() != 0)
                    model.StatusId = statuses.First();
                else
                    model.StatusId = 0;
            }
            if (!statuses.Contains((int)model.StatusId))
            {
                response.Message = "Invalid status";
                response.AddError(localizer["Invalid status."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (model.StartDate.HasValue && !(model.EndDate.HasValue) && selectedTask.EndDate.HasValue)
            {
                if (model.StartDate > selectedTask.EndDate)
                {
                    response.Message = "Task model data has problem";
                    response.AddError(localizer["Start Date should be before End Date."]);
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
            }
            if (model.EndDate.HasValue && !(model.StartDate.HasValue) && selectedTask.StartDate.HasValue)
            {
                if (model.EndDate < selectedTask.StartDate)
                {
                    response.Message = "Task model data has problem";
                    response.AddError(localizer["Start Date should be before End Date."]);
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
            }

            if (model.TeamId != null && model.ProjectId != null)
                model.TeamId = null;

            selectedTask.StartDate = model.StartDate;
            selectedTask.EndDate = model.EndDate;
            selectedTask.ModifiedBy = userId;
            selectedTask.ModifiedOn = DateTime.Now;
            selectedTask.Title = model.Title;
            selectedTask.Description = model.Description;
            selectedTask.TaskPriorityId = model.PriorityId;
            selectedTask.TeamId = model.TeamId;
            selectedTask.ProjectId = model.ProjectId;
            selectedTask.TaskStatusId = (int)model.StatusId;

            if (selectedTask.TeamId == null && model.ProjectId != null)
            {
                var selectedProject = vwsDbContext.Projects.FirstOrDefault(project => project.Id == model.ProjectId);
                selectedTask.ProjectId = model.ProjectId;
                selectedTask.TeamId = selectedProject.TeamId;
            }

            vwsDbContext.Save();

            await UpdateTaskUsers(selectedTask.Id, model.Users);

            var updatedTaskResponseModel = new TaskResponseModel()
            {
                Id = selectedTask.Id,
                Title = selectedTask.Title,
                CreatedOn = selectedTask.CreatedOn,
                Description = selectedTask.Description,
                EndDate = selectedTask.EndDate,
                StartDate = selectedTask.StartDate,
                Guid = selectedTask.Guid,
                ModifiedOn = selectedTask.ModifiedOn,
                ModifiedBy = (await userManager.FindByIdAsync(selectedTask.ModifiedBy.ToString())).UserName,
                CreatedBy = (await userManager.FindByIdAsync(selectedTask.CreatedBy.ToString())).UserName,
                PriorityId = selectedTask.TaskPriorityId,
                PriorityTitle = localizer[((SeedDataEnum.TaskPriority)selectedTask.TaskPriorityId).ToString()],
                UsersAssignedTo = await GetAssignedTo(selectedTask.Id),
                ProjectId = selectedTask.ProjectId,
                TeamId = selectedTask.TeamId,
                StatusId = selectedTask.TaskStatusId,
                StatusTitle = vwsDbContext.TaskStatuses.FirstOrDefault(statuse => statuse.Id == selectedTask.TaskStatusId).Title
            };

            response.Value = updatedTaskResponseModel;
            response.Message = "Task updated successfully!";
            return Ok(response);

        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public async Task<IEnumerable<TaskResponseModel>> GetTasks()
        {
            Guid userId = LoggedInUserId.Value;

            List<TaskResponseModel> response = new List<TaskResponseModel>();

            var userTasks = GetUserTasks(userId);
            foreach (var userTask in userTasks)
            {
                if (userTask.IsDeleted || userTask.IsArchived)
                    continue;

                response.Add(new TaskResponseModel()
                {
                    Id = userTask.Id,
                    Title = userTask.Title,
                    Description = userTask.Description,
                    StartDate = userTask.StartDate,
                    EndDate = userTask.EndDate,
                    CreatedOn = userTask.CreatedOn,
                    ModifiedOn = userTask.ModifiedOn,
                    CreatedBy = (await userManager.FindByIdAsync(userTask.CreatedBy.ToString())).UserName,
                    ModifiedBy = (await userManager.FindByIdAsync(userTask.ModifiedBy.ToString())).UserName,
                    Guid = userTask.Guid,
                    PriorityId = userTask.TaskPriorityId,
                    PriorityTitle = localizer[((SeedDataEnum.TaskPriority)userTask.TaskPriorityId).ToString()],
                    UsersAssignedTo = await GetAssignedTo(userTask.Id),
                    ProjectId = userTask.ProjectId,
                    TeamId = userTask.TeamId,
                    StatusId = userTask.TaskStatusId,
                    StatusTitle = vwsDbContext.TaskStatuses.FirstOrDefault(statuse => statuse.Id == userTask.TaskStatusId).Title
                });
            }
            return response;
        }

        [HttpGet]
        [Authorize]
        [Route("getArchived")]
        public async Task<IEnumerable<TaskResponseModel>> GetArchivedTasks()
        {
            Guid userId = LoggedInUserId.Value;

            List<TaskResponseModel> response = new List<TaskResponseModel>();

            var userTasks = GetUserTasks(userId);
            foreach (var userTask in userTasks)
            {
                if (userTask.IsArchived && !userTask.IsDeleted)
                {
                    response.Add(new TaskResponseModel()
                    {
                        Id = userTask.Id,
                        Title = userTask.Title,
                        Description = userTask.Description,
                        StartDate = userTask.StartDate,
                        EndDate = userTask.EndDate,
                        CreatedOn = userTask.CreatedOn,
                        ModifiedOn = userTask.ModifiedOn,
                        CreatedBy = (await userManager.FindByIdAsync(userTask.CreatedBy.ToString())).UserName,
                        ModifiedBy = (await userManager.FindByIdAsync(userTask.ModifiedBy.ToString())).UserName,
                        Guid = userTask.Guid,
                        PriorityId = userTask.TaskPriorityId,
                        PriorityTitle = localizer[((SeedDataEnum.TaskPriority)userTask.TaskPriorityId).ToString()],
                        UsersAssignedTo = await GetAssignedTo(userTask.Id),
                        ProjectId = userTask.ProjectId,
                        TeamId = userTask.TeamId,
                        StatusId = userTask.TaskStatusId,
                        StatusTitle = vwsDbContext.TaskStatuses.FirstOrDefault(statuse => statuse.Id == userTask.TaskStatusId).Title
                    });
                }
            }
            return response;
        }

        [HttpDelete]
        [Authorize]
        [Route("delete")]
        public async Task<IActionResult> DeleteTask(long taskId)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedTask = await vwsDbContext.GetTaskAsync(taskId);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (selectedTask.CreatedBy != userId)
            {
                response.Message = "Task access forbidden";
                response.AddError(localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedTask.IsDeleted = true;
            selectedTask.ModifiedBy = userId;
            selectedTask.ModifiedOn = DateTime.Now;

            vwsDbContext.Save();

            response.Message = "Task updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("archive")]
        public async Task<IActionResult> ArchiveTask(long taskId)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedTask = await vwsDbContext.GetTaskAsync(taskId);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (selectedTask.CreatedBy != userId)
            {
                response.Message = "Task access forbidden";
                response.AddError(localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedTask.IsArchived = true;

            vwsDbContext.Save();

            response.Message = "Task updated successfully!";
            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        [Route("assignTask")]
        public async Task<IActionResult> AssignTask([FromBody] AssignTaskModel model)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();
            var selectedUserId = new Guid(model.UserId);

            var selectedTask = await vwsDbContext.GetTaskAsync(model.TaskId);
            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.AddError(localizer["Task does not exist."]);
                response.Message = "Task not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedTask.CreatedBy != userId)
            {
                response.AddError(localizer["You don't have access to this task."]);
                response.Message = "Task access forbidden";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (!vwsDbContext.UserProfiles.Any(profile => profile.UserId == selectedUserId))
            {
                response.AddError(localizer["User does not exist."]);
                response.Message = "User not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (vwsDbContext.TaskAssigns.Any(taskAssign => taskAssign.UserProfileId == selectedUserId &&
                                              taskAssign.GeneralTaskId == model.TaskId &&
                                              taskAssign.IsDeleted == false))
            {
                response.AddError(localizer["Task is assigned to user before."]);
                response.Message = "Task assigned before";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var newTaskAssign = new TaskAssign()
            {
                Guid = Guid.NewGuid(),
                GeneralTaskId = model.TaskId,
                UserProfileId = selectedUserId,
                IsDeleted = false,
                CreatedBy = userId,
                CreatedOn = DateTime.Now
            };

            await vwsDbContext.AddTaskAssignAsync(newTaskAssign);
            vwsDbContext.Save();

            response.Message = "Task assigned successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getUsersAssignedTo")]
        public async Task<IActionResult> GetUsersAssignedTo(long id)
        {
            var response = new ResponseModel<List<UserModel>>();
            var assignedUsersList = new List<UserModel>();

            var userId = LoggedInUserId.Value;

            var selectedTask = await vwsDbContext.GetTaskAsync(id);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedTask.CreatedBy != userId)
            {
                var assignedTask = vwsDbContext.TaskAssigns.FirstOrDefault(taskAssign => taskAssign.UserProfileId == userId &&
                                                                                         taskAssign.GeneralTaskId == id &&
                                                                                         taskAssign.IsDeleted == false);

                if (assignedTask == null)
                {
                    response.Message = "Task access forbidden";
                    response.AddError(localizer["You don't have access to this task."]);
                    return StatusCode(StatusCodes.Status403Forbidden, response);
                }
            }

            assignedUsersList = await GetAssignedTo(id);

            response.Message = "Users returned successfully!";
            response.Value = assignedUsersList;
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("deleteUserAssignedTo")]
        public async Task<IActionResult> DeleteUserAssignedTo(long taskId, Guid userId)
        {
            var response = new ResponseModel();

            var selectedTask = await vwsDbContext.GetTaskAsync(taskId);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedTask.CreatedBy != LoggedInUserId.Value)
            {
                var assignedTask = vwsDbContext.TaskAssigns.FirstOrDefault(taskAssign => taskAssign.UserProfileId == LoggedInUserId.Value &&
                                                                                         taskAssign.GeneralTaskId == taskId &&
                                                                                         taskAssign.IsDeleted == false);

                if (assignedTask == null)
                {
                    response.Message = "Task access forbidden";
                    response.AddError(localizer["You don't have access to this task."]);
                    return StatusCode(StatusCodes.Status403Forbidden, response);
                }
            }

            var selectedUserAssignedTask = vwsDbContext.TaskAssigns.FirstOrDefault(taskAssign => taskAssign.UserProfileId == userId &&
                                                                                   taskAssign.GeneralTaskId == taskId &&
                                                                                   taskAssign.IsDeleted == false);

            if (selectedUserAssignedTask == null)
            {
                response.Message = "User does not have access already!";
                response.AddError(localizer["User you selected does not have access to task already."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedUserAssignedTask.IsDeleted = true;
            selectedUserAssignedTask.DeletedBy = LoggedInUserId.Value;
            selectedUserAssignedTask.DeletedOn = DateTime.Now;

            vwsDbContext.Save();

            response.Message = "User unassigned from task successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getTaskPriorities")]
        public ICollection<Object> GetTaskPriorities()
        {
            List<Object> result = new List<Object>();

            foreach (var priority in Enum.GetValues(typeof(SeedDataEnum.TaskPriority)))
                result.Add(new { Id = (byte)priority, Name = priority.ToString() });

            return result;
        }

        [HttpGet]
        [Authorize]
        [Route("getUsersCanBeAssigned")]
        public async Task<IActionResult> GetUsersCanBeAssigned(int? teamId, int? projectId)
        {
            var response = new ResponseModel<List<UserModel>>();
            var result = new List<UserModel>();

            if (projectId != null && teamId != null)
                teamId = null;

            #region CheckTeamAndProjectExistance
            if (projectId != null && !vwsDbContext.Projects.Any(p => p.Id == projectId && !p.IsDeleted))
            {
                response.Message = "Project not found";
                response.AddError(localizer["Project not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (teamId != null && !vwsDbContext.Teams.Any(t => t.Id == teamId && !t.IsDeleted))
            {
                response.Message = "Team not found";
                response.AddError(localizer["Team not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            #endregion

            #region CheckTeamAndProjectAccess
            if (projectId != null && !permissionService.HasAccessToProject(LoggedInUserId.Value, (int)projectId))
            {
                response.Message = "Project access denied";
                response.AddError(localizer["You do not have access to project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            if (teamId != null && !permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)teamId))
            {
                response.Message = "Team access denied";
                response.AddError(localizer["You do not have access to team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            var users = GetUsersCanBeAddedToTask(teamId, projectId);
            foreach (var user in users)
            {
                var selectedUser = vwsDbContext.UserProfiles.FirstOrDefault(profile => profile.UserId == user);
                result.Add(new UserModel()
                {
                    UserId = user,
                    UserName = (await userManager.FindByIdAsync(user.ToString())).UserName,
                    ProfileImageGuid = selectedUser.ProfileImageGuid
                });
            }

            response.Value = result;
            response.Message = "Users returned successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getStatuses")]
        public IActionResult GetStatuses(int? projectId, int? teamId)
        {
            var response = new ResponseModel<List<TaskStatusResponseModel>>();

            if (teamId != null && projectId != null)
                teamId = null;

            #region CheckTeamAndProjectExistance
            if (projectId != null && !vwsDbContext.Projects.Any(p => p.Id == projectId && !p.IsDeleted))
            {
                response.Message = "Project not found";
                response.AddError(localizer["Project not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (teamId != null && !vwsDbContext.Teams.Any(t => t.Id == teamId && !t.IsDeleted))
            {
                response.Message = "Team not found";
                response.AddError(localizer["Team not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            #endregion

            #region CheckTeamAndProjectAccess
            if (projectId != null && !permissionService.HasAccessToProject(LoggedInUserId.Value, (int)projectId))
            {
                response.Message = "Project access denied";
                response.AddError(localizer["You do not have access to project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            if (teamId != null && !permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)teamId))
            {
                response.Message = "Team access denied";
                response.AddError(localizer["You do not have access to team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            response.Value = GetTaskStatuses(projectId, teamId);
            response.Message = "Statuses returned successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("addStatuses")]
        public IActionResult AddStatus(int? projectId, int? teamId, string title)
        {
            var response = new ResponseModel<TaskStatusResponseModel>();

            if (teamId != null && projectId != null)
                teamId = null;

            #region CheckTeamAndProjectExistance
            if (projectId != null && !vwsDbContext.Projects.Any(p => p.Id == projectId && !p.IsDeleted))
            {
                response.Message = "Project not found";
                response.AddError(localizer["Project not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (teamId != null && !vwsDbContext.Teams.Any(t => t.Id == teamId && !t.IsDeleted))
            {
                response.Message = "Team not found";
                response.AddError(localizer["Team not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            #endregion

            #region CheckTeamAndProjectAccess
            if (projectId != null && !permissionService.HasAccessToProject(LoggedInUserId.Value, (int)projectId))
            {
                response.Message = "Project access denied";
                response.AddError(localizer["You do not have access to project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            if (teamId != null && !permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)teamId))
            {
                response.Message = "Team access denied";
                response.AddError(localizer["You do not have access to team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            Domain._task.TaskStatus newStatus;
            int lastStatus = 0;

            if (teamId != null)
            {
                var teamStatuses = vwsDbContext.TaskStatuses.Where(status => status.TeamId == teamId)
                                                            .OrderByDescending(status => status.EvenOrder);
                
                if (teamStatuses.Count() != 0)
                    lastStatus = teamStatuses.First().EvenOrder;

                newStatus = new Domain._task.TaskStatus() { EvenOrder = lastStatus + 2, TeamId = teamId, ProjectId = null, Title = title, UserProfileId = null };
                vwsDbContext.AddTaskStatus(newStatus);
                vwsDbContext.Save();

                response.Value = new TaskStatusResponseModel() { Id = newStatus.Id, Title = newStatus.Title };
                response.Message = "New status added successfully!";
                return Ok(response);
            }
            else if (projectId != null)
            {
                var projectStatuses = vwsDbContext.TaskStatuses.Where(status => status.ProjectId == projectId)
                                                               .OrderByDescending(status => status.EvenOrder);

                if (projectStatuses.Count() != 0)
                    lastStatus = projectStatuses.First().EvenOrder;


                newStatus = new Domain._task.TaskStatus() { EvenOrder = lastStatus + 2, TeamId = null, ProjectId = projectId, Title = title, UserProfileId = null };
                vwsDbContext.AddTaskStatus(newStatus);
                vwsDbContext.Save();

                response.Value = new TaskStatusResponseModel() { Id = newStatus.Id, Title = newStatus.Title };
                response.Message = "New status added successfully!";
                return Ok(response);
            }
            var userStatuses = vwsDbContext.TaskStatuses.Where(status => status.UserProfileId == LoggedInUserId.Value)
                                                        .OrderByDescending(status => status.EvenOrder);

            newStatus = new Domain._task.TaskStatus() { EvenOrder = lastStatus + 2, TeamId = null, ProjectId = null, Title = title, UserProfileId = LoggedInUserId.Value };
            vwsDbContext.AddTaskStatus(newStatus);
            vwsDbContext.Save();

            response.Value = new TaskStatusResponseModel() { Id = newStatus.Id, Title = newStatus.Title };
            response.Message = "New status added successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateStatusTitle")]
        public IActionResult UpdateStatusTitle(int statusId, string newTitle)
        {
            var response = new ResponseModel();

            var selectedStatus = vwsDbContext.TaskStatuses.FirstOrDefault(status => status.Id == statusId);
            if (selectedStatus == null)
            {
                response.Message = "Status not found!";
                response.AddError(localizer["Status not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            #region CheckAccess
            if ((selectedStatus.ProjectId != null && !permissionService.HasAccessToProject(LoggedInUserId.Value, (int)selectedStatus.ProjectId)) ||
                (selectedStatus.TeamId != null && !permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)selectedStatus.TeamId)) ||
                selectedStatus.UserProfileId != null && selectedStatus.UserProfileId != LoggedInUserId.Value)
            {
                response.Message = "Task Status access denied";
                response.AddError(localizer["You do not have access to task status."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            selectedStatus.Title = newTitle;
            vwsDbContext.Save();

            response.Message = "Status title updated successfull!";
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("deleteStatus")]
        public IActionResult DeleteStatus(int statusId)
        {
            var response = new ResponseModel();

            var selectedStatus = vwsDbContext.TaskStatuses.FirstOrDefault(status => status.Id == statusId);
            if (selectedStatus == null)
            {
                response.Message = "Status not found!";
                response.AddError(localizer["Status not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            #region CheckAccess
            if ((selectedStatus.ProjectId != null && !permissionService.HasAccessToProject(LoggedInUserId.Value, (int)selectedStatus.ProjectId)) ||
                (selectedStatus.TeamId != null && !permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)selectedStatus.TeamId)) ||
                selectedStatus.UserProfileId != null && selectedStatus.UserProfileId != LoggedInUserId.Value)
            {
                response.Message = "Task Status access denied";
                response.AddError(localizer["You do not have access to task status."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            if (vwsDbContext.GeneralTasks.Any(task => task.TaskStatusId == statusId && !task.IsDeleted))
            {
                response.Message = "You can not delete status";
                response.AddError(localizer["You can not delete status which has task."]);
                return Ok(response);
            }

            vwsDbContext.DeleteTaskStatus(statusId);
            vwsDbContext.Save();

            response.Message = "Status deleted successfully!";
            return Ok(response);
        }
    }
}
