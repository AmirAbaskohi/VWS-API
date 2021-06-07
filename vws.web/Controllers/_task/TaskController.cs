using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._project;
using vws.web.Domain._task;
using vws.web.Domain._team;
using vws.web.Enums;
using vws.web.Models;
using vws.web.Models._task;
using vws.web.Repositories;
using vws.web.Services;
using vws.web.Services._project;
using vws.web.Services._task;
using vws.web.Services._team;
using static vws.web.EmailTemplates.EmailTemplateTypes;

namespace vws.web.Controllers._task
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class TaskController : BaseController
    {
        #region Feilds
        private readonly IStringLocalizer<TaskController> _localizer;
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _configuration;
        private readonly IFileManager _fileManager;
        private readonly INotificationService _notificationService;
        private readonly ITaskManagerService _taskManager;
        private readonly IProjectManagerService _projectManager;
        private readonly ITeamManagerService _teamManager;
        #endregion

        #region Ctor
        public TaskController(IStringLocalizer<TaskController> localizer, IVWS_DbContext vwsDbContext,
            IPermissionService permissionService, IConfiguration configuration, IFileManager fileManager,
            INotificationService notificationService, ITaskManagerService taskManager,
            IProjectManagerService projectManager, ITeamManagerService teamManager)
        {
            _localizer = localizer;
            _vwsDbContext = vwsDbContext;
            _permissionService = permissionService;
            _configuration = configuration;
            _fileManager = fileManager;
            _notificationService = notificationService;
            _taskManager = taskManager;
            _teamManager = teamManager;
            _projectManager = projectManager;
        }
        #endregion

        #region PrivateMethods
        private async Task AddUsersToTask(long taskId, List<Guid> users)
        {
            var creationTime = DateTime.UtcNow;
            foreach (var user in users)
            {
                await _vwsDbContext.AddTaskAssignAsync(new TaskAssign()
                {
                    CreatedBy = LoggedInUserId.Value,
                    IsDeleted = false,
                    CreatedOn = creationTime,
                    UserProfileId = user,
                    Guid = Guid.NewGuid(),
                    GeneralTaskId = taskId
                });
            }
            _vwsDbContext.Save();
        }

        private List<Guid> GetUsersCanBeAddedToTask(int? teamId, int? projectId)
        {
            if (teamId != null && projectId != null)
                teamId = null;

            if (teamId != null)
                return _permissionService.GetUsersHaveAccessToTeam((int)teamId);
            else if (projectId != null)
                return _permissionService.GetUsersHaveAccessToProject((int)projectId);

            return new List<Guid>();
        }

        private List<TaskStatusResponseModel> GetTaskStatuses(int? projectId, int? teamId)
        {
            if (projectId != null)
            {
                return _vwsDbContext.TaskStatuses.Where(status => status.ProjectId == projectId && !status.IsDeleted)
                                                          .OrderBy(status => status.EvenOrder)
                                                          .Select(status => new TaskStatusResponseModel() { Id = status.Id, Title = status.Title, ProjectId = status.ProjectId, TeamId = status.TeamId, UserProfileId = status.UserProfileId })
                                                          .ToList();
            }
            else if (teamId != null)
            {
                return _vwsDbContext.TaskStatuses.Where(status => status.TeamId == teamId && !status.IsDeleted)
                                                          .OrderBy(status => status.EvenOrder)
                                                          .Select(status => new TaskStatusResponseModel() { Id = status.Id, Title = status.Title, ProjectId = status.ProjectId, TeamId = status.TeamId, UserProfileId = status.UserProfileId })
                                                          .ToList();
            }
            return _vwsDbContext.TaskStatuses.Where(status => status.UserProfileId == LoggedInUserId.Value && !status.IsDeleted)
                                                      .OrderBy(status => status.EvenOrder)
                                                      .Select(status => new TaskStatusResponseModel() { Id = status.Id, Title = status.Title, ProjectId = status.ProjectId, TeamId = status.TeamId, UserProfileId = status.UserProfileId })
                                                      .ToList();
        }

        private List<TagResponseModel> GetAvailableTags(int? projectId, int? teamId)
        {
            if (projectId != null)
            {
                return _vwsDbContext.Tags.Where(tag => tag.ProjectId == projectId)
                                        .Select(tag => new TagResponseModel() { Id = tag.Id, Title = tag.Title, Color = tag.Color })
                                        .ToList();
            }
            else if (teamId != null)
            {
                return _vwsDbContext.Tags.Where(tag => tag.TeamId == teamId)
                                        .Select(tag => new TagResponseModel() { Id = tag.Id, Title = tag.Title, Color = tag.Color })
                                        .ToList();
            }
            return _vwsDbContext.Tags.Where(tag => tag.UserProfileId == LoggedInUserId.Value)
                                    .Select(tag => new TagResponseModel() { Id = tag.Id, Title = tag.Title, Color = tag.Color })
                                    .ToList();
        }

        private bool HasCheckListsTitleMoreCharacters(List<CheckListModel> checkLists)
        {
            return checkLists.Any(checkList => checkList.Title.Length > 250);
        }

        private bool HasCheckListsTitleItemMoreCharacters(List<CheckListItemModel> checkListItems)
        {
            return checkListItems.Any(checkListItem => checkListItem.Title.Length > 500);
        }

        private void AddCheckLists(long id, List<CheckListModel> checkLists)
        {
            foreach (var checkList in checkLists)
            {
                var newCheckList = new TaskCheckList()
                {
                    CreatedBy = LoggedInUserId.Value,
                    CreatedOn = DateTime.UtcNow,
                    IsDeleted = false,
                    GeneralTaskId = id,
                    ModifiedBy = LoggedInUserId.Value,
                    ModifiedOn = DateTime.UtcNow,
                    Title = checkList.Title
                };
                _vwsDbContext.AddCheckList(newCheckList);
                _vwsDbContext.Save();

                var itemsResponse = AddCheckListItems(newCheckList.Id, checkList.Items);
                _vwsDbContext.Save();
            }
        }

        private List<CheckListItemResponseModel> AddCheckListItems(long checkListId, List<CheckListItemModel> items)
        {
            var taskCheckListItems = new List<TaskCheckListItem>();
            var creationTime = DateTime.UtcNow;
            
            taskCheckListItems = items.Select(item => new TaskCheckListItem
            {
                CreatedBy = LoggedInUserId.Value,
                CreatedOn = creationTime,
                IsDeleted = false,
                TaskCheckListId = checkListId,
                IsChecked = item.IsChecked,
                Title = item.Title,
                ModifiedBy = LoggedInUserId.Value,
                ModifiedOn = creationTime
            }).ToList();

            _vwsDbContext.AddCheckListItems(taskCheckListItems);
            _vwsDbContext.Save();

            return taskCheckListItems.Select(item => new CheckListItemResponseModel()
            {
                Id = item.Id,
                CreatedBy = item.CreatedBy,
                CreatedOn = item.CreatedOn,
                IsChecked = item.IsChecked,
                ModifiedBy = item.ModifiedBy,
                ModifiedOn = item.ModifiedOn,
                TaskCheckListId = item.TaskCheckListId,
                Title = item.Title
            }).ToList();
        }

        private bool AreTagsValid(ICollection<int> tagIds, int? projectId, int? teamId)
        {
            Guid? userId = LoggedInUserId.Value;
            
            if (projectId != null && teamId != null)
                teamId = null;
            if (projectId != null || teamId != null)
                userId = null;

            var validTagIds = _vwsDbContext.Tags.Where(tag => tag.ProjectId == projectId && tag.TeamId == teamId && tag.UserProfileId == userId)
                                               .Select(tag => tag.Id).ToList();

            return tagIds.Except(validTagIds).Count() == 0;
        }

        private void ReorderStatuses(int? teamId, int?projectId)
        {
            var userId = LoggedInUserId;
            if (teamId != null && projectId != null)
                teamId = null;
            if (teamId != null || projectId != null)
                userId = null;

            int startOrder = 2;
            var statuses = _vwsDbContext.TaskStatuses.Where(status => status.TeamId == teamId && status.ProjectId == projectId && status.UserProfileId == userId && !status.IsDeleted)
                                                     .OrderBy(status => status.EvenOrder);
            foreach (var status in statuses)
            {
                status.EvenOrder = startOrder;
                startOrder += 2;
            }

            _vwsDbContext.Save();
        }

        private void AddTaskTags(long taskId, ICollection<int> tagIds)
        {
            foreach (var tagId in tagIds)
                _vwsDbContext.AddTaskTag(new TaskTag() { GeneralTaskId = taskId, TagId = tagId });
            _vwsDbContext.Save();
        }


        private List<FileModel> AddCommentAttachments(long commentId, List<Guid> attachments)
        {
            var fileAlreadyAttachments = _vwsDbContext.TaskCommentAttachments.Where(commentAttachment => commentAttachment.TaskCommentId == commentId)
                                                                            .Select(commentAttachment => commentAttachment.FileContainerGuid);

            attachments = attachments.Except(fileAlreadyAttachments).ToList();

            var result = new List<FileModel>();
            foreach(var attachment in attachments)
            {
                var selectedFileContainer = _vwsDbContext.FileContainers.FirstOrDefault(container => container.Guid == attachment);
                if (selectedFileContainer == null || selectedFileContainer.CreatedBy != LoggedInUserId.Value)
                    continue;
                _vwsDbContext.AddTaskCommentAttachment(new TaskCommentAttachment()
                {
                    FileContainerId = selectedFileContainer.Id,
                    FileContainerGuid = selectedFileContainer.Guid,
                    TaskCommentId = commentId
                });
                var recentFile = _vwsDbContext.Files.FirstOrDefault(file => file.Id == selectedFileContainer.RecentFileId);
                result.Add(new FileModel() 
                {
                    FileContainerGuid = selectedFileContainer.Guid,
                    Name = recentFile.Name,
                    Extension = recentFile.Extension,
                    Size = recentFile.Size
                });
            }

            _vwsDbContext.Save();
            return result;
        }

        private List<FileModel> AddTaskAttachments(long taskId, List<Guid> attachments)
        {
            var fileAlreadyAttachments = _vwsDbContext.TaskAttachments.Where(taskAttachment => taskAttachment.GeneralTaskId == taskId)
                                                                      .Select(taskAttachment => taskAttachment.FileContainerGuid);

            attachments = attachments.Except(fileAlreadyAttachments).ToList();

            var result = new List<FileModel>();
            foreach (var attachment in attachments)
            {
                var selectedFileContainer = _vwsDbContext.FileContainers.FirstOrDefault(container => container.Guid == attachment);
                if (selectedFileContainer == null || selectedFileContainer.CreatedBy != LoggedInUserId.Value)
                    continue;
                _vwsDbContext.AddTaskAttachment(new TaskAttachment()
                {
                    FileContainerId = selectedFileContainer.Id,
                    FileContainerGuid = selectedFileContainer.Guid,
                    GeneralTaskId = taskId
                });
                var recentFile = _vwsDbContext.Files.FirstOrDefault(file => file.Id == selectedFileContainer.RecentFileId);
                result.Add(new FileModel()
                {
                    FileContainerGuid = selectedFileContainer.Guid,
                    Name = recentFile.Name,
                    Extension = recentFile.Extension,
                    Size = recentFile.Size
                });
            }

            _vwsDbContext.Save();
            return result;
        }

        private void DeleteTaskComment(long commentId)
        {
            var attachments = _vwsDbContext.TaskCommentAttachments.Include(attachment => attachment.FileContainer)
                                                                 .ThenInclude(container => container.Files)
                                                                 .Where(attachment => attachment.TaskCommentId == commentId)
                                                                 .Select(attachment => attachment.FileContainer);
            foreach (var attachment in attachments)
            {
                foreach (var file in attachment.Files)
                    _fileManager.DeleteFile(file.Address);
                _vwsDbContext.DeleteFileContainer(attachment);
            }
            _vwsDbContext.DeleteTaskComment(commentId);
            _vwsDbContext.Save();
        }

        private async Task AddCreateTaaskHistory(TaskResponseModel taskResponseModel, Guid creatorId)
        {
            var user = await _vwsDbContext.GetUserProfileAsync(creatorId);
            var userModel = new UserModel()
            {
                NickName = user.NickName,
                ProfileImageGuid = user.ProfileImageGuid,
                UserId = user.UserId
            };

            var newTaskHistory = new TaskHistory()
            {
                EventTime = taskResponseModel.CreatedOn,
                Event = "Task {0} created by {1}.",
                GeneralTaskId = taskResponseModel.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = taskResponseModel.Title,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(userModel),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();

            var taskUsers = _taskManager.GetAssignedTo(taskResponseModel.Id).Select(user => user.UserId).ToList();
            taskUsers.Remove(creatorId);
            _notificationService.SendMultipleNotification(taskUsers, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            if (taskResponseModel.ProjectId != null)
            {
                var newProjectHistory = new ProjectHistory()
                {
                    EventTime = taskResponseModel.CreatedOn,
                    Event = "Task {0} created by {1} for this project.",
                    ProjectId = taskResponseModel.ProjectId.Value
                };
                _vwsDbContext.AddProjectHistory(newProjectHistory);
                _vwsDbContext.Save();
                _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                    Body = taskResponseModel.Title,
                    ProjectHistoryId = newProjectHistory.Id
                });
                _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                    Body = JsonConvert.SerializeObject(userModel),
                    ProjectHistoryId = newProjectHistory.Id
                });
                _vwsDbContext.Save();

                var projectUsers = _projectManager.GetProjectUsers((int)taskResponseModel.ProjectId).Select(user => user.UserId).ToList();
                projectUsers.Remove(creatorId);
                _notificationService.SendMultipleNotification(taskUsers, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);
            }
            else if (taskResponseModel.TeamId != null)
            {
                var newTeamHistory = new TeamHistory()
                {
                    EventTime = taskResponseModel.CreatedOn,
                    Event = "Task {0} created by {1} for this team.",
                    TeamId = taskResponseModel.TeamId.Value
                };
                _vwsDbContext.AddTeamHistory(newTeamHistory);
                _vwsDbContext.Save();
                _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                    Body = taskResponseModel.Title,
                    TeamHistoryId = newTeamHistory.Id
                });
                _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                    Body = JsonConvert.SerializeObject(userModel),
                    TeamHistoryId = newTeamHistory.Id
                });
                _vwsDbContext.Save();

                var teamUsers = (await _teamManager.GetTeamMembers((int)taskResponseModel.TeamId)).Select(user => user.UserId).ToList();
                teamUsers.Remove(creatorId);
                _notificationService.SendMultipleNotification(teamUsers, (byte)SeedDataEnum.NotificationTypes.Team, newTeamHistory.Id);
            }
        }

        private async Task<List<long>> SaveAssignTaskHistory(long taskId, Guid userId, List<Guid> assignedUsers, DateTime assignTime)
        {
            var result = new List<long>();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            var userModelSerialized = JsonConvert.SerializeObject(new UserModel()
            {
                NickName = user.NickName,
                ProfileImageGuid = user.ProfileImageGuid,
                UserId = user.UserId
            });

            foreach (var assinedUser in assignedUsers)
            {
                var selectedUser = await _vwsDbContext.GetUserProfileAsync(assinedUser);
                var selectedUserModelSerialized = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = selectedUser.NickName,
                    ProfileImageGuid = selectedUser.ProfileImageGuid,
                    UserId = selectedUser.UserId
                });

                var newHistory = new TaskHistory()
                {
                    Event = "{0} assigned task to {1}.",
                    EventTime = assignTime,
                    GeneralTaskId = taskId
                };
                _vwsDbContext.AddTaskHistory(newHistory);
                _vwsDbContext.Save();
                _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                    Body = userModelSerialized,
                    TaskHistoryId = newHistory.Id,
                });
                _vwsDbContext.Save();
                _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                    Body = selectedUserModelSerialized,
                    TaskHistoryId = newHistory.Id,
                });
                _vwsDbContext.Save();

                result.Add(newHistory.Id);
            }

            return result;
        }

        private void UpdateUserTeamAndProjectActivity(int? teamId, int? projectId, DateTime time)
        {
            var userId = LoggedInUserId.Value;
            if (teamId != null)
            {
                _vwsDbContext.AddUserTeamActivity(new UserTeamActivity()
                {
                    TeamId = (int)teamId,
                    Time = time,
                    UserProfileId = userId
                });
                _vwsDbContext.Save();
            }
            if (projectId != null)
            {
                _vwsDbContext.AddUserProjectActivity(new UserProjectActivity()
                {
                    ProjectId = (int)projectId,
                    Time = time,
                    UserProfileId = userId
                });
                _vwsDbContext.Save();
            }
        }

        #endregion

        #region TaskAPIS
        [HttpPost]
        [Authorize]
        [Route("create")]
        public async Task<IActionResult> CreateTask([FromBody] TaskModel model)
        {
            var response = new ResponseModel<TaskResponseModel>();

            if (model.TeamId != null && model.ProjectId != null)
                model.TeamId = null;

            if (!String.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
            {
                response.Message = "Task model data has problem.";
                response.AddError(_localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Title.Length > 500)
            {
                response.Message = "Task model data has problem.";
                response.AddError(_localizer["Length of title is more than 500 characters."]);
            }
            if (model.StartDate.HasValue && model.EndDate.HasValue)
            {
                if (model.StartDate > model.EndDate)
                {
                    response.Message = "Task model data has problem.";
                    response.AddError(_localizer["Start Date should be before End Date."]);
                }
            }
            if (!Enum.IsDefined(typeof(SeedDataEnum.TaskPriority), model.PriorityId))
            {
                response.Message = "Task model data has problem.";
                response.AddError(_localizer["Priority id is not defined."]);
            }
            if (HasCheckListsTitleMoreCharacters(model.CheckLists))
            {
                response.Message = "Task model data has problem.";
                response.AddError(_localizer["Check list title can not have more than 250 characters."]);
            }
            foreach (var checkList in model.CheckLists)
            {
                if (HasCheckListsTitleItemMoreCharacters(checkList.Items))
                {
                    response.Message = "Task model data has problem.";
                    response.AddError(_localizer["Check list item title can not have more than 500 characters."]);
                    break;
                }
            }
            if (response.HasError)
                return StatusCode(StatusCodes.Status400BadRequest, response);

            Guid userId = LoggedInUserId.Value;

            #region CheckTeamAndProjectExistance
            if (model.ProjectId != null && !_vwsDbContext.Projects.Any(p => p.Id == model.ProjectId && !p.IsDeleted))
            {
                response.Message = "Project not found";
                response.AddError(_localizer["Project not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (model.TeamId != null && !_vwsDbContext.Teams.Any(t => t.Id == model.TeamId && !t.IsDeleted))
            {
                response.Message = "Team not found";
                response.AddError(_localizer["Team not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            #endregion

            #region CheckTeamAndProjectAccess
            if (model.ProjectId != null && !_permissionService.HasAccessToProject(userId, (int)model.ProjectId))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You do not have access to project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            if (model.TeamId != null && !_permissionService.HasAccessToTeam(userId, (int)model.TeamId))
            {
                response.Message = "Team access denied";
                response.AddError(_localizer["You do not have access to team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            model.Users = model.Users.Distinct().ToList();

            DateTime creationTime = DateTime.UtcNow;

            if (!AreTagsValid(model.Tags, model.ProjectId, model.TeamId))
            {
                response.Message = "Invalid tags";
                response.AddError(_localizer["Invalid tags."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }    

            if (GetUsersCanBeAddedToTask(model.TeamId, model.ProjectId).Intersect(model.Users).Count() != model.Users.Count)
            {
                response.Message = "Users do not have access";
                response.AddError(_localizer["Some of users you want to add do not have access to team or project."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var statuses = GetTaskStatuses(model.ProjectId, model.TeamId).Select(status => status.Id);
            if (statuses.Count() == 0)
            {
                response.Message = "No status";
                response.AddError(_localizer["There is no status to give task."]);
                return StatusCode(StatusCodes.Status424FailedDependency, response);
            }
            if (model.StatusId == null)
                model.StatusId = statuses.First();

            if (!statuses.Contains((int)model.StatusId))
            {
                response.Message = "Invalid status";
                response.AddError(_localizer["Invalid status."]);
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
                TaskStatusId = (int)model.StatusId,
                IsUrgent = model.IsUrgent
            };

            if (newTask.TeamId == null && model.ProjectId != null)
            {
                var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == model.ProjectId);
                newTask.ProjectId = model.ProjectId;
                newTask.TeamId = selectedProject.TeamId;
            }

            await _vwsDbContext.AddTaskAsync(newTask);
            _vwsDbContext.Save();

            AddTaskTags(newTask.Id, model.Tags);

            AddTaskAttachments(newTask.Id, model.Attachments);

            await AddUsersToTask(newTask.Id, model.Users);
            AddCheckLists(newTask.Id, model.CheckLists);
            model.Users.Remove(userId);
            string[] arguments = { newTask.Title, LoggedInNickName };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, model.Users, "New task with title <b>«{0}»</b> has been assigned to you by <b>«{1}»</b>.", "Task Assign", arguments);

            UpdateUserTeamAndProjectActivity(newTask.TeamId, newTask.ProjectId, newTask.CreatedOn);

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
                ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(newTask.ModifiedBy)).NickName,
                CreatedBy = (await _vwsDbContext.GetUserProfileAsync(newTask.CreatedBy)).NickName,
                PriorityId = newTask.TaskPriorityId,
                PriorityTitle = _localizer[((SeedDataEnum.TaskPriority)newTask.TaskPriorityId).ToString()],
                UsersAssignedTo = _taskManager.GetAssignedTo(newTask.Id),
                ProjectId = newTask.ProjectId,
                TeamId = newTask.TeamId,
                TeamName = newTask.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == newTask.TeamId).Name,
                ProjectName = newTask.ProjectId == null ? null : _vwsDbContext.Projects.FirstOrDefault(project => project.Id == newTask.ProjectId).Name,
                StatusId = newTask.TaskStatusId,
                StatusTitle = _vwsDbContext.TaskStatuses.FirstOrDefault(statuse => statuse.Id == newTask.TaskStatusId).Title,
                CheckLists = _taskManager.GetCheckLists(newTask.Id),
                Tags = _taskManager.GetTaskTags(newTask.Id),
                Comments = await _taskManager.GetTaskComments(newTask.Id),
                Attachments = _taskManager.GetTaskAttachments(newTask.Id),
                IsUrgent = newTask.IsUrgent
            };

            await AddCreateTaaskHistory(newTaskResponseModel, newTask.CreatedBy);

            response.Value = newTaskResponseModel;
            response.Message = "Task created successfully!";
            return Ok(response);

        }

        [HttpPut]
        [Authorize]
        [Route("updateTitle")]
        public async Task<IActionResult> UpdateTitle(long id, string newTitle)
        {
            var response = new ResponseModel();
            
            if (String.IsNullOrEmpty(newTitle) || newTitle.Length > 500)
            {
                response.Message = "Task model data has problem";
                response.AddError(_localizer["Title can not be empty or with more than 500 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTask = await _vwsDbContext.GetTaskAsync(id);
            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(LoggedInUserId.Value, id))
            {
                response.AddError(_localizer["You don't have access to this task."]);
                response.Message = "Task access forbidden";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedTask.Title == newTitle)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            string lastTitle = selectedTask.Title;

            selectedTask.Title = newTitle;
            selectedTask.ModifiedBy = LoggedInUserId.Value;
            selectedTask.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "Task title updated from {0} to {1} by {2}.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastTitle,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedTask.Title,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body =  JsonConvert.SerializeObject(new UserModel() 
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { lastTitle, newTitle, LoggedInNickName };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "Your task title has been updated from <b>«{0}»</b> to <b>«{1}»</b> by <b>«{2}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Task title updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateDescription")]
        public async Task<IActionResult> UpdateDescription(long id, string newDescription)
        {
            var response = new ResponseModel();

            if (!String.IsNullOrEmpty(newDescription) && newDescription.Length > 2000)
            {
                response.Message = "Task model data has problem";
                response.AddError(_localizer["Length of description is more than 2000 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTask = await _vwsDbContext.GetTaskAsync(id);
            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(LoggedInUserId.Value, id))
            {
                response.AddError(_localizer["You don't have access to this task."]);
                response.Message = "Task access forbidden";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedTask.Description == newDescription)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            string lastDescription = selectedTask.Description;

            selectedTask.Description = newDescription;
            selectedTask.ModifiedBy = LoggedInUserId.Value;
            selectedTask.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "Task desciption updated to {0} by {1}.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedTask.Description,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { selectedTask.Title, lastDescription, selectedTask.Description, LoggedInNickName };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "Your task desciption with title <b>«{0}»</b> has been updated from <b>«{1}»</b> to <b>«{2}»</b> by <b>«{3}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Task title updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updatePriority")]
        public async Task<IActionResult> UpdatePriority(long id, byte newPriority)
        {
            var response = new ResponseModel();

            if (!Enum.IsDefined(typeof(SeedDataEnum.TaskPriority), newPriority))
            {
                response.Message = "Task model data has problem.";
                response.AddError(_localizer["Priority id is not defined."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTask = await _vwsDbContext.GetTaskAsync(id);
            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(LoggedInUserId.Value, id))
            {
                response.AddError(_localizer["You don't have access to this task."]);
                response.Message = "Task access forbidden";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedTask.TaskPriorityId == newPriority)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastPriority = selectedTask.TaskPriorityId;

            selectedTask.TaskPriorityId = newPriority;
            selectedTask.ModifiedBy = LoggedInUserId.Value;
            selectedTask.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "Task priority updated from {0} to {1} by {2}.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = ((SeedDataEnum.TaskPriority)lastPriority).ToString(),
                TaskHistoryId = newTaskHistory.Id,
                ShouldBeLocalized = true
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = ((SeedDataEnum.TaskPriority)selectedTask.TaskPriorityId).ToString(),
                TaskHistoryId = newTaskHistory.Id,
                ShouldBeLocalized = true
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { selectedTask.Title, ((SeedDataEnum.TaskPriority)lastPriority).ToString(), ((SeedDataEnum.TaskPriority)selectedTask.TaskPriorityId).ToString(), LoggedInNickName };
            bool[] argumentsLocalize = { false, true, true, false };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "Your task priority with title <b>«{0}»</b> has been updated from <b>«{1}»</b> to <b>«{2}»</b> by <b>«{3}»</b>.", "Task Update", arguments, argumentsLocalize);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Task priority updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateUrgent")]
        public async Task<IActionResult> UpdateTaskUrgent(long id, bool isUrgent)
        {
            var response = new ResponseModel();

            var selectedTask = await _vwsDbContext.GetTaskAsync(id);
            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(LoggedInUserId.Value, id))
            {
                response.AddError(_localizer["You don't have access to this task."]);
                response.Message = "Task access forbidden";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedTask.IsUrgent == isUrgent)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastIsUrgent = selectedTask.IsUrgent;

            selectedTask.IsUrgent = isUrgent;
            selectedTask.ModifiedBy = LoggedInUserId.Value;
            selectedTask.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "Task emergency updated updated from {0} to {1} by {2}.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastIsUrgent ? "Urgent" : "Non-Urgent",
                TaskHistoryId = newTaskHistory.Id,
                ShouldBeLocalized = true
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedTask.IsUrgent ? "Urgent" : "Non-Urgent",
                TaskHistoryId = newTaskHistory.Id,
                ShouldBeLocalized = true
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { selectedTask.Title, lastIsUrgent ? "Urgent" : "Non-Urgent", selectedTask.IsUrgent ? "Urgent" : "Non-Urgent", LoggedInNickName };
            bool[] argumentsLocalize = { false, true, true, false };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "Your task emergency with title <b>«{0}»</b> has been updated from <b>«{1}»</b> to <b>«{2}»</b> by <b>«{3}»</b>.", "Task Update", arguments, argumentsLocalize);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Task priority updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateTeamAndProject")]
        public async Task<IActionResult> UpdateTeamAndProject(long id, int? projectId, int? teamId)
        {
            var response = new ResponseModel();

            if (projectId != null && teamId != null)
                teamId = null;

            Guid userId = LoggedInUserId.Value;

            #region CheckTeamAndProjectExistance
            if (projectId != null && !_vwsDbContext.Projects.Any(p => p.Id == projectId && !p.IsDeleted))
            {
                response.Message = "Project not found";
                response.AddError(_localizer["Project not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (teamId != null && !_vwsDbContext.Teams.Any(t => t.Id == teamId && !t.IsDeleted))
            {
                response.Message = "Team not found";
                response.AddError(_localizer["Team not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            #endregion

            #region CheckTeamAndProjectAccess
            if (projectId != null && !_permissionService.HasAccessToProject(userId, (int)projectId))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You do not have access to project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            if (teamId != null && !_permissionService.HasAccessToTeam(userId, (int)teamId))
            {
                response.Message = "Team access denied";
                response.AddError(_localizer["You do not have access to team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            var selectedTask = _vwsDbContext.GeneralTasks.Include(task => task.Team).Include(task => task.Project).FirstOrDefault(task => task.Id == id);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (!_permissionService.HasAccessToTask(userId, id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if ((selectedTask.ProjectId == null && selectedTask.TeamId == null && projectId == null && teamId == null) ||
                (selectedTask.ProjectId == null && selectedTask.TeamId != null && projectId == null && selectedTask.TeamId == teamId) ||
                (selectedTask.ProjectId != null && selectedTask.ProjectId == projectId))
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            #region CreatingEmailAndActivityMessage
            var selectedProject = projectId == null ? (Project)null : _vwsDbContext.Projects.FirstOrDefault(project => project.Id == projectId);
            var selectedTeam = teamId == null ? (Team)null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == teamId);
            List<string> emailMessageArguments = new List<string>();
            List<bool> emailMessageArgumentsLocalize = new List<bool>();
            emailMessageArguments.Add(selectedTask.Title);
            emailMessageArgumentsLocalize.Add(false);
            string emailMessage = "Your task with title <b>«{0}»</b> which was ";
            if (selectedTask.ProjectId == null && selectedTask.TeamId == null)
            {
                emailMessage += "<b>«{1}»</b> ";
                emailMessageArguments.Add("Personal");
                emailMessageArgumentsLocalize.Add(true);
            }
            else if (selectedTask.Project != null)
            {
                emailMessage += "under <b>«{1}»</b> project ";
                emailMessageArguments.Add(selectedTask.Project.Name);
                emailMessageArgumentsLocalize.Add(false);
            }
            else
            {
                emailMessage += "under <b>«{1}»</b> team ";
                emailMessageArguments.Add(selectedTask.Team.Name);
                emailMessageArgumentsLocalize.Add(false);
            }
            emailMessage += "updated to ";
            if (selectedProject == null && selectedTeam == null)
            {
                emailMessage += "<b>«{2}»</b> ";
                emailMessageArguments.Add("Personal");
                emailMessageArgumentsLocalize.Add(true);
            }
            else if (selectedProject != null)
            {
                emailMessage += "under <b>«{2}»</b> project ";
                emailMessageArguments.Add(selectedProject.Name);
                emailMessageArgumentsLocalize.Add(false);
            }
            else
            {
                emailMessage += "under <b>«{2}»</b> team ";
                emailMessageArguments.Add(selectedTeam.Name);
                emailMessageArgumentsLocalize.Add(false);
            }
            emailMessage += "by <b>«{3}»</b>.";
            emailMessageArguments.Add(LoggedInNickName);
            emailMessageArgumentsLocalize.Add(false);
            #endregion

            selectedTask.TeamId = teamId;
            selectedTask.ProjectId = projectId;
            if (selectedTask.TeamId == null && projectId != null)
            {
                var selectedProj = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == projectId);
                selectedTask.TeamId = selectedProj.TeamId;
            }
            selectedTask.ModifiedBy = userId;
            selectedTask.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "Task updated from {0} to {1} by {2}.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = emailMessageArguments[1],
                TaskHistoryId = newTaskHistory.Id,
                ShouldBeLocalized = emailMessageArgumentsLocalize[1]
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = emailMessageArguments[2],
                TaskHistoryId = newTaskHistory.Id,
                ShouldBeLocalized = emailMessageArgumentsLocalize[2]
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, emailMessage, "Task Update", emailMessageArguments.ToArray(), emailMessageArgumentsLocalize.ToArray());

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Task team and project updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateStartDate")]
        public async Task<IActionResult> UpdateStartDate(long id, DateTime? newStartDate)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedTask = await _vwsDbContext.GetTaskAsync(id);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (!_permissionService.HasAccessToTask(userId, id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedTask.EndDate.HasValue && newStartDate.HasValue && newStartDate > selectedTask.EndDate)
            {
                response.Message = "Task model data has problem";
                response.AddError(_localizer["Start Date should be before End Date."]);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            if (selectedTask.StartDate == newStartDate)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastStartDate = selectedTask.StartDate;

            selectedTask.StartDate = newStartDate;
            selectedTask.ModifiedBy = userId;
            selectedTask.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "Task start date updated from {0} to {1} by {2}.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastStartDate == null ? "No Time" : lastStartDate.ToString(),
                TaskHistoryId = newTaskHistory.Id,
                ShouldBeLocalized = lastStartDate == null ? true : false
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedTask.StartDate == null ? "No Time" : selectedTask.StartDate.ToString(),
                TaskHistoryId = newTaskHistory.Id,
                ShouldBeLocalized = selectedTask.StartDate == null ? true : false
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { selectedTask.Title, lastStartDate == null ? "No Time" : lastStartDate.ToString(), selectedTask.StartDate == null ? "No Time" : selectedTask.StartDate.ToString(), LoggedInNickName };
            bool[] argumentsLocalize = { false, lastStartDate == null ? true : false, selectedTask.StartDate == null ? true : false, false };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "Your task start date with title <b>«{0}»</b> has been updated from <b>«{1}»</b> to <b>«{2}»</b> by <b>«{3}»</b>.", "Task Update", arguments, argumentsLocalize);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Task start date updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateEndDate")]
        public async Task<IActionResult> UpdateEndDate(long id, DateTime? newEndDate)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedTask = await _vwsDbContext.GetTaskAsync(id);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (!_permissionService.HasAccessToTask(userId, id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedTask.StartDate.HasValue && newEndDate.HasValue && selectedTask.StartDate > newEndDate)
            {
                response.Message = "Task model data has problem";
                response.AddError(_localizer["Start Date should be before End Date."]);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            if (selectedTask.EndDate == newEndDate)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastEndDate = selectedTask.EndDate;

            selectedTask.EndDate = newEndDate;
            selectedTask.ModifiedBy = userId;
            selectedTask.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "Task end date updated from {0} to {1} by {2}.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastEndDate == null ? "No Time" : lastEndDate.ToString(),
                TaskHistoryId = newTaskHistory.Id,
                ShouldBeLocalized = lastEndDate == null ? true : false
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedTask.EndDate == null ? "No Time" : selectedTask.EndDate.ToString(),
                TaskHistoryId = newTaskHistory.Id,
                ShouldBeLocalized = selectedTask.EndDate == null ? true : false
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { selectedTask.Title, lastEndDate == null ? "No Time" : lastEndDate.ToString(), selectedTask.EndDate == null ? "No Time" : selectedTask.EndDate.ToString(), LoggedInNickName };
            bool[] argumentsLocalize = { false, lastEndDate == null ? true : false, selectedTask.EndDate == null ? true : false, false };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "Your task end date with title <b>«{0}»</b> has been updated from <b>«{1}»</b> to <b>«{2}»</b> by <b>«{3}»</b>.", "Task Update", arguments, argumentsLocalize);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Task start date updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateStatus")]
        public async Task<IActionResult> UpdateTaskStatus(long id, int newStatusId)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var selectedTask = _vwsDbContext.GeneralTasks.Include(task => task.Status).FirstOrDefault(task => task.Id == id);
            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, id))
            {
                response.AddError(_localizer["You don't have access to this task."]);
                response.Message = "Task access forbidden";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var statuses = GetTaskStatuses(selectedTask.ProjectId, selectedTask.TeamId).Select(status => status.Id);
            if (!statuses.Contains(newStatusId))
            {
                response.Message = "Invalid status";
                response.AddError(_localizer["Invalid status."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedTask.TaskStatusId == newStatusId)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastStatus = selectedTask.Status.Title;
            var lastStatusId = selectedTask.TaskStatusId;

            selectedTask.TaskStatusId = newStatusId;
            selectedTask.ModifiedBy = userId;
            selectedTask.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            var newStatus = _vwsDbContext.TaskStatuses.FirstOrDefault(status => status.Id == selectedTask.TaskStatusId).Title;

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "Task status updated from {0} to {1} by {2}.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastStatus,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = newStatus,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            _vwsDbContext.AddTaskStatusHistory(new TaskStatusHistory()
            {
                ChangeById = userId,
                GeneralTaskId = selectedTask.Id,
                LastStatusId = lastStatusId,
               NewStatusId = selectedTask.TaskStatusId
            });
            _vwsDbContext.Save();

            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { selectedTask.Title, lastStatus, newStatus , LoggedInNickName };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "Status of your task with title <b>«{0}»</b> has been updated from <b>«{1}»</b> to <b>«{2}»</b> by <b>«{3}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Task status changed";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("archive")]
        public async Task<IActionResult> ArchiveTask(long id)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedTask = await _vwsDbContext.GetTaskAsync(id);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (selectedTask.IsArchived)
            {
                response.Message = "Task archived already";
                response.AddError(_localizer["Task is archived already."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (!_permissionService.HasAccessToTask(userId, id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedTask.IsArchived = true;
            selectedTask.ModifiedBy = userId;
            selectedTask.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "Task has been archived by {0}.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { selectedTask.Title, LoggedInNickName };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "Your task with title <b>«{0}»</b> has been archived by <b>«{1}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Task archived successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("archiveStatusTasks")]
        public async Task<IActionResult> ArchiveAllStatusTasks(int statusId)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedStatus = _vwsDbContext.TaskStatuses.FirstOrDefault(status => status.Id == statusId);
            if (selectedStatus == null || selectedStatus.IsDeleted)
            {
                response.Message = "Status not found!";
                response.AddError(_localizer["Status not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            #region CheckAccess
            if ((selectedStatus.ProjectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)selectedStatus.ProjectId)) ||
                (selectedStatus.TeamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)selectedStatus.TeamId)) ||
                selectedStatus.UserProfileId != null && selectedStatus.UserProfileId != LoggedInUserId.Value)
            {
                response.Message = "Task Status access denied";
                response.AddError(_localizer["You do not have access to task status."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            bool isPersonal = (selectedStatus.UserProfileId != null);
            bool isForTeam = (selectedStatus.TeamId != null);
            int? id = isPersonal ? null : (isForTeam ? selectedStatus.TeamId : selectedStatus.ProjectId);

            var modificationTime = DateTime.UtcNow;
            var allStatusTasks = _vwsDbContext.GeneralTasks.Where(task => task.TaskStatusId == statusId && !task.IsArchived && !task.IsDeleted);
            foreach (var statusTask in allStatusTasks)
            {
                statusTask.ModifiedBy = userId;
                statusTask.ModifiedOn = modificationTime;
                statusTask.IsArchived = true;
            }
            _vwsDbContext.Save();

            if (!isPersonal && isForTeam)
            {
                #region History
                var newTeamHistory = new TeamHistory()
                {
                    EventTime = modificationTime,
                    Event = "{0} archived all tasks in status {1}.",
                    TeamId = (int)id
                };
                _vwsDbContext.AddTeamHistory(newTeamHistory);
                _vwsDbContext.Save();
                var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
                _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                    Body = JsonConvert.SerializeObject(new UserModel()
                    {
                        NickName = user.NickName,
                        ProfileImageGuid = user.ProfileImageGuid,
                        UserId = user.UserId
                    }),
                    TeamHistoryId = newTeamHistory.Id
                });
                _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                    Body = selectedStatus.Title,
                    TeamHistoryId = newTeamHistory.Id
                });
                _vwsDbContext.Save();
                #endregion

                var selectedTeam = _vwsDbContext.Teams.FirstOrDefault(team => team.Id == (int)newTeamHistory.TeamId);
                var teamMemebers = (await _teamManager.GetTeamMembers(newTeamHistory.TeamId)).Select(user => user.UserId).ToList();
                teamMemebers = teamMemebers.Distinct().ToList();
                teamMemebers.Remove(LoggedInUserId.Value);
                string[] arguments = { selectedStatus.Title, LoggedInNickName, selectedTeam.Name };
                await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, teamMemebers, "All tasks in status <b>«{0}»</b> archived by <b>«{1}»</b> in your team with name <b>«{2}»</b>.", "Team Update", arguments);

                _notificationService.SendMultipleNotification(teamMemebers, (byte)SeedDataEnum.NotificationTypes.Team, newTeamHistory.Id);
            }
            else if (!isPersonal && !isForTeam)
            {
                #region History
                var newProjectHistory = new ProjectHistory()
                {
                    EventTime = modificationTime,
                    Event = "{0} archived all tasks in status {1}.",
                    ProjectId = (int)id
                };
                _vwsDbContext.AddProjectHistory(newProjectHistory);
                _vwsDbContext.Save();
                var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
                _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                    Body = JsonConvert.SerializeObject(new UserModel()
                    {
                        NickName = user.NickName,
                        ProfileImageGuid = user.ProfileImageGuid,
                        UserId = user.UserId
                    }),
                    ProjectHistoryId = newProjectHistory.Id
                });
                _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                    Body = selectedStatus.Title,
                    ProjectHistoryId = newProjectHistory.Id
                });
                _vwsDbContext.Save();
                #endregion

                var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == (int)newProjectHistory.ProjectId);
                var projectMemebers = _projectManager.GetProjectUsers(newProjectHistory.ProjectId).Select(user => user.UserId).ToList();
                projectMemebers = projectMemebers.Distinct().ToList();
                projectMemebers.Remove(LoggedInUserId.Value);
                string[] arguments = { selectedStatus.Title, LoggedInNickName, selectedProject.Name };
                await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, projectMemebers, "All tasks in status <b>«{0}»</b> archived by <b>«{1}»</b> in your project with name <b>«{2}»</b>.", "Team Update", arguments);

                _notificationService.SendMultipleNotification(projectMemebers, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);
            }

            response.Message = "Status's tasks archived successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public async Task<IEnumerable<TaskResponseModel>> GetTasks()
        {
            Guid userId = LoggedInUserId.Value;

            List<TaskResponseModel> response = new List<TaskResponseModel>();

            var userTasks = _taskManager.GetUserTasks(userId);
            userTasks = userTasks.OrderByDescending(task => task.CreatedOn).ToList();

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
                    CreatedBy = (await _vwsDbContext.GetUserProfileAsync(userTask.CreatedBy)).NickName,
                    ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(userTask.ModifiedBy)).NickName,
                    Guid = userTask.Guid,
                    PriorityId = userTask.TaskPriorityId,
                    PriorityTitle = _localizer[((SeedDataEnum.TaskPriority)userTask.TaskPriorityId).ToString()],
                    UsersAssignedTo = _taskManager.GetAssignedTo(userTask.Id),
                    ProjectId = userTask.ProjectId,
                    TeamId = userTask.TeamId,
                    TeamName = userTask.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == userTask.TeamId).Name,
                    ProjectName = userTask.ProjectId == null ? null : _vwsDbContext.Projects.FirstOrDefault(project => project.Id == userTask.ProjectId).Name,
                    StatusId = userTask.TaskStatusId,
                    StatusTitle = _vwsDbContext.TaskStatuses.FirstOrDefault(statuse => statuse.Id == userTask.TaskStatusId).Title,
                    CheckLists = _taskManager.GetCheckLists(userTask.Id),
                    Tags = _taskManager.GetTaskTags(userTask.Id),
                    Comments = await _taskManager.GetTaskComments(userTask.Id),
                    Attachments = _taskManager.GetTaskAttachments(userTask.Id),
                    IsUrgent = userTask.IsUrgent
                });
            }
            return response;
        }

        [HttpGet]
        [Authorize]
        [Route("getTasksUsers")]
        public IActionResult GetTasksUsers(int? teamId, int? projectId, bool forAll)
        {
            var response = new ResponseModel<HashSet<UserModel>>();
            var users = new HashSet<UserModel>();
            var userId = LoggedInUserId.Value;

            var userTasks = _taskManager.GetUserTasks(userId);
            var selectedTasks = new List<GeneralTask>();

            if (forAll)
                selectedTasks = userTasks;
            else
            {
                if (teamId != null && projectId != null)
                    teamId = null;

                #region CheckTeamAndProjectExistance
                if (projectId != null && !_vwsDbContext.Projects.Any(p => p.Id == projectId && !p.IsDeleted))
                {
                    response.Message = "Project not found";
                    response.AddError(_localizer["Project not found."]);
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }
                if (teamId != null && !_vwsDbContext.Teams.Any(t => t.Id == teamId && !t.IsDeleted))
                {
                    response.Message = "Team not found";
                    response.AddError(_localizer["Team not found."]);
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }
                #endregion

                #region CheckTeamAndProjectAccess
                if (projectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)projectId))
                {
                    response.Message = "Project access denied";
                    response.AddError(_localizer["You do not have access to project."]);
                    return StatusCode(StatusCodes.Status403Forbidden, response);
                }
                if (teamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)teamId))
                {
                    response.Message = "Team access denied";
                    response.AddError(_localizer["You do not have access to team."]);
                    return StatusCode(StatusCodes.Status403Forbidden, response);
                }
                #endregion

                selectedTasks = _vwsDbContext.GeneralTasks.Where(task => ((teamId == null && projectId != null && task.ProjectId == projectId) ||
                                                                              (teamId == null && projectId == null && task.TeamId == teamId && task.ProjectId == projectId && task.CreatedBy == userId) ||
                                                                              (task.TeamId == teamId && task.ProjectId == projectId)) &&
                                                                              !task.IsArchived && !task.IsDeleted)
                                                          .ToList();
            }

            foreach (var userTask in selectedTasks)
            {
                users.UnionWith(_taskManager.GetAssignedTo(userTask.Id));
                var creator = _vwsDbContext.UserProfiles.FirstOrDefault(profile => profile.UserId == userTask.CreatedBy);
                users.Add(new UserModel() { UserId = creator.UserId, NickName = creator.NickName, ProfileImageGuid = creator.ProfileImageGuid });
            }
            response.Value = users;
            response.Message = "Users returned successfully";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getRunningTasks")]
        public IEnumerable<RunningTaskResponseModel> GetRunningTasks()
        {
            Guid userId = LoggedInUserId.Value;

            var pausedTaskIds = _vwsDbContext.TimeTrackPauses.Include(timeTrackPause => timeTrackPause.GeneralTask)
                                                             .Where(timeTrackPause => timeTrackPause.UserProfileId == userId && !timeTrackPause.GeneralTask.IsDeleted)
                                                             .Select(timeTrackPause => new RunningTaskResponseModel() { IsPaused = true, StartDate = null, TaskId = timeTrackPause.GeneralTaskId });

            var notEndedTaskIds = _vwsDbContext.TimeTracks.Include(timeTrack => timeTrack.GeneralTask)
                                                          .Where(timeTrack => timeTrack.UserProfileId == userId && timeTrack.EndDate == null && !timeTrack.GeneralTask.IsDeleted)
                                                          .Select(timeTrack => new RunningTaskResponseModel() { IsPaused = false, StartDate = timeTrack.StartDate, TaskId = timeTrack.GeneralTaskId });

            var allRunningTasks = pausedTaskIds.Union(notEndedTaskIds);
            return allRunningTasks;
        }

        [HttpGet]
        [Authorize]
        [Route("getRunningTasksWithDetails")]
        public async Task<IEnumerable<FullRunningTaskResponseModel>> GetRunningTasksWithDetails()
        {
            Guid userId = LoggedInUserId.Value;

            var pausedTasks = _vwsDbContext.TimeTrackPauses.Include(timeTrackPause => timeTrackPause.GeneralTask)
                                                           .Where(timeTrackPause => timeTrackPause.UserProfileId == userId && !timeTrackPause.GeneralTask.IsDeleted);

            var notEndedTasks = _vwsDbContext.TimeTracks.Include(timeTrack => timeTrack.GeneralTask)
                                                        .Where(timeTrack => timeTrack.UserProfileId == userId && timeTrack.EndDate == null && !timeTrack.GeneralTask.IsDeleted);

            var pausedTasksResponse = new List<FullRunningTaskResponseModel>();
            var notEndedTasksResponse = new List<FullRunningTaskResponseModel>();

            #region paused
            foreach (var pausedTask in pausedTasks)
            {
                pausedTasksResponse.Add(new FullRunningTaskResponseModel()
                {
                    Id = pausedTask.GeneralTask.Id,
                    Title = pausedTask.GeneralTask.Title,
                    Description = pausedTask.GeneralTask.Description,
                    StartDate = pausedTask.GeneralTask.StartDate,
                    EndDate = pausedTask.GeneralTask.EndDate,
                    CreatedOn = pausedTask.GeneralTask.CreatedOn,
                    ModifiedOn = pausedTask.GeneralTask.ModifiedOn,
                    CreatedBy = (await _vwsDbContext.GetUserProfileAsync(pausedTask.GeneralTask.CreatedBy)).NickName,
                    ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(pausedTask.GeneralTask.ModifiedBy)).NickName,
                    Guid = pausedTask.GeneralTask.Guid,
                    PriorityId = pausedTask.GeneralTask.TaskPriorityId,
                    PriorityTitle = _localizer[((SeedDataEnum.TaskPriority)pausedTask.GeneralTask.TaskPriorityId).ToString()],
                    UsersAssignedTo = _taskManager.GetAssignedTo(pausedTask.GeneralTask.Id),
                    ProjectId = pausedTask.GeneralTask.ProjectId,
                    TeamId = pausedTask.GeneralTask.TeamId,
                    TeamName = pausedTask.GeneralTask.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == pausedTask.GeneralTask.TeamId).Name,
                    ProjectName = pausedTask.GeneralTask.ProjectId == null ? null : _vwsDbContext.Projects.FirstOrDefault(project => project.Id == pausedTask.GeneralTask.ProjectId).Name,
                    StatusId = pausedTask.GeneralTask.TaskStatusId,
                    StatusTitle = _vwsDbContext.TaskStatuses.FirstOrDefault(statuse => statuse.Id == pausedTask.GeneralTask.TaskStatusId).Title,
                    CheckLists = _taskManager.GetCheckLists(pausedTask.GeneralTask.Id),
                    Tags = _taskManager.GetTaskTags(pausedTask.GeneralTask.Id),
                    Comments = await _taskManager.GetTaskComments(pausedTask.GeneralTask.Id),
                    Attachments = _taskManager.GetTaskAttachments(pausedTask.GeneralTask.Id),
                    IsUrgent = pausedTask.GeneralTask.IsUrgent,
                    TimeTrackStartDate = null,
                    IsPaused = true
                });
            }
            #endregion

            #region notEnded
            foreach (var notEndedTask in notEndedTasks)
            {
                notEndedTasksResponse.Add(new FullRunningTaskResponseModel()
                {
                    Id = notEndedTask.GeneralTask.Id,
                    Title = notEndedTask.GeneralTask.Title,
                    Description = notEndedTask.GeneralTask.Description,
                    StartDate = notEndedTask.GeneralTask.StartDate,
                    EndDate = notEndedTask.GeneralTask.EndDate,
                    CreatedOn = notEndedTask.GeneralTask.CreatedOn,
                    ModifiedOn = notEndedTask.GeneralTask.ModifiedOn,
                    CreatedBy = (await _vwsDbContext.GetUserProfileAsync(notEndedTask.GeneralTask.CreatedBy)).NickName,
                    ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(notEndedTask.GeneralTask.ModifiedBy)).NickName,
                    Guid = notEndedTask.GeneralTask.Guid,
                    PriorityId = notEndedTask.GeneralTask.TaskPriorityId,
                    PriorityTitle = _localizer[((SeedDataEnum.TaskPriority)notEndedTask.GeneralTask.TaskPriorityId).ToString()],
                    UsersAssignedTo = _taskManager.GetAssignedTo(notEndedTask.GeneralTask.Id),
                    ProjectId = notEndedTask.GeneralTask.ProjectId,
                    TeamId = notEndedTask.GeneralTask.TeamId,
                    TeamName = notEndedTask.GeneralTask.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == notEndedTask.GeneralTask.TeamId).Name,
                    ProjectName = notEndedTask.GeneralTask.ProjectId == null ? null : _vwsDbContext.Projects.FirstOrDefault(project => project.Id == notEndedTask.GeneralTask.ProjectId).Name,
                    StatusId = notEndedTask.GeneralTask.TaskStatusId,
                    StatusTitle = _vwsDbContext.TaskStatuses.FirstOrDefault(statuse => statuse.Id == notEndedTask.GeneralTask.TaskStatusId).Title,
                    CheckLists = _taskManager.GetCheckLists(notEndedTask.GeneralTask.Id),
                    Tags = _taskManager.GetTaskTags(notEndedTask.GeneralTask.Id),
                    Comments = await _taskManager.GetTaskComments(notEndedTask.GeneralTask.Id),
                    Attachments = _taskManager.GetTaskAttachments(notEndedTask.GeneralTask.Id),
                    IsUrgent = notEndedTask.GeneralTask.IsUrgent,
                    IsPaused = false,
                    TimeTrackStartDate = notEndedTask.StartDate
                });
            }
            #endregion

            return pausedTasksResponse.Union(notEndedTasksResponse);
        }

        [HttpGet]
        [Authorize]
        [Route("getArchived")]
        public async Task<IEnumerable<TaskResponseModel>> GetArchivedTasks()
        {
            Guid userId = LoggedInUserId.Value;

            List<TaskResponseModel> response = new List<TaskResponseModel>();

            var userTasks = _taskManager.GetUserTasks(userId);
            userTasks = userTasks.OrderByDescending(task => task.CreatedOn).ToList();

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
                        CreatedBy = (await _vwsDbContext.GetUserProfileAsync(userTask.CreatedBy)).NickName,
                        ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(userTask.ModifiedBy)).NickName,
                        Guid = userTask.Guid,
                        PriorityId = userTask.TaskPriorityId,
                        PriorityTitle = _localizer[((SeedDataEnum.TaskPriority)userTask.TaskPriorityId).ToString()],
                        UsersAssignedTo = _taskManager.GetAssignedTo(userTask.Id),
                        ProjectId = userTask.ProjectId,
                        TeamId = userTask.TeamId,
                        TeamName = userTask.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == userTask.TeamId).Name,
                        ProjectName = userTask.ProjectId == null ? null : _vwsDbContext.Projects.FirstOrDefault(project => project.Id == userTask.ProjectId).Name,
                        StatusId = userTask.TaskStatusId,
                        StatusTitle = _vwsDbContext.TaskStatuses.FirstOrDefault(statuse => statuse.Id == userTask.TaskStatusId).Title,
                        CheckLists = _taskManager.GetCheckLists(userTask.Id),
                        Tags = _taskManager.GetTaskTags(userTask.Id),
                        Comments = await _taskManager.GetTaskComments(userTask.Id),
                        Attachments = _taskManager.GetTaskAttachments(userTask.Id),
                        IsUrgent = userTask.IsUrgent
                    });
                }
            }
            return response;
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
        [Route("getTaskHistory")]
        public IActionResult GetTaskHistory(long id)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel<List<HistoryModel>>();

            var selectedTask = _vwsDbContext.GeneralTasks.FirstOrDefault(task => task.Id == id);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (!_permissionService.HasAccessToTask(userId, id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var events = new List<HistoryModel>();
            var taskEvents = _vwsDbContext.TaskHistories.Where(taskHistory => taskHistory.GeneralTaskId == id).OrderByDescending(taskHistory => taskHistory.EventTime);
            foreach (var taskEvent in taskEvents)
            {
                var parameters = _vwsDbContext.TaskHistoryParameters.Where(param => param.TaskHistoryId == taskEvent.Id)
                                                             .OrderBy(param => param.Id)
                                                             .ToList();
                for (int i = 0; i < parameters.Count(); i++)
                {
                    if (parameters[i].ActivityParameterTypeId == (byte)SeedDataEnum.ActivityParameterTypes.Text && parameters[i].ShouldBeLocalized)
                        parameters[i].Body = _localizer[parameters[i].Body];
                }
                events.Add(new HistoryModel()
                {
                    Message = _localizer[taskEvent.Event],
                    Parameters = parameters.Select(param => new HistoryParameterModel() { ParameterBody = param.Body, ParameterType = param.ActivityParameterTypeId }).ToList(),
                    Time = taskEvent.EventTime
                });
            }

            response.Message = "History returned successfully!";
            response.Value = events;
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("delete")]
        public async Task<IActionResult> DeleteTask(long id)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedTask = _vwsDbContext.GeneralTasks.Include(task => task.TaskComments)
                                                        .ThenInclude(comment => comment.Attachments)
                                                        .ThenInclude(attachment => attachment.FileContainer)
                                                        .ThenInclude(container => container.Files)
                                                        .FirstOrDefault(task => task.Id == id);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (!_permissionService.HasAccessToTask(userId, id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var commentsAttachmentsPaths = new List<string>();
            foreach (var comment in selectedTask.TaskComments)
            {
                foreach (var attachment in comment.Attachments)
                {
                    foreach (var file in attachment.FileContainer.Files)
                        _fileManager.DeleteFile(file.Address);

                    var selectedContainer = _vwsDbContext.FileContainers.FirstOrDefault(container => container.Id == attachment.FileContainerId);
                    _vwsDbContext.DeleteFileContainer(selectedContainer);
                }
            }
            selectedTask.IsDeleted = true;
            selectedTask.ModifiedBy = userId;
            selectedTask.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            _taskManager.StopRunningTimes(selectedTask.Id, selectedTask.ModifiedOn);

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "Task deleted by {0}.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { selectedTask.Title, LoggedInNickName  };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "Your task with title <b>«{0}»</b> has been deleted by <b>«{1}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Task deleted successfully!";
            return Ok(response);
        }
        #endregion

        #region TaskAssignAPIS
        [HttpPost]
        [Authorize]
        [Route("assignTask")]
        public async Task<IActionResult> AssignTask([FromBody] AssignTaskModel model)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel<Object>();

            var successfulAssignedUsers = new List<Guid>();
            var failedToAssignUsers = new List<Guid>();

            model.Users = model.Users.Distinct().ToList();

            var selectedTask = await _vwsDbContext.GetTaskAsync(model.TaskId);
            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.AddError(_localizer["Task does not exist."]);
                response.Message = "Task not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, model.TaskId))
            {
                response.AddError(_localizer["You don't have access to this task."]);
                response.Message = "Task access forbidden";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var taskUsers = _taskManager.GetAssignedTo(model.TaskId).Select(user => user.UserId).ToList();

            var usersCanBeAssigned = GetUsersCanBeAddedToTask(selectedTask.TeamId, selectedTask.ProjectId);

            var assignTime = DateTime.UtcNow;

            foreach (var user in model.Users)
            {
                if (!_vwsDbContext.UserProfiles.Any(profile => profile.UserId == user) ||
                    taskUsers.Contains(user) || !usersCanBeAssigned.Contains(user))
                {
                    failedToAssignUsers.Add(user);
                    continue;
                }
                var newTaskAssign = new TaskAssign()
                {
                    Guid = Guid.NewGuid(),
                    GeneralTaskId = model.TaskId,
                    UserProfileId = user,
                    IsDeleted = false,
                    CreatedBy = userId,
                    CreatedOn = assignTime
                };
                await _vwsDbContext.AddTaskAssignAsync(newTaskAssign);
                successfulAssignedUsers.Add(user);
            }
            _vwsDbContext.Save();

            var historyIds = await SaveAssignTaskHistory(selectedTask.Id, LoggedInUserId.Value, successfulAssignedUsers, assignTime);

            taskUsers.Remove(userId);
            foreach (var user in successfulAssignedUsers)
            {
                var nickName = (await _vwsDbContext.GetUserProfileAsync(user)).NickName;
                string[] args = { LoggedInNickName ,selectedTask.Title, nickName };
                await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, taskUsers, "<b>«{0}»</b> assigned task <b>«{1}»</b> to <b>«{2}»</b>.", "Task Update", args);
            }

            string[] arguments = { selectedTask.Title, LoggedInNickName };
            Guid[] reuqestedUser = { userId };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, successfulAssignedUsers.Except(reuqestedUser).ToList(), "New task with title <b>«{0}»</b> has been assigned to you by <b>«{1}»</b>.", "Task Assign", arguments) ;

            var usersAssignedTo = _taskManager.GetAssignedTo(selectedTask.Id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            foreach (var historyId in historyIds)
                _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, historyId);

            response.Value = new { SuccessAssigs = successfulAssignedUsers, FailedAssign = failedToAssignUsers};
            response.Message = "Task assigned successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getUsersCanBeAssigned")]
        public IActionResult GetUsersCanBeAssigned(int? teamId, int? projectId)
        {
            var response = new ResponseModel<List<UserModel>>();
            var result = new List<UserModel>();

            if (projectId != null && teamId != null)
                teamId = null;

            #region CheckTeamAndProjectExistance
            if (projectId != null && !_vwsDbContext.Projects.Any(p => p.Id == projectId && !p.IsDeleted))
            {
                response.Message = "Project not found";
                response.AddError(_localizer["Project not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (teamId != null && !_vwsDbContext.Teams.Any(t => t.Id == teamId && !t.IsDeleted))
            {
                response.Message = "Team not found";
                response.AddError(_localizer["Team not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            #endregion

            #region CheckTeamAndProjectAccess
            if (projectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)projectId))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You do not have access to project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            if (teamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)teamId))
            {
                response.Message = "Team access denied";
                response.AddError(_localizer["You do not have access to team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            var users = GetUsersCanBeAddedToTask(teamId, projectId);
            foreach (var user in users)
            {
                var selectedUser = _vwsDbContext.UserProfiles.FirstOrDefault(profile => profile.UserId == user);
                result.Add(new UserModel()
                {
                    UserId = user,
                    NickName = selectedUser.NickName,
                    ProfileImageGuid = selectedUser.ProfileImageGuid
                });
            }

            response.Value = result;
            response.Message = "Users returned successfully!";
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

            var selectedTask = await _vwsDbContext.GetTaskAsync(id);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            assignedUsersList = _taskManager.GetAssignedTo(id);

            response.Message = "Users returned successfully!";
            response.Value = assignedUsersList;
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("deleteUserAssignedTo")]
        public async Task<IActionResult> DeleteUserAssignedTo(long id, Guid userId)
        {
            var response = new ResponseModel();

            var selectedTask = await _vwsDbContext.GetTaskAsync(id);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var selectedUserAssignedTask = _vwsDbContext.TaskAssigns.FirstOrDefault(taskAssign => taskAssign.UserProfileId == userId &&
                                                                                   taskAssign.GeneralTaskId == id &&
                                                                                   taskAssign.IsDeleted == false);

            if (selectedUserAssignedTask == null)
            {
                response.Message = "User does not have access already!";
                response.AddError(_localizer["User you selected does not have access to task already."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedUserAssignedTask.IsDeleted = true;
            selectedUserAssignedTask.DeletedBy = LoggedInUserId.Value;
            selectedUserAssignedTask.DeletedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedUserAssignedTask.DeletedOn,
                Event = "{0} unassigned {1} from task.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            var deletedUser = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = deletedUser.NickName,
                    ProfileImageGuid = deletedUser.ProfileImageGuid,
                    UserId = deletedUser.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            string[] arguments = { selectedTask.Title, LoggedInNickName };
            await _notificationService.SendSingleEmail((int)EmailTemplateEnum.NotificationEmail, "You have been unassigned from task with title <b>«{0}»</b> by <b>«{1}»</b>.", "Task Assign", userId, arguments);

            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            usersAssignedTo.Remove(userId);
            string[] args = { LoggedInNickName, (await _vwsDbContext.GetUserProfileAsync(userId)).NickName, selectedTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> unassigned <b>«{1}»</b> from task with title <b>«{2}»</b>.", "Task Update", arguments);

            response.Message = "User unassigned from task successfully!";
            return Ok(response);
        }
        #endregion

        #region TaskStatusAPIS
        [HttpPost]
        [Authorize]
        [Route("addStatus")]
        public IActionResult AddStatus(int? projectId, int? teamId, string title)
        {
            var response = new ResponseModel<TaskStatusResponseModel>();

            if (!String.IsNullOrEmpty(title) && title.Length > 100)
            {
                response.AddError(_localizer["Length of title is more than 100 characters."]);
                response.Message = "Tag model data has problem.";
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            if (teamId != null && projectId != null)
                teamId = null;

            #region CheckTeamAndProjectExistance
            if (projectId != null && !_vwsDbContext.Projects.Any(p => p.Id == projectId && !p.IsDeleted))
            {
                response.Message = "Project not found";
                response.AddError(_localizer["Project not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (teamId != null && !_vwsDbContext.Teams.Any(t => t.Id == teamId && !t.IsDeleted))
            {
                response.Message = "Team not found";
                response.AddError(_localizer["Team not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            #endregion

            #region CheckTeamAndProjectAccess
            if (projectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)projectId))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You do not have access to project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            if (teamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)teamId))
            {
                response.Message = "Team access denied";
                response.AddError(_localizer["You do not have access to team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            Domain._task.TaskStatus newStatus;
            int lastStatus = 0;

            if (teamId != null)
            {
                var teamStatuses = _vwsDbContext.TaskStatuses.Where(status => status.TeamId == teamId && !status.IsDeleted)
                                                             .OrderByDescending(status => status.EvenOrder);
                
                if (teamStatuses.Count() != 0)
                    lastStatus = teamStatuses.First().EvenOrder;

                newStatus = new Domain._task.TaskStatus() { EvenOrder = lastStatus + 2, TeamId = teamId, ProjectId = null, Title = title, UserProfileId = null };
                _vwsDbContext.AddTaskStatus(newStatus);
                _vwsDbContext.Save();

                response.Value = new TaskStatusResponseModel() { Id = newStatus.Id, Title = newStatus.Title };
                response.Message = "New status added successfully!";
                return Ok(response);
            }
            else if (projectId != null)
            {
                var projectStatuses = _vwsDbContext.TaskStatuses.Where(status => status.ProjectId == projectId && !status.IsDeleted)
                                                               .OrderByDescending(status => status.EvenOrder);

                if (projectStatuses.Count() != 0)
                    lastStatus = projectStatuses.First().EvenOrder;


                newStatus = new Domain._task.TaskStatus() { EvenOrder = lastStatus + 2, TeamId = null, ProjectId = projectId, Title = title, UserProfileId = null };
                _vwsDbContext.AddTaskStatus(newStatus);
                _vwsDbContext.Save();

                response.Value = new TaskStatusResponseModel() { Id = newStatus.Id, Title = newStatus.Title };
                response.Message = "New status added successfully!";
                return Ok(response);
            }
            var userStatuses = _vwsDbContext.TaskStatuses.Where(status => status.UserProfileId == LoggedInUserId.Value && !status.IsDeleted)
                                                        .OrderByDescending(status => status.EvenOrder);

            if (userStatuses.Count() != 0)
                lastStatus = userStatuses.First().EvenOrder;

            newStatus = new Domain._task.TaskStatus() { EvenOrder = lastStatus + 2, TeamId = null, ProjectId = null, Title = title, UserProfileId = LoggedInUserId.Value };
            _vwsDbContext.AddTaskStatus(newStatus);
            _vwsDbContext.Save();

            response.Value = new TaskStatusResponseModel() { Id = newStatus.Id, Title = newStatus.Title, ProjectId = newStatus.ProjectId, TeamId = newStatus.TeamId, UserProfileId = newStatus.UserProfileId };
            response.Message = "New status added successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateStatusTitle")]
        public IActionResult UpdateStatusTitle(int statusId, string newTitle)
        {
            var response = new ResponseModel();

            if (!String.IsNullOrEmpty(newTitle) && newTitle.Length > 100)
            {
                response.AddError(_localizer["Length of title is more than 100 characters."]);
                response.Message = "Status model data has problem.";
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            var selectedStatus = _vwsDbContext.TaskStatuses.FirstOrDefault(status => status.Id == statusId);
            if (selectedStatus == null || selectedStatus.IsDeleted)
            {
                response.Message = "Status not found!";
                response.AddError(_localizer["Status not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            #region CheckAccess
            if ((selectedStatus.ProjectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)selectedStatus.ProjectId)) ||
                (selectedStatus.TeamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)selectedStatus.TeamId)) ||
                selectedStatus.UserProfileId != null && selectedStatus.UserProfileId != LoggedInUserId.Value)
            {
                response.Message = "Task Status access denied";
                response.AddError(_localizer["You do not have access to task status."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            if (selectedStatus.Title == newTitle)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            selectedStatus.Title = newTitle;
            _vwsDbContext.Save();

            response.Message = "Status title updated successfull!";
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
            if (projectId != null && !_vwsDbContext.Projects.Any(p => p.Id == projectId && !p.IsDeleted))
            {
                response.Message = "Project not found";
                response.AddError(_localizer["Project not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (teamId != null && !_vwsDbContext.Teams.Any(t => t.Id == teamId && !t.IsDeleted))
            {
                response.Message = "Team not found";
                response.AddError(_localizer["Team not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            #endregion

            #region CheckTeamAndProjectAccess
            if (projectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)projectId))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You do not have access to project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            if (teamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)teamId))
            {
                response.Message = "Team access denied";
                response.AddError(_localizer["You do not have access to team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            response.Value = GetTaskStatuses(projectId, teamId);
            response.Message = "Statuses returned successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getAllStatuses")]
        public IEnumerable<TaskStatusResponseModel> GetAllStatuses()
        {
            var userId = LoggedInUserId.Value;

            var userTeams = _teamManager.GetAllUserTeams(userId).Select(team => team.Id);
            var userProjects = _projectManager.GetAllUserProjects(userId).Select(project => project.Id);

            var statuses = _vwsDbContext.TaskStatuses.Where(status => ((status.TeamId.HasValue && userTeams.Contains(status.TeamId.Value)) ||
                                                                      (status.ProjectId.HasValue && userProjects.Contains(status.ProjectId.Value)) ||
                                                                      (status.UserProfileId.HasValue && status.UserProfileId == userId)) && !status.IsDeleted);
            return statuses.Select(status => new TaskStatusResponseModel() { Id = status.Id, ProjectId = status.ProjectId, TeamId = status.TeamId, Title = status.Title, UserProfileId = status.UserProfileId }).ToList();
        }

        [HttpDelete]
        [Authorize]
        [Route("deleteStatus")]
        public IActionResult DeleteStatus(int statusId)
        {
            var response = new ResponseModel();

            var selectedStatus = _vwsDbContext.TaskStatuses.FirstOrDefault(status => status.Id == statusId);
            if (selectedStatus == null || selectedStatus.IsDeleted)
            {
                response.Message = "Status not found!";
                response.AddError(_localizer["Status not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            #region CheckAccess
            if ((selectedStatus.ProjectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)selectedStatus.ProjectId)) ||
                (selectedStatus.TeamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)selectedStatus.TeamId)) ||
                selectedStatus.UserProfileId != null && selectedStatus.UserProfileId != LoggedInUserId.Value)
            {
                response.Message = "Task Status access denied";
                response.AddError(_localizer["You do not have access to task status."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            if (_vwsDbContext.GeneralTasks.Any(task => task.TaskStatusId == statusId && !task.IsDeleted))
            {
                response.Message = "You can not delete status";
                response.AddError(_localizer["You can not delete status which has task."]);
                return Ok(response);
            }

            selectedStatus.IsDeleted = true;
            _vwsDbContext.Save();
            ReorderStatuses(selectedStatus.TeamId, selectedStatus.ProjectId);

            response.Message = "Status deleted successfully!";
            return Ok(response);
        }
        #endregion

        #region CheckListAPIS
        [HttpPost]
        [Authorize]
        [Route("addCheckList")]
        public async Task<IActionResult> AddCheckList(long id, [FromBody] CheckListModel model)
        {
            var response = new ResponseModel<CheckListResponseModel>();
            var userId = LoggedInUserId.Value;

            if (model.Title.Length > 250)
            {
                response.Message = "Task check list model has problem.";
                response.AddError(_localizer["Check list title can not have more than 250 characters."]);
            }
            if (HasCheckListsTitleItemMoreCharacters(model.Items))
            {
                response.Message = "Task check list model has problem.";
                response.AddError(_localizer["Check list item title can not have more than 500 characters."]);
            }
            if (response.HasError)
                return StatusCode(StatusCodes.Status400BadRequest);


            var selectedTask = _vwsDbContext.GeneralTasks.FirstOrDefault(task => task.Id == id);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var newCheckList = new TaskCheckList()
            {
                CreatedBy = userId,
                CreatedOn = DateTime.UtcNow,
                IsDeleted = false,
                GeneralTaskId = id,
                ModifiedBy = userId,
                ModifiedOn = DateTime.UtcNow,
                Title = model.Title
            };
            _vwsDbContext.AddCheckList(newCheckList);
            selectedTask.ModifiedOn = DateTime.UtcNow;
            selectedTask.ModifiedBy = userId;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "{0} added check list {1} to task.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = newCheckList.Title,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var itemsResponse = AddCheckListItems(newCheckList.Id, model.Items);
            
            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, newCheckList.Title, selectedTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> added new check list with title <b>«{1}»</b> to your task with title <b>«{2}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Check list added successfully!";
            response.Value = new CheckListResponseModel()
            {
                CreatedBy = newCheckList.CreatedBy,
                CreatedOn = newCheckList.CreatedOn,
                GeneralTaskId = newCheckList.GeneralTaskId,
                ModifiedBy = newCheckList.ModifiedBy,
                ModifiedOn = newCheckList.ModifiedOn,
                Title = newCheckList.Title,
                Id = newCheckList.Id,
                Items = itemsResponse
            };
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateCheckListTitle")]
        public async Task<IActionResult> UpdateCheckListTitle(long checkListId, string newTitle)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            if (newTitle.Length > 250)
            {
                response.Message = "Task check list title has problem.";
                response.AddError(_localizer["Check list title can not have more than 250 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedCheckList = _vwsDbContext.TaskCheckLists.Include(checkList => checkList.GeneralTask)
                                                               .FirstOrDefault(checkList => checkList.Id == checkListId && !checkList.IsDeleted);

            if (selectedCheckList == null || selectedCheckList.IsDeleted)
            {
                response.AddError(_localizer["Check list with given id does not exist."]);
                response.Message = "Check list not found";
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            if (selectedCheckList.GeneralTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, selectedCheckList.GeneralTaskId))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedCheckList.Title == newTitle)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastTitle = selectedCheckList.Title;

            selectedCheckList.Title = newTitle;
            selectedCheckList.GeneralTask.ModifiedOn = DateTime.UtcNow;
            selectedCheckList.GeneralTask.ModifiedBy = userId;
            selectedCheckList.ModifiedBy = userId;
            selectedCheckList.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedCheckList.GeneralTask.ModifiedOn,
                Event = "Check list title updated from {0} to {1} by {2}.",
                GeneralTaskId = selectedCheckList.GeneralTaskId
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastTitle,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedCheckList.Title,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(selectedCheckList.GeneralTaskId).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedCheckList.GeneralTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, lastTitle, selectedCheckList.Title, selectedCheckList.GeneralTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> updated check list title from <b>«{1}»</b> to <b>«{2}»</b> in your task with title <b>«{3}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Check list title updated successfully!";
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("deleteCheckList")]
        public async Task<IActionResult> DeleteCheckList(long checkListId)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var selectedCheckList = _vwsDbContext.TaskCheckLists.Include(checkList => checkList.GeneralTask)
                                                               .FirstOrDefault(checkList => checkList.Id == checkListId && !checkList.IsDeleted);

            if (selectedCheckList == null || selectedCheckList.IsDeleted)
            {
                response.AddError(_localizer["Check list with given id does not exist."]);
                response.Message = "Check list not found";
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            if (selectedCheckList.GeneralTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, selectedCheckList.GeneralTaskId))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedCheckList.IsDeleted = true;
            selectedCheckList.GeneralTask.ModifiedOn = DateTime.UtcNow;
            selectedCheckList.GeneralTask.ModifiedBy = userId;
            selectedCheckList.ModifiedBy = userId;
            selectedCheckList.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedCheckList.GeneralTask.ModifiedOn,
                Event = "{0} deleted check list {1}.",
                GeneralTaskId = selectedCheckList.GeneralTaskId
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedCheckList.Title,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(selectedCheckList.GeneralTaskId).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedCheckList.GeneralTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, selectedCheckList.Title, selectedCheckList.GeneralTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> deleted check list with title <b>«{1}»</b> in your task with title <b>«{2}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Check list deleted successfully!";
            return Ok(response);
        }
        #endregion

        #region CheckListItemAPIS
        [HttpPost]
        [Authorize]
        [Route("addCheckListItem")]
        public async Task<IActionResult> AddCheckListItem(long checkListId, [FromBody] CheckListItemModel model)
        {
            var response = new ResponseModel<CheckListItemResponseModel>();
            var userId = LoggedInUserId.Value;

            if (model.Title.Length > 250)
            {
                response.Message = "Task check list item title has problem.";
                response.AddError(_localizer["Check list title can not have more than 250 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedCheckList = _vwsDbContext.TaskCheckLists.Include(checkList => checkList.GeneralTask)
                                                               .FirstOrDefault(checkList => checkList.Id == checkListId && !checkList.IsDeleted);

            if (selectedCheckList == null || selectedCheckList.IsDeleted)
            {
                response.AddError(_localizer["Check list with given id does not exist."]);
                response.Message = "Check list not found";
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            if (selectedCheckList.GeneralTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, selectedCheckList.GeneralTaskId))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var creationTime = DateTime.UtcNow;
            var newCheckListItem = new TaskCheckListItem()
            {
                CreatedBy = userId,
                CreatedOn = creationTime,
                IsDeleted = false,
                TaskCheckListId = checkListId,
                IsChecked = model.IsChecked,
                ModifiedBy = userId,
                ModifiedOn = creationTime,
                Title = model.Title
            };
            _vwsDbContext.AddCheckListItem(newCheckListItem);
            selectedCheckList.GeneralTask.ModifiedOn = DateTime.UtcNow;
            selectedCheckList.GeneralTask.ModifiedBy = userId;
            selectedCheckList.ModifiedBy = userId;
            selectedCheckList.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedCheckList.GeneralTask.ModifiedOn,
                Event = "{0} added check list item {1} to check list {2}.",
                GeneralTaskId = selectedCheckList.GeneralTaskId
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = newCheckListItem.Title,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedCheckList.Title,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(selectedCheckList.GeneralTaskId).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedCheckList.GeneralTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, newCheckListItem.Title, selectedCheckList.Title, selectedCheckList.GeneralTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> added new check list item with title <b>«{1}»</b> to check list with title <b>«{2}»</b> in your task with title <b>«{3}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Value = new CheckListItemResponseModel()
            {
                Id = newCheckListItem.Id,
                CreatedBy = newCheckListItem.CreatedBy,
                CreatedOn = newCheckListItem.CreatedOn,
                IsChecked = newCheckListItem.IsChecked,
                ModifiedBy = newCheckListItem.ModifiedBy,
                ModifiedOn = newCheckListItem.ModifiedOn,
                TaskCheckListId = newCheckListItem.TaskCheckListId,
                Title = newCheckListItem.Title
            };
            response.Message = "Check list item added successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateCheckListItemTitle")]
        public async Task<IActionResult> UpdateCheckListItemTitle(long checkListItemId, string newTitle)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            if (newTitle.Length > 500)
            {
                response.Message = "Task check list item title has problem.";
                response.AddError(_localizer["Check list item title can not have more than 500 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedCheckListItem = _vwsDbContext.TaskCheckListItems.Include(checkListItem => checkListItem.TaskCheckList)
                                                                       .ThenInclude(checkList => checkList.GeneralTask)
                                                                       .FirstOrDefault(checkListItem => checkListItem.Id == checkListItemId && !checkListItem.IsDeleted);

            if (selectedCheckListItem == null || selectedCheckListItem.IsDeleted)
            {
                response.AddError(_localizer["Check list item with given id does not exist."]);
                response.Message = "Check list not found";
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            if (selectedCheckListItem.TaskCheckList.GeneralTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, selectedCheckListItem.TaskCheckList.GeneralTask.Id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedCheckListItem.Title == newTitle)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastTitle = selectedCheckListItem.Title;

            selectedCheckListItem.Title = newTitle;
            selectedCheckListItem.ModifiedOn = DateTime.UtcNow;
            selectedCheckListItem.ModifiedBy = userId;
            selectedCheckListItem.TaskCheckList.ModifiedOn = DateTime.UtcNow;
            selectedCheckListItem.TaskCheckList.CreatedBy = userId;
            selectedCheckListItem.TaskCheckList.GeneralTask.ModifiedOn = DateTime.UtcNow;
            selectedCheckListItem.TaskCheckList.GeneralTask.CreatedBy = userId;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedCheckListItem.TaskCheckList.GeneralTask.ModifiedOn,
                Event = "Title of check list item updated from {0} to {1} by {2}.",
                GeneralTaskId = selectedCheckListItem.TaskCheckList.GeneralTaskId
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastTitle,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedCheckListItem.Title,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(selectedCheckListItem.TaskCheckList.GeneralTaskId).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedCheckListItem.TaskCheckList.GeneralTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, lastTitle, selectedCheckListItem.Title, selectedCheckListItem.TaskCheckList.Title, selectedCheckListItem.TaskCheckList.GeneralTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> updated check list item title from <b>«{1}»</b> to <b>«{2}»</b> in check list with title <b>«{3}»</b> of your task with title <b>«{4}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Check list item title updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateCheckListItemIsChecked")]
        public async Task<IActionResult> UpdateCheckListItemIsChecked(long checkListItemId, bool isChecked)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var selectedCheckListItem = _vwsDbContext.TaskCheckListItems.Include(checkListItem => checkListItem.TaskCheckList)
                                                                       .ThenInclude(checkList => checkList.GeneralTask)
                                                                       .FirstOrDefault(checkListItem => checkListItem.Id == checkListItemId && !checkListItem.IsDeleted);

            if (selectedCheckListItem == null || selectedCheckListItem.IsDeleted)
            {
                response.AddError(_localizer["Check list item with given id does not exist."]);
                response.Message = "Check list not found";
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            if (selectedCheckListItem.TaskCheckList.GeneralTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, selectedCheckListItem.TaskCheckList.GeneralTask.Id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedCheckListItem.IsChecked == isChecked)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastStatus = selectedCheckListItem.IsChecked;

            selectedCheckListItem.IsChecked = isChecked;
            selectedCheckListItem.ModifiedOn = DateTime.UtcNow;
            selectedCheckListItem.ModifiedBy = userId;
            selectedCheckListItem.TaskCheckList.ModifiedOn = DateTime.UtcNow;
            selectedCheckListItem.TaskCheckList.CreatedBy = userId;
            selectedCheckListItem.TaskCheckList.GeneralTask.ModifiedOn = DateTime.UtcNow;
            selectedCheckListItem.TaskCheckList.GeneralTask.CreatedBy = userId;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedCheckListItem.TaskCheckList.GeneralTask.ModifiedOn,
                Event = "Status of check list item {0} updated from {1} to {2} by {3}.",
                GeneralTaskId = selectedCheckListItem.TaskCheckList.GeneralTaskId
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedCheckListItem.Title,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastStatus ? "Done" : "UnderDone",
                TaskHistoryId = newTaskHistory.Id,
                ShouldBeLocalized = true
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedCheckListItem.IsChecked ? "Done" : "UnderDone",
                TaskHistoryId = newTaskHistory.Id,
                ShouldBeLocalized = true
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(selectedCheckListItem.TaskCheckList.GeneralTaskId).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedCheckListItem.TaskCheckList.GeneralTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, lastStatus ? "Done" : "UnderDone", selectedCheckListItem.IsChecked ? "Done" : "UnderDone", selectedCheckListItem.TaskCheckList.Title, selectedCheckListItem.TaskCheckList.GeneralTask.Title };
            bool[] arguemtsLocalize = { false, true, true, false, false };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> updated check list item status from <b>«{1}»</b> to <b>«{2}»</b> in check list with title <b>«{3}»</b> of your task with title <b>«{4}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Check list item is checked updated successfully!";
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("deleteCheckListItem")]
        public async Task<IActionResult> DeleteCheckListItem(long checkListItemId)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var selectedCheckListItem = _vwsDbContext.TaskCheckListItems.Include(checkListItem => checkListItem.TaskCheckList)
                                                                       .ThenInclude(checkList => checkList.GeneralTask)
                                                                       .FirstOrDefault(checkListItem => checkListItem.Id == checkListItemId && !checkListItem.IsDeleted);

            if (selectedCheckListItem == null || selectedCheckListItem.IsDeleted)
            {
                response.AddError(_localizer["Check list item with given id does not exist."]);
                response.Message = "Check list not found";
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            if (selectedCheckListItem.TaskCheckList.GeneralTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, selectedCheckListItem.TaskCheckList.GeneralTask.Id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedCheckListItem.IsDeleted = true;
            selectedCheckListItem.ModifiedOn = DateTime.UtcNow;
            selectedCheckListItem.ModifiedBy = userId;
            selectedCheckListItem.TaskCheckList.ModifiedOn = DateTime.UtcNow;
            selectedCheckListItem.TaskCheckList.CreatedBy = userId;
            selectedCheckListItem.TaskCheckList.GeneralTask.ModifiedOn = DateTime.UtcNow;
            selectedCheckListItem.TaskCheckList.GeneralTask.CreatedBy = userId;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedCheckListItem.TaskCheckList.GeneralTask.ModifiedOn,
                Event = "Check list item {0} deleted by {1}.",
                GeneralTaskId = selectedCheckListItem.TaskCheckList.GeneralTaskId
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedCheckListItem.Title,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(selectedCheckListItem.TaskCheckList.GeneralTaskId).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedCheckListItem.TaskCheckList.GeneralTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, selectedCheckListItem.Title, selectedCheckListItem.TaskCheckList.Title, selectedCheckListItem.TaskCheckList.GeneralTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> deleted check list item with title <b>«{1}»</b> in your check list with title <b>«{2}»</b> of <b>«{3}»</b> task.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Check list item delete successfully!";
            return Ok(response);
        }
        #endregion

        #region TagAPIS
        [HttpPost]
        [Authorize]
        [Route("addTag")]
        public IActionResult AddTag([FromBody] TagModel model)
        {
            var response = new ResponseModel<TagResponseModel>();
            var userId = LoggedInUserId.Value;

            if (!String.IsNullOrEmpty(model.Color) && model.Color.Length > 6)  
                response.AddError(_localizer["Length of color is more than 6 characters."]);
            if (!String.IsNullOrEmpty(model.Title) && model.Title.Length > 100)
                response.AddError(_localizer["Length of title is more than 100 characters."]);
            if (response.HasError)
            {
                response.Message = "Tag model data has problem.";
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            if (model.TeamId != null && model.ProjectId != null)
                model.TeamId = null;

            #region CheckTeamAndProjectExistance
            if (model.ProjectId != null && !_vwsDbContext.Projects.Any(p => p.Id == model.ProjectId && !p.IsDeleted))
            {
                response.Message = "Project not found";
                response.AddError(_localizer["Project not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (model.TeamId != null && !_vwsDbContext.Teams.Any(t => t.Id == model.TeamId && !t.IsDeleted))
            {
                response.Message = "Team not found";
                response.AddError(_localizer["Team not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            #endregion

            #region CheckTeamAndProjectAccess
            if (model.ProjectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)model.ProjectId))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You do not have access to project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            if (model.TeamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)model.TeamId))
            {
                response.Message = "Team access denied";
                response.AddError(_localizer["You do not have access to team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            bool isForUser = (model.ProjectId == null && model.TeamId == null) ? true : false;

            Tag newTag = new Tag()
            {
                ProjectId = model.ProjectId,
                TeamId = model.TeamId,
                UserProfileId = isForUser ? userId : (Guid?)null,
                Title = model.Title,
                Color = model.Color
            };
            _vwsDbContext.AddTag(newTag);
            _vwsDbContext.Save();

            response.Value = new TagResponseModel() { Id = newTag.Id, Title = newTag.Title, Color = newTag.Color };
            response.Message = "New tag added successfully!";
            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        [Route("addTagToTask")]
        public async Task<IActionResult> AddTagToTask(long id, int tagId)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var selectedTask = _vwsDbContext.GeneralTasks.FirstOrDefault(task => task.Id == id);
            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var selectedTag = _vwsDbContext.Tags.FirstOrDefault(tag => tag.Id == tagId);
            if (selectedTag == null)
            {
                response.Message = "Status not found!";
                response.AddError(_localizer["Status not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            #region CheckAccess
            if ((selectedTag.ProjectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)selectedTag.ProjectId)) ||
                (selectedTag.TeamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)selectedTag.TeamId)) ||
                selectedTag.UserProfileId != null && selectedTag.UserProfileId != LoggedInUserId.Value)
            {
                response.Message = "Task tag access denied";
                response.AddError(_localizer["You do not have access to task tag."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            int[] tagArray = { tagId };
            if (!AreTagsValid(tagArray, selectedTask.ProjectId, selectedTask.TeamId))
            {
                response.Message = "Invalid tags";
                response.AddError(_localizer["Invalid tags."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (_vwsDbContext.TaskTags.Any(taskTag => taskTag.GeneralTaskId == id && taskTag.TagId == tagId))
            {
                response.Message = "Tag was assigned before";
                response.AddError(_localizer["Task already has the tag."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            AddTaskTags(id, tagArray);
            selectedTask.ModifiedOn = DateTime.UtcNow;
            selectedTask.ModifiedBy = userId;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "{0} added tag {1} to task.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedTag.Title,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, selectedTag.Title, selectedTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> added new tag <b>«{1}»</b> to your task with title <b>«{2}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "New tag added to task";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateTagTitle")]
        public IActionResult UpdateTagTitle(int tagId, string newTitle)
        {
            var response = new ResponseModel();

            if (!String.IsNullOrEmpty(newTitle) && newTitle.Length > 100)
            {
                response.AddError(_localizer["Length of title is more than 100 characters."]);
                response.Message = "Tag model data has problem.";
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            var selectedTag = _vwsDbContext.Tags.FirstOrDefault(tag => tag.Id == tagId);
            if (selectedTag == null)
            {
                response.Message = "Status not found!";
                response.AddError(_localizer["Status not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            #region CheckAccess
            if ((selectedTag.ProjectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)selectedTag.ProjectId)) ||
                (selectedTag.TeamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)selectedTag.TeamId)) ||
                selectedTag.UserProfileId != null && selectedTag.UserProfileId != LoggedInUserId.Value)
            {
                response.Message = "Task tag access denied";
                response.AddError(_localizer["You do not have access to task tag."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            if (selectedTag.Title == newTitle)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            selectedTag.Title = newTitle;
            _vwsDbContext.Save();

            response.Message = "Tag title updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateTagColor")]
        public IActionResult UpdateTagColor(int tagId, string newColor)
        {
            var response = new ResponseModel();

            if (!String.IsNullOrEmpty(newColor) && newColor.Length > 6)
            {
                response.AddError(_localizer["Length of color is more than 6 characters."]);
                response.Message = "Tag model data has problem.";
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            var selectedTag = _vwsDbContext.Tags.FirstOrDefault(tag => tag.Id == tagId);
            if (selectedTag == null)
            {
                response.Message = "Status not found!";
                response.AddError(_localizer["Status not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            #region CheckAccess
            if ((selectedTag.ProjectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)selectedTag.ProjectId)) ||
                (selectedTag.TeamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)selectedTag.TeamId)) ||
                selectedTag.UserProfileId != null && selectedTag.UserProfileId != LoggedInUserId.Value)
            {
                response.Message = "Task tag access denied";
                response.AddError(_localizer["You do not have access to task tag."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            if (selectedTag.Color == newColor)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            selectedTag.Color = newColor;
            _vwsDbContext.Save();

            response.Message = "Tag color updated successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getTags")]
        public IActionResult GetTags(int? projectId, int? teamId)
        {
            var response = new ResponseModel<List<TagResponseModel>>();

            if (teamId != null && projectId != null)
                teamId = null;

            #region CheckTeamAndProjectExistance
            if (projectId != null && !_vwsDbContext.Projects.Any(p => p.Id == projectId && !p.IsDeleted))
            {
                response.Message = "Project not found";
                response.AddError(_localizer["Project not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (teamId != null && !_vwsDbContext.Teams.Any(t => t.Id == teamId && !t.IsDeleted))
            {
                response.Message = "Team not found";
                response.AddError(_localizer["Team not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            #endregion

            #region CheckTeamAndProjectAccess
            if (projectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)projectId))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You do not have access to project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            if (teamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)teamId))
            {
                response.Message = "Team access denied";
                response.AddError(_localizer["You do not have access to team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            response.Value = GetAvailableTags(projectId, teamId);
            response.Message = "Tags returned successfully!";
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("deleteTag")]
        public IActionResult DeleteTag(int tagId)
        {
            var response = new ResponseModel();

            var selectedTag = _vwsDbContext.Tags.FirstOrDefault(tag => tag.Id == tagId);
            if (selectedTag == null)
            {
                response.Message = "Status not found!";
                response.AddError(_localizer["Status not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            #region CheckAccess
            if ((selectedTag.ProjectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)selectedTag.ProjectId)) ||
                (selectedTag.TeamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)selectedTag.TeamId)) ||
                selectedTag.UserProfileId != null && selectedTag.UserProfileId != LoggedInUserId.Value)
            {
                response.Message = "Task tag access denied";
                response.AddError(_localizer["You do not have access to task tag."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            _vwsDbContext.DeleteTag(tagId);
            _vwsDbContext.Save();

            response.Message = "Tag deleted successfully!";
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("deleteTagFromTask")]
        public async Task<IActionResult> DeleteTagFromTask(long id, int tagId)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var selectedTask = _vwsDbContext.GeneralTasks.FirstOrDefault(task => task.Id == id);
            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var selectedTag = _vwsDbContext.Tags.FirstOrDefault(tag => tag.Id == tagId);
            if (selectedTag == null)
            {
                response.Message = "Status not found!";
                response.AddError(_localizer["Status not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            #region CheckAccess
            if ((selectedTag.ProjectId != null && !_permissionService.HasAccessToProject(LoggedInUserId.Value, (int)selectedTag.ProjectId)) ||
                (selectedTag.TeamId != null && !_permissionService.HasAccessToTeam(LoggedInUserId.Value, (int)selectedTag.TeamId)) ||
                selectedTag.UserProfileId != null && selectedTag.UserProfileId != LoggedInUserId.Value)
            {
                response.Message = "Task tag access denied";
                response.AddError(_localizer["You do not have access to task tag."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            var selectedTaskTag = _vwsDbContext.TaskTags.FirstOrDefault(taskTag => taskTag.GeneralTaskId == id && taskTag.TagId == tagId);

            if (selectedTag == null)
            {
                response.Message = "Tag was not assigned before";
                response.AddError(_localizer["Task does not have the tag."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var tagName = selectedTag.Title;

            _vwsDbContext.DeleteTaskTag(selectedTaskTag.GeneralTaskId, selectedTaskTag.TagId);
            selectedTask.ModifiedBy = userId;
            selectedTask.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "{0} removed tag {1} from task.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedTag.Title,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, selectedTag.Title, selectedTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> deleted tag <b>«{1}»</b> from your task with title <b>«{2}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Task tag deleted";
            return Ok(response);
        }
        #endregion

        #region CommentAPIS

        [HttpPost]
        [Authorize]
        [Route("addComment")]
        public async Task<IActionResult> AddComment([FromBody] TaskCommentModel model)
        {
            var response = new ResponseModel<CommentResponseModel>();

            if (String.IsNullOrEmpty(model.Body) || model.Body.Length > 1000)
            {
                response.AddError(_localizer["Comment body can not be emty or have more than 1000 characters."]);
                response.Message = "Invalid comment body";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var userId = LoggedInUserId.Value;

            var selectedTask = _vwsDbContext.GeneralTasks.FirstOrDefault(task => task.Id == model.Id);
            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTask(userId, model.Id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var creationTime = DateTime.UtcNow;
            var newComment = new TaskComment()
            {
                Body = model.Body,
                CommentedBy = userId,
                CommentedOn = creationTime,
                ModifiedOn = creationTime,
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskComment(newComment);
            selectedTask.ModifiedOn = DateTime.UtcNow;
            selectedTask.ModifiedBy = userId;
            _vwsDbContext.Save();
            AddCommentAttachments(newComment.Id, model.Attachments);

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "{0} added comment {1} to task.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = newComment.Body,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(model.Id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, newComment.Body, selectedTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> added new comment <b>«{1}»</b> to your task with title <b>«{2}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            UserProfile userProfile = await _vwsDbContext.GetUserProfileAsync(newComment.CommentedBy);
            response.Value = new CommentResponseModel()
            {
                Id = newComment.Id,
                Body = newComment.Body,
                CommentedBy = new UserModel()
                {
                    UserId = newComment.CommentedBy,
                    NickName = userProfile.NickName,
                    ProfileImageGuid = userProfile.ProfileImageGuid
                },
                CommentedOn = newComment.CommentedOn,
                MidifiedOn = newComment.ModifiedOn,
                Attachments = _taskManager.GetCommentAttachments(newComment.Id)
            };
            response.Message = "Comment added successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("editComment")]
        public async Task<IActionResult> EditComment(long commentId, string newBody)
        {
            var response = new ResponseModel();

            if (String.IsNullOrEmpty(newBody) || newBody.Length > 1000)
            {
                response.AddError(_localizer["Comment body can not be emty or have more than 1000 characters."]);
                response.Message = "Invalid comment body";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var userId = LoggedInUserId.Value;

            var selectedComment = _vwsDbContext.TaskComments.Include(comment => comment.GeneralTask).FirstOrDefault(comment => comment.Id == commentId);
            if (selectedComment == null)
            {
                response.AddError(_localizer["Task comment not found."]);
                response.Message = "Task comment not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (selectedComment.CommentedBy != userId)
            {
                response.AddError(_localizer["Task comment access denied."]);
                response.Message = "Task comment access denied";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if ((DateTime.UtcNow - selectedComment.ModifiedOn).TotalMinutes < Int16.Parse(_configuration["TimeDifference:EditTaskComment"]))
            {
                response.AddError(_localizer["You can not edit and delete comment before 5 minutes of your last change."]);
                response.Message = "Can not edit and delete comment before 5 minutes of your last change";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedComment.Body == newBody)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            string lastBody = selectedComment.Body;

            selectedComment.Body = newBody;
            selectedComment.ModifiedOn = DateTime.UtcNow;
            selectedComment.GeneralTask.ModifiedOn = DateTime.UtcNow;
            selectedComment.GeneralTask.ModifiedBy = userId;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedComment.GeneralTask.ModifiedOn,
                Event = "{0} updated comment from {1} to {2}.",
                GeneralTaskId = selectedComment.GeneralTaskId
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastBody,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedComment.Body,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(selectedComment.GeneralTaskId).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedComment.GeneralTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, lastBody, selectedComment.Body, selectedComment.GeneralTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> updated comment from <b>«{1}»</b> to <b>«{2}»</b> in your task with title <b>«{3}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Comment body updated successfully";
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("deleteComment")]
        public async Task<IActionResult> DeleteComment(long commentId)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            var selectedComment = _vwsDbContext.TaskComments.Include(comment => comment.GeneralTask).FirstOrDefault(comment => comment.Id == commentId);
            if (selectedComment == null)
            {
                response.AddError(_localizer["Task comment not found."]);
                response.Message = "Task comment not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (selectedComment.CommentedBy != userId)
            {
                response.AddError(_localizer["Task comment access denied."]);
                response.Message = "Task comment access denied";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if ((DateTime.UtcNow - selectedComment.ModifiedOn).TotalMinutes < Int16.Parse(_configuration["TimeDifference:EditTaskComment"]))
            {
                response.AddError(_localizer["You can not edit and delete comment before 5 minutes of your last change."]);
                response.Message = "Can not edit and delete comment before 5 minutes of your last change";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            DeleteTaskComment(commentId);
            selectedComment.ModifiedOn = DateTime.UtcNow;
            selectedComment.GeneralTask.ModifiedOn = DateTime.UtcNow;
            selectedComment.GeneralTask.ModifiedBy = userId;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedComment.GeneralTask.ModifiedOn,
                Event = "{0} deleted comment {1}.",
                GeneralTaskId = selectedComment.GeneralTaskId
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedComment.Body,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(selectedComment.GeneralTaskId).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedComment.GeneralTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, selectedComment.Body, selectedComment.GeneralTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> deleted comment <b>«{1}»</b> in your task with title <b>«{2}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            response.Message = "Comment body deleted successfully";
            return Ok(response);
        }
        #endregion

        #region CommentAttachmentAPIS
        [HttpPost]
        [Authorize]
        [Route("addAtachmentToComment")]
        public async Task<IActionResult> AddAtachmentToComment([FromBody] AddAtachmentModel model)
        {
            var response = new ResponseModel<List<FileModel>>();

            var userId = LoggedInUserId.Value;

            var selectedComment = _vwsDbContext.TaskComments.Include(comment => comment.GeneralTask).FirstOrDefault(comment => comment.Id == model.Id);
            if (selectedComment == null)
            {
                response.AddError(_localizer["Task comment not found."]);
                response.Message = "Task comment not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (selectedComment.CommentedBy != userId)
            {
                response.AddError(_localizer["Task comment access denied."]);
                response.Message = "Task comment access denied";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            response.Value = AddCommentAttachments(selectedComment.Id, model.Attachments);
            response.Message = "Attachments added successfully";

            if (response.Value.Count() != 0)
            {
                selectedComment.ModifiedOn = DateTime.UtcNow;
                selectedComment.GeneralTask.ModifiedOn = DateTime.UtcNow;
                selectedComment.GeneralTask.ModifiedBy = userId;
                _vwsDbContext.Save();
                string links = "";
                string emailMessage = "<b>«{0}»</b> added below attachments to comment <b>«{1}»</b> in your task with title <b>«{2}»</b>:\n <br> {3}";
                foreach (var file in response.Value)
                    links += $"<a href='{Request.Scheme}://{Request.Host}/en-US/File/get?id={file.FileContainerGuid}'>{file.Name}</a>\n<br>\n";

                #region History
                var newTaskHistory = new TaskHistory()
                {
                    EventTime = selectedComment.GeneralTask.ModifiedOn,
                    Event = "{0} added below attachments to comment {1}: {2}",
                    GeneralTaskId = selectedComment.GeneralTaskId
                };
                _vwsDbContext.AddTaskHistory(newTaskHistory);
                _vwsDbContext.Save();
                var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
                _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                    Body = JsonConvert.SerializeObject(new UserModel()
                    {
                        NickName = user.NickName,
                        ProfileImageGuid = user.ProfileImageGuid,
                        UserId = user.UserId
                    }),
                    TaskHistoryId = newTaskHistory.Id
                });
                _vwsDbContext.Save();
                _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                    Body = selectedComment.Body,
                    TaskHistoryId = newTaskHistory.Id
                });
                _vwsDbContext.Save();
                _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.ListOfFiles,
                    Body = JsonConvert.SerializeObject(response.Value),
                    TaskHistoryId = newTaskHistory.Id
                });
                _vwsDbContext.Save();
                #endregion

                var usersAssignedTo = _taskManager.GetAssignedTo(selectedComment.GeneralTaskId).Select(user => user.UserId).ToList();
                usersAssignedTo.Add(selectedComment.GeneralTask.CreatedBy);
                usersAssignedTo = usersAssignedTo.Distinct().ToList();
                usersAssignedTo.Remove(LoggedInUserId.Value);
                string[] arguments = { LoggedInNickName, selectedComment.Body, selectedComment.GeneralTask.Title, links };
                await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, emailMessage, "Task Update", arguments);

                _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);
            }

            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("deleteAttachmentFromComment")]
        public async Task<IActionResult> DeleteAttachmentFromComment(long commentId, Guid attachmentGuid)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            var selectedComment = _vwsDbContext.TaskComments.Include(comment => comment.GeneralTask).FirstOrDefault(comment => comment.Id == commentId);
            if (selectedComment == null)
            {
                response.AddError(_localizer["Task comment not found."]);
                response.Message = "Task comment not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (selectedComment.CommentedBy != userId)
            {
                response.AddError(_localizer["Task comment access denied."]);
                response.Message = "Task comment access denied";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedAttachment = _vwsDbContext.TaskCommentAttachments.Include(attachment => attachment.FileContainer)
                                                                        .ThenInclude(attachment => attachment.Files)
                                                                        .FirstOrDefault(attachment => attachment.FileContainerGuid == attachmentGuid && attachment.TaskCommentId == commentId);
            if (selectedAttachment == null)
            {
                response.AddError(_localizer["There is no attachment with such information for given comment."]);
                response.Message = "Attachment not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            foreach (var file in selectedAttachment.FileContainer.Files)
                _fileManager.DeleteFile(file.Address);

            var selectedContainer = _vwsDbContext.FileContainers.FirstOrDefault(container => container.Id == selectedAttachment.FileContainerId);
            var fileName = _vwsDbContext.Files.FirstOrDefault(file => file.Id == selectedContainer.RecentFileId).Name;
            _vwsDbContext.DeleteFileContainer(selectedContainer);
            selectedComment.ModifiedOn = DateTime.UtcNow;
            selectedComment.GeneralTask.ModifiedOn = DateTime.UtcNow;
            selectedComment.GeneralTask.ModifiedBy = userId;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedComment.GeneralTask.ModifiedOn,
                Event = "{0} deleted attchment {1} from comment {2}.",
                GeneralTaskId = selectedComment.GeneralTaskId
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = fileName,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedComment.Body,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(selectedComment.GeneralTaskId).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedComment.GeneralTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, fileName, selectedComment.Body, selectedComment.GeneralTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> deleted attachment <b>«{1}»</b> from comment <b>«{2}»</b> in your task with title <b>«{3}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            return Ok(response);
        }
        #endregion

        #region TaskAttachmentAPIS
        [HttpPost]
        [Authorize]
        [Route("addAtachment")]
        public async Task<IActionResult> AddAtachment([FromBody] AddAtachmentModel model)
        {
            var response = new ResponseModel<List<FileModel>>();

            var userId = LoggedInUserId.Value;

            var selectedTask = _vwsDbContext.GeneralTasks.Include(task => task.TaskAttachments).FirstOrDefault(task => task.Id == model.Id);
            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (!_permissionService.HasAccessToTask(userId, model.Id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            response.Value = AddTaskAttachments(selectedTask.Id, model.Attachments);
            response.Message = "Attachments added successfully";

            if (response.Value.Count() != 0)
            {
                selectedTask.ModifiedOn = DateTime.UtcNow;
                selectedTask.ModifiedBy = userId;
                _vwsDbContext.Save();
                string links = "";
                string emailMessage = "<b>«{0}»</b> added below attachments to your task with title <b>«{1}»</b>: \n <br> {3}";
                foreach (var file in response.Value)
                    links += $"<a href='{Request.Scheme}://{Request.Host}/en-US/File/get?id={file.FileContainerGuid}'>{file.Name}</a>\n<br>\n";

                #region History
                var newTaskHistory = new TaskHistory()
                {
                    EventTime = selectedTask.ModifiedOn,
                    Event = "{0} added below attachments to task: {1}",
                    GeneralTaskId = selectedTask.Id
                };
                _vwsDbContext.AddTaskHistory(newTaskHistory);
                _vwsDbContext.Save();
                var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
                _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                    Body = JsonConvert.SerializeObject(new UserModel()
                    {
                        NickName = user.NickName,
                        ProfileImageGuid = user.ProfileImageGuid,
                        UserId = user.UserId
                    }),
                    TaskHistoryId = newTaskHistory.Id
                });
                _vwsDbContext.Save();
                _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
                {
                    ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.ListOfFiles,
                    Body = JsonConvert.SerializeObject(response.Value),
                    TaskHistoryId = newTaskHistory.Id
                });
                _vwsDbContext.Save();
                #endregion

                var usersAssignedTo = _taskManager.GetAssignedTo(selectedTask.Id).Select(user => user.UserId).ToList();
                usersAssignedTo.Add(selectedTask.CreatedBy);
                usersAssignedTo = usersAssignedTo.Distinct().ToList();
                usersAssignedTo.Remove(LoggedInUserId.Value);
                string[] arguments = { LoggedInNickName, selectedTask.Title, links };
                await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, emailMessage, "Task Update", arguments);

                _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);
            }

            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("deleteAttachment")]
        public async Task<IActionResult> DeleteAttachment(long id, Guid attachmentGuid)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            var selectedTask = _vwsDbContext.GeneralTasks.FirstOrDefault(task => task.Id == id);
            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(_localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (!_permissionService.HasAccessToTask(userId, id))
            {
                response.Message = "Task access forbidden";
                response.AddError(_localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var selectedAttachment = _vwsDbContext.TaskAttachments.Include(attachment => attachment.FileContainer)
                                                                  .ThenInclude(attachment => attachment.Files)
                                                                  .FirstOrDefault(attachment => attachment.FileContainerGuid == attachmentGuid && attachment.GeneralTaskId == id);

            if (selectedAttachment == null)
            {
                response.AddError(_localizer["There is no attachment with such information for given task."]);
                response.Message = "Attachment not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            foreach (var file in selectedAttachment.FileContainer.Files)
                _fileManager.DeleteFile(file.Address);

            var selectedContainer = _vwsDbContext.FileContainers.FirstOrDefault(container => container.Id == selectedAttachment.FileContainerId);
            var fileName = _vwsDbContext.Files.FirstOrDefault(file => file.Id == selectedContainer.RecentFileId).Name;
            _vwsDbContext.DeleteFileContainer(selectedContainer);
            selectedTask.ModifiedOn = DateTime.UtcNow;
            selectedTask.ModifiedBy = userId;
            _vwsDbContext.Save();

            #region History
            var newTaskHistory = new TaskHistory()
            {
                EventTime = selectedTask.ModifiedOn,
                Event = "{0} deleted attchment {1} from task.",
                GeneralTaskId = selectedTask.Id
            };
            _vwsDbContext.AddTaskHistory(newTaskHistory);
            _vwsDbContext.Save();
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddTaskHistoryParameter(new TaskHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = fileName,
                TaskHistoryId = newTaskHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var usersAssignedTo = _taskManager.GetAssignedTo(selectedTask.Id).Select(user => user.UserId).ToList();
            usersAssignedTo.Add(selectedTask.CreatedBy);
            usersAssignedTo = usersAssignedTo.Distinct().ToList();
            usersAssignedTo.Remove(LoggedInUserId.Value);
            string[] arguments = { LoggedInNickName, fileName, selectedTask.Title };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, usersAssignedTo, "<b>«{0}»</b> deleted attachment <b>«{1}»</b> from your task with title <b>«{2}»</b>.", "Task Update", arguments);

            _notificationService.SendMultipleNotification(usersAssignedTo, (byte)SeedDataEnum.NotificationTypes.Task, newTaskHistory.Id);

            return Ok(response);
        }
        #endregion
    }
}
