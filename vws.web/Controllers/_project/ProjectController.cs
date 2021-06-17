using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._file;
using vws.web.Domain._project;
using vws.web.Domain._team;
using vws.web.Enums;
using vws.web.Models;
using vws.web.Models._project;
using vws.web.Models._task;
using vws.web.Repositories;
using vws.web.Services;
using vws.web.Services._chat;
using vws.web.Services._project;
using vws.web.Services._task;
using static vws.web.EmailTemplates.EmailTemplateTypes;

namespace vws.web.Controllers._project
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class ProjectController : BaseController
    {
        #region Feilds
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<ProjectController> _localizer;
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly IFileManager _fileManager;
        private readonly IPermissionService _permissionService;
        private readonly IProjectManagerService _projectManager;
        private readonly ITaskManagerService _taskManager;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        #endregion

        #region Ctor
        public ProjectController(UserManager<ApplicationUser> userManager, IStringLocalizer<ProjectController> localizer,
            IVWS_DbContext vwsDbContext, IFileManager fileManager, IPermissionService permissionService,
            IProjectManagerService projectManager, ITaskManagerService taskManagerService,
            INotificationService notificationService, IUserService userService)
        {
            _userManager = userManager;
            _localizer = localizer;
            _vwsDbContext = vwsDbContext;
            _fileManager = fileManager;
            _permissionService = permissionService;
            _projectManager = projectManager;
            _taskManager = taskManagerService;
            _notificationService = notificationService;
            _userService = userService;
        }
        #endregion

        #region PrivateMethods
        private List<string> CheckProjectModel(ProjectModel model)
        {
            var result = new List<string>();

            if (!String.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
                result.Add(_localizer["Length of description is more than 2000 characters."]);

            if (String.IsNullOrEmpty(model.Name))
                result.Add(_localizer["Name can not be empty."]);

            if (!String.IsNullOrEmpty(model.Name) && model.Name.Length > 500)
                result.Add(_localizer["Length of title is more than 500 characters."]);

            if (!String.IsNullOrEmpty(model.Color) && model.Color.Length > 6)
                result.Add(_localizer["Length of color is more than 6 characters."]);

            if (model.StartDate.HasValue && model.EndDate.HasValue && model.StartDate > model.EndDate)
                result.Add(_localizer["Start Date should be before End Date."]);

            return result;
        }

        private List<string> CheckAreDepartmentsForTeam(int teamId, List<int> departmentIds)
        {
            var result = new List<string>();

            foreach (var departmentId in departmentIds)
            {
                var selectedDepartment = _vwsDbContext.Departments.FirstOrDefault(department => department.Id == departmentId);

                if (selectedDepartment.TeamId != teamId)
                    result.Add(String.Format(_localizer["Department with name {0} is not for selected team."], selectedDepartment.Name));
            }

            return result;
        }

        private List<string> CheckDepartmentExistency(List<int> departmentIds)
        {
            var result = new List<string>();

            foreach (var departmentId in departmentIds)
            {
                var selectedDepartment = _vwsDbContext.Departments.FirstOrDefault(department => department.Id == departmentId);
                if (selectedDepartment == null || selectedDepartment.IsDeleted)
                    result.Add(String.Format(_localizer["There is no department with id {0}."], departmentId));
            }

            return result;
        }

        private List<string> CheckBegingAMemberOfDepartment(Guid userId, List<int> departmentIds)
        {
            var result = new List<string>();

            foreach (var departmentId in departmentIds)
            {
                var selectedDepartmentMember = _vwsDbContext.DepartmentMembers.FirstOrDefault(departmentMember => departmentMember.UserProfileId == userId &&
                                                                                                                 departmentMember.DepartmentId == departmentId &&
                                                                                                                 departmentMember.IsDeleted == false);

                var selectedDepartment = _vwsDbContext.Departments.FirstOrDefault(department => department.Id == departmentId);

                if (selectedDepartmentMember == null)
                    result.Add(String.Format(_localizer["You are not member of department with name {0}."], selectedDepartment.Name));
            }

            return result;
        }

        private List<Guid> GetAvailableUsersToAddProject(int? projectId)
        {
            var availableUsers = new List<Guid>();
            var projectUsers = new List<Guid>();

            if (projectId != null)
                projectUsers = _vwsDbContext.ProjectMembers.Where(projectMember => projectMember.ProjectId == projectId &&
                                                                                   projectMember.IsPermittedByCreator == true &&
                                                                                   !projectMember.IsDeleted)
                                                          .Select(projectMember => projectMember.UserProfileId).ToList();

            List<Team> userTeams = _vwsDbContext.GetUserTeams(LoggedInUserId.Value).ToList();
            List<Guid> userTeamMates = _vwsDbContext.TeamMembers
                .Where(teamMember => userTeams.Select(userTeam => userTeam.Id).Contains(teamMember.TeamId) && !teamMember.IsDeleted)
                .Select(teamMember => teamMember.UserProfileId).Distinct().Where(id => id != LoggedInUserId.Value).ToList();

            availableUsers = userTeamMates.Except(projectUsers).ToList();

            return availableUsers;
        }

        private async Task AddProjectUsers(int projectId, List<Guid> users)
        {
            var creationTime = DateTime.UtcNow;
            foreach (var user in users)
            {
                await _vwsDbContext.AddProjectMemberAsync(new ProjectMember()
                {
                    CreatedOn = creationTime,
                    IsDeleted = false,
                    IsPermittedByCreator = true,
                    UserProfileId = user,
                    ProjectId = projectId,
                    PermittedOn = creationTime
                });
                if (user != LoggedInUserId.Value)
                    _vwsDbContext.AddUsersActivity(new UsersActivity() { Time = creationTime, TargetUserId = user, OwnerUserId = LoggedInUserId.Value });
            }
            _vwsDbContext.Save();
        }

        private void CreateProjectTaskStatuses(int projectId)
        {
            _vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 2, ProjectId = projectId, UserProfileId = null, TeamId = null, Title = "To Do" });
            _vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 4, ProjectId = projectId, UserProfileId = null, TeamId = null, Title = "Doing" });
            _vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 6, ProjectId = projectId, UserProfileId = null, TeamId = null, Title = "Done"});

            _vwsDbContext.Save();
        }

        private void DeleteProjectTasks(int projectId, DateTime deleteTime)
        {
            var projectTasks = _vwsDbContext.GeneralTasks.Where(task => task.ProjectId == projectId && !task.IsDeleted);

            foreach (var projectTask in projectTasks)
            {
                projectTask.IsDeleted = true;
                projectTask.ModifiedBy = LoggedInUserId.Value;
                projectTask.ModifiedOn = deleteTime;
            }
            _vwsDbContext.Save();
        }

        private void DeleteProjectEvents(int projectId, DateTime daleteTime)
        {
            var relatedEvents = _vwsDbContext.Events.Include(_event => _event.EventProjects)
                                                    .Where(_event => !_event.IsDeleted &&
                                                                     _event.EventProjects.Count > 0 &&
                                                                     _event.EventProjects.Select(eventProject => eventProject.ProjectId)
                                                                                         .Contains(projectId))
                                                    .ToList();

            foreach (var relatedEvent in relatedEvents)
            {
                if (relatedEvent.EventProjects.Count == 1)
                {
                    relatedEvent.IsDeleted = true;
                    relatedEvent.ModifiedOn = daleteTime;
                    relatedEvent.ModifiedBy = LoggedInUserId.Value;
                }
                else
                {
                    var selectedEventProject = _vwsDbContext.EventProjects.FirstOrDefault(eventProject => eventProject.EventId == relatedEvent.Id && eventProject.ProjectId == projectId);
                    _vwsDbContext.RemoveEventProject(selectedEventProject);
                }
            }
            _vwsDbContext.Save();
        }

        #endregion

        #region ProjectAPIS
        [HttpPost]
        [Authorize]
        [Route("create")]
        public async Task<IActionResult> CreateProject([FromBody] ProjectModel model)
        {
            var response = new ResponseModel<ProjectResponseModel>();

            var checkModelResult = CheckProjectModel(model);

            if (checkModelResult.Count != 0)
            {
                response.Message = "Invalid project model";
                response.AddErrors(checkModelResult);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            Guid userId = LoggedInUserId.Value;

            if (model.TeamId != null)
            {
                var selectedTeam = await _vwsDbContext.GetTeamAsync((int)model.TeamId);
                if (selectedTeam == null || selectedTeam.IsDeleted)
                {
                    response.AddError(_localizer["There is no team with given Id."]);
                    response.Message = "Team not found";
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }
                var selectedTeamMember = await _vwsDbContext.GetTeamMemberAsync((int)model.TeamId, userId);
                if (selectedTeamMember == null)
                {
                    response.AddError(_localizer["You are not a member of team."]);
                    response.Message = "Not member of team";
                    return StatusCode(StatusCodes.Status403Forbidden, response);
                }

                var checkDepartmentExistencytResult = CheckDepartmentExistency(model.DepartmentIds);

                if (checkDepartmentExistencytResult.Count != 0)
                {
                    response.AddErrors(checkDepartmentExistencytResult);
                    response.Message = "Department not found";
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }

                var checkBeingMemberOfDepartmentsResult = CheckBegingAMemberOfDepartment(userId, model.DepartmentIds);

                if (checkBeingMemberOfDepartmentsResult.Count != 0)
                {
                    response.AddErrors(checkBeingMemberOfDepartmentsResult);
                    response.Message = "Department access denied";
                    return StatusCode(StatusCodes.Status403Forbidden, response);
                }

                var checkDepartmentAreForTeamResult = CheckAreDepartmentsForTeam((int)model.TeamId, model.DepartmentIds);

                if (checkDepartmentAreForTeamResult.Count != 0)
                {
                    response.AddErrors(checkDepartmentAreForTeamResult);
                    response.Message = "Department not for team";
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }
            }
            else if (model.DepartmentIds.Count != 0)
            {
                response.AddError(_localizer["If your project is under department, you should specify the team."]);
                response.Message = "Invalid projectmodel";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            model.Users.Add(userId);
            model.Users = model.Users.Distinct().ToList();

            DateTime creationTime = DateTime.UtcNow;

            var newProject = new Project()
            {
                Name = model.Name,
                StatusId = (byte)SeedDataEnum.ProjectStatuses.Active,
                Description = model.Description,
                Color = model.Color,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Guid = Guid.NewGuid(),
                IsDeleted = false,
                CreatedBy = userId,
                ModifiedBy = userId,
                CreatedOn = creationTime,
                ModifiedOn = creationTime,
                TeamId = model.TeamId
            };

            await _vwsDbContext.AddProjectAsync(newProject);
            _vwsDbContext.Save();

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = newProject.Id,
                Event = "Project {0} created by {1}.",
                EventTime = creationTime
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                ProjectHistoryId = newProjectHistory.Id,
                Body = newProject.Name
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                })
            });
            _vwsDbContext.Save();

            foreach (var departmentId in model.DepartmentIds)
                _vwsDbContext.AddProjectDepartment(new ProjectDepartment { DepartmentId = departmentId, ProjectId = newProject.Id });

            if (newProject.TeamId == null)
                await AddProjectUsers(newProject.Id, model.Users);

            CreateProjectTaskStatuses(newProject.Id);

            var newProjectResponse = new ProjectResponseModel()
            {
                Id = newProject.Id,
                StatusId = newProject.StatusId,
                Name = newProject.Name,
                Description = newProject.Description,
                Color = newProject.Color,
                StartDate = newProject.StartDate,
                EndDate = newProject.EndDate,
                Guid = newProject.Guid,
                TeamId = newProject.TeamId,
                TeamName = newProject.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == newProject.TeamId).Name,
                ProjectImageGuid = newProject.ProjectImageGuid,
                DepartmentIds = model.DepartmentIds,
                NumberOfUpdates = _vwsDbContext.ProjectHistories.Where(history => history.ProjectId == newProject.Id).Count(),
                Users = _projectManager.GetProjectUsers(newProject.Id),
                NumberOfTasks = _projectManager.GetNumberOfProjectTasks(newProject.Id),
                SpentTimeInMinutes = _projectManager.GetProjectSpentTime(newProject.Id),
                CreatedBy = _userService.GetUser(newProject.CreatedBy),
                ModifiedBy = _userService.GetUser(newProject.ModifiedBy),
                CreatedOn = newProject.CreatedOn,
                ModifiedOn = newProject.ModifiedOn
            };

            var users = _projectManager.GetProjectUsers(newProject.Id).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> created new project with name <b>«{1}»</b>.";
            string[] arguments = { LoggedInNickName, newProject.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Create", arguments);

            _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

            response.Value = newProjectResponse;
            response.Message = "Project created successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateName")]
        public async Task<IActionResult> UpdateName(int id, [FromBody] StringModel model)
        {
            string newName = model.Value;
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == id);
            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.Message = "Project not found";
                response.AddError(_localizer["There is no project with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToProject(userId, id))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You are not a memeber of project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (String.IsNullOrEmpty(newName))
                response.AddError(_localizer["Name can not be empty."]);

            if (!String.IsNullOrEmpty(newName) && newName.Length > 500)
                response.AddError(_localizer["Length of title is more than 500 characters."]);

            if (response.HasError)
            {
                response.Message = "Invalid model.";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedProject.Name == newName)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastName = selectedProject.Name;

            selectedProject.Name = newName;
            selectedProject.ModifiedOn = DateTime.UtcNow;
            selectedProject.ModifiedBy = userId;
            _vwsDbContext.Save();

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "Project name updated from {0} to {1} by {2}.",
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                ProjectHistoryId = newProjectHistory.Id,
                Body = lastName
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                ProjectHistoryId = newProjectHistory.Id,
                Body = selectedProject.Name
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                })
            });
            _vwsDbContext.Save();

            var users = _projectManager.GetProjectUsers(selectedProject.Id).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated project name from <b>«{1}»</b> to <b>«{2}»</b>.";
            string[] arguments = { LoggedInNickName, lastName, selectedProject.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments);

            _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

            response.Message = "Name updated successfully!";
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

            var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == id);
            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.Message = "Project not found";
                response.AddError(_localizer["There is no project with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToProject(userId, id))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You are not a memeber of project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (!String.IsNullOrEmpty(newDescription) && newDescription.Length > 2000)
            {
                response.AddError(_localizer["Length of description is more than 2000 characters."]);
                response.Message = "Invalid model.";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedProject.Description == newDescription)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastDescription = selectedProject.Description;

            selectedProject.Description = newDescription;
            selectedProject.ModifiedOn = DateTime.UtcNow;
            selectedProject.ModifiedBy = userId;
            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "Project description updated to {0} by {1}.",
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                ProjectHistoryId = newProjectHistory.Id,
                Body = selectedProject.Description
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                ProjectHistoryId = newProjectHistory.Id,
                Body = selectedProject.Name
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                })
            });
            _vwsDbContext.Save();

            var users = _projectManager.GetProjectUsers(selectedProject.Id).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated description from <b>«{1}»</b> to <b>«{2}»</b> in project <b>«{3}»</b>.";
            string[] arguments = { LoggedInNickName, lastDescription, selectedProject.Description, selectedProject.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments);

            _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

            response.Message = "Description updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateColor")]
        public async Task<IActionResult> UpdateColor(int id, [FromBody] StringModel model)
        {
            string newColor = model.Value;
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == id);
            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.Message = "Project not found";
                response.AddError(_localizer["There is no project with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToProject(userId, id))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You are not a memeber of project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (!String.IsNullOrEmpty(newColor) && newColor.Length > 6)
            {
                response.AddError(_localizer["Length of color is more than 6 characters."]);
                response.Message = "Invalid model.";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedProject.Color == newColor)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastColor = selectedProject.Color;

            selectedProject.Color = newColor;
            selectedProject.ModifiedOn = DateTime.UtcNow;
            selectedProject.ModifiedBy = userId;
            _vwsDbContext.Save();

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "Project color updated from {0} to {1} by {2}.",
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Color,
                ProjectHistoryId = newProjectHistory.Id,
                Body = lastColor
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Color,
                ProjectHistoryId = newProjectHistory.Id,
                Body = selectedProject.Color
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                })
            });
            _vwsDbContext.Save();

            var users = _projectManager.GetProjectUsers(selectedProject.Id).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated color from <b>«{1}»</b> to <b>«{2}»</b> in project <b>«{3}»</b>.";
            string[] arguments = { LoggedInNickName, lastColor, selectedProject.Color, selectedProject.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments);

            _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

            response.Message = "Color updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateStartDate")]
        public async Task<IActionResult> UpdateStartDate(int id, DateTime? newStartDate)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == id);
            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.Message = "Project not found";
                response.AddError(_localizer["There is no project with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToProject(userId, id))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You are not a memeber of project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (newStartDate.HasValue && selectedProject.EndDate.HasValue && newStartDate > selectedProject.EndDate)
            {
                response.Message = "Invalid model";
                response.AddError(_localizer["Start Date should be before End Date."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedProject.StartDate == newStartDate)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastStartDate = selectedProject.StartDate;

            selectedProject.StartDate = newStartDate;
            selectedProject.ModifiedOn = DateTime.UtcNow;
            selectedProject.ModifiedBy = userId;
            _vwsDbContext.Save();

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "Project start date updated from {0} to {1} by {2}.",
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                ProjectHistoryId = newProjectHistory.Id,
                Body = lastStartDate == null ? "NoTime" : lastStartDate.ToString(),
                ShouldBeLocalized = lastStartDate == null ? true : false
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                ProjectHistoryId = newProjectHistory.Id,
                Body = selectedProject.StartDate == null ? "NoTime" : selectedProject.StartDate.ToString(),
                ShouldBeLocalized = selectedProject.StartDate == null ? true : false
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                })
            });
            _vwsDbContext.Save();

            var users = _projectManager.GetProjectUsers(selectedProject.Id).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated start date from <b>«{1}»</b> to <b>«{2}»</b> in project <b>«{3}»</b>.";
            string[] arguments = { LoggedInNickName, lastStartDate == null ? "No Time" : lastStartDate.ToString(), selectedProject.StartDate == null ? "No Time" : selectedProject.StartDate.ToString(), selectedProject.Name };
            bool[] argumentLocalization = { false, lastStartDate == null ? true : false, selectedProject.StartDate == null ? true : false, false };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments, argumentLocalization);

            _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

            response.Message = "Start date updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateEndDate")]
        public async Task<IActionResult> UpdateEndDate(int id, DateTime? newEndDate)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == id);
            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.Message = "Project not found";
                response.AddError(_localizer["There is no project with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToProject(userId, id))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You are not a memeber of project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (newEndDate.HasValue && selectedProject.StartDate.HasValue && selectedProject.StartDate > newEndDate)
            {
                response.Message = "Invalid model";
                response.AddError(_localizer["Start Date should be before End Date."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedProject.EndDate == newEndDate)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastEndDate = selectedProject.EndDate;

            selectedProject.EndDate = newEndDate;
            selectedProject.ModifiedOn = DateTime.UtcNow;
            selectedProject.ModifiedBy = userId;
            _vwsDbContext.Save();

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "Project end date updated from {0} to {1} by {2}.",
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                ProjectHistoryId = newProjectHistory.Id,
                Body = lastEndDate == null ? "NoTime" : lastEndDate.ToString()
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                ProjectHistoryId = newProjectHistory.Id,
                Body = selectedProject.EndDate == null ? "NoTime" : selectedProject.EndDate.ToString()
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                })
            });
            _vwsDbContext.Save();

            var users = _projectManager.GetProjectUsers(selectedProject.Id).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated end date from <b>«{1}»</b> to <b>«{2}»</b> in project <b>«{3}»</b>.";
            string[] arguments = { LoggedInNickName, lastEndDate == null ? "No Time" : lastEndDate.ToString(), selectedProject.EndDate == null ? "No Time" : selectedProject.EndDate.ToString(), selectedProject.Name };
            bool[] argumentLocalization = { false, lastEndDate == null ? true : false, selectedProject.EndDate == null ? true : false, false };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments, argumentLocalization);

            _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

            response.Message = "End date updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateTeamAndDepartments")]
        public async Task<IActionResult> UpdateTeamAndDepartments([FromBody] UpdateTeamAndDepartmentsModel model)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            if (model.TeamId != null)
            {
                var selectedTeam = await _vwsDbContext.GetTeamAsync((int)model.TeamId);
                if (selectedTeam == null || selectedTeam.IsDeleted)
                {
                    response.AddError(_localizer["There is no team with given Id."]);
                    response.Message = "Team not found";
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }
                var selectedTeamMember = await _vwsDbContext.GetTeamMemberAsync((int)model.TeamId, userId);
                if (selectedTeamMember == null)
                {
                    response.AddError(_localizer["You are not a member of team."]);
                    response.Message = "Not member of team";
                    return StatusCode(StatusCodes.Status403Forbidden, response);
                }

                var checkDepartmentExistencytResult = CheckDepartmentExistency(model.DepartmentIds);

                if (checkDepartmentExistencytResult.Count != 0)
                {
                    response.AddErrors(checkDepartmentExistencytResult);
                    response.Message = "Department not found";
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }

                var checkBeingMemberOfDepartmentsResult = CheckBegingAMemberOfDepartment(userId, model.DepartmentIds);

                if (checkBeingMemberOfDepartmentsResult.Count != 0)
                {
                    response.AddErrors(checkBeingMemberOfDepartmentsResult);
                    response.Message = "Department access denied";
                    return StatusCode(StatusCodes.Status403Forbidden, response);
                }

                var checkDepartmentAreForTeamResult = CheckAreDepartmentsForTeam((int)model.TeamId, model.DepartmentIds);

                if (checkDepartmentAreForTeamResult.Count != 0)
                {
                    response.AddErrors(checkDepartmentAreForTeamResult);
                    response.Message = "Department not for team";
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }
            }
            else if (model.DepartmentIds.Count != 0)
            {
                response.AddError(_localizer["If your project is under department, you should specify the team."]);
                response.Message = "Invalid projectmodel";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedProject = _vwsDbContext.Projects.Include(project => project.ProjectDepartments).FirstOrDefault(project => project.Id == model.Id);
            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.Message = "Project not found";
                response.AddError(_localizer["There is no project with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToProject(userId, model.Id))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You are not a memeber of project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            bool isProjectPersonal = selectedProject.TeamId == null ? true : false;
            bool willBeProjectPersonal = model.TeamId == null ? true : false;

            if (!isProjectPersonal && willBeProjectPersonal)
            {
                response.AddError(_localizer["You can not change non-personal project to personal project."]);
                response.Message = "Non-personal project to project";
                return StatusCode(StatusCodes.Status406NotAcceptable, response);
            }

            List<int> projectDepartmentsIds = selectedProject.ProjectDepartments.Select(projectDepartment => projectDepartment.DepartmentId).ToList();

            List<int> shouldBeRemoved = projectDepartmentsIds.Except(model.DepartmentIds).ToList();
            List<int> shouldBeAdded = model.DepartmentIds.Except(projectDepartmentsIds).ToList();

            if ((isProjectPersonal && willBeProjectPersonal) || (selectedProject.TeamId == model.TeamId && shouldBeAdded.Count == 0 && shouldBeRemoved.Count == 0))
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            foreach (var rmProjectDepartment in shouldBeRemoved)
            {
                var selectedProjectDepartment = _vwsDbContext.ProjectDepartments
                                                            .FirstOrDefault(projectDepartment => projectDepartment.DepartmentId == rmProjectDepartment &&
                                                                                                 projectDepartment.ProjectId == model.Id);

                _vwsDbContext.DeleteProjectDepartment(selectedProjectDepartment);
            }

            foreach (var addProjectDepartment in shouldBeAdded)
                _vwsDbContext.AddProjectDepartment(new ProjectDepartment() { ProjectId = model.Id, DepartmentId = addProjectDepartment });

            if (isProjectPersonal && !willBeProjectPersonal)
            {
                var projectMembers = _vwsDbContext.ProjectMembers.Where(projectMember => projectMember.ProjectId == model.Id);
                foreach (var projectMember in projectMembers)
                    _vwsDbContext.DeleteProjectMember(projectMember);
            }

            selectedProject.TeamId = model.TeamId;
            selectedProject.ModifiedOn = DateTime.UtcNow;
            selectedProject.ModifiedBy = userId;
            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "Project team and departments updated by {0}.",
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                })
            });
            _vwsDbContext.Save();

            var users = _projectManager.GetProjectUsers(selectedProject.Id).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated project team and department in project <b>«{1}»</b>.";
            string[] arguments = { LoggedInNickName, selectedProject.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments);

            _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

            response.Message = "Project team and departments updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("uploadProjectImage")]
        public async Task<IActionResult> UploadProjectImage(IFormFile image, int projectId)
        {
            var response = new ResponseModel<Guid>();

            string[] types = { "png", "jpg", "jpeg" };

            var files = Request.Form.Files.ToList();

            Guid userId = LoggedInUserId.Value;

            if (files.Count > 1)
            {
                response.AddError(_localizer["There is more than one file."]);
                response.Message = "Too many files passed";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (files.Count == 0 && image == null)
            {
                response.AddError(_localizer["You did not upload an image."]);
                response.Message = "There is no image";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            var uploadedImage = files.Count == 0 ? image : files[0];

            var selectedProject = _vwsDbContext.Projects.Include(project => project.ProjectImage).FirstOrDefault(project => project.Id == projectId);
            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.AddError(_localizer["There is no project with given Id."]);
                response.Message = "Project not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToProject(userId, projectId))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You are not a memeber of project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            bool isProjectPersonal = selectedProject.TeamId == null ? true : false;

            if (isProjectPersonal && userId != selectedProject.CreatedBy)
            {
                response.AddError(_localizer["Updating personal project just can be done by creator."]);
                response.Message = "Update project access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            ResponseModel<File> fileResponse;

            if (selectedProject.ProjectImage != null)
            {
                fileResponse = await _fileManager.WriteFile(uploadedImage, userId, "profileImages", (int)selectedProject.ProjectImageId, types.ToList());
                if (fileResponse.HasError)
                {
                    foreach (var error in fileResponse.Errors)
                        response.AddError(_localizer[error]);
                    response.Message = "Error in writing file";
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
                selectedProject.ProjectImage.RecentFileId = fileResponse.Value.Id;
            }
            else
            {
                var time = DateTime.UtcNow;
                var newFileContainer = new FileContainer
                {
                    ModifiedOn = time,
                    CreatedOn = time,
                    CreatedBy = userId,
                    ModifiedBy = userId,
                    Guid = Guid.NewGuid()
                };
                await _vwsDbContext.AddFileContainerAsync(newFileContainer);
                _vwsDbContext.Save();
                fileResponse = await _fileManager.WriteFile(uploadedImage, userId, "profileImages", newFileContainer.Id, types.ToList());
                if (fileResponse.HasError)
                {
                    foreach (var error in fileResponse.Errors)
                        response.AddError(_localizer[error]);
                    response.Message = "Error in writing file";
                    _vwsDbContext.DeleteFileContainer(newFileContainer);
                    _vwsDbContext.Save();
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
                newFileContainer.RecentFileId = fileResponse.Value.Id;
                selectedProject.ProjectImageId = newFileContainer.Id;
                selectedProject.ProjectImageGuid = newFileContainer.Guid;
            }
            selectedProject.ModifiedBy = LoggedInUserId.Value;
            selectedProject.ModifiedOn = DateTime.UtcNow;

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = projectId,
                Event = "Project image updated to {0} by {1}.",
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.File,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new FileModel()
                {
                    Name = fileResponse.Value.Name,
                    Extension = fileResponse.Value.Extension,
                    FileContainerGuid = fileResponse.Value.FileContainerGuid,
                    Size = fileResponse.Value.Size
                })
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                })
            });
            _vwsDbContext.Save();

            var users = _projectManager.GetProjectUsers(selectedProject.Id).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated project image to <b>«{1}»</b> in project <b>«{2}»</b>.";
            string[] arguments = { LoggedInNickName, $"<a href='{Request.Scheme}://{Request.Host}/en-US/File/get?id={fileResponse.Value.FileContainerGuid}'>{fileResponse.Value.Name}</a>", selectedProject.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments);

            _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

            response.Value = fileResponse.Value.FileContainerGuid;
            response.Message = "Project image added successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("changeStatus")]
        public async Task<IActionResult> ChangeProjectStatus(int id, byte statusId)
        {
            var response = new ResponseModel();

            var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == id);
            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.Message = "Project not found";
                response.AddError(_localizer["There is no project with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (statusId < 1 || statusId > 3)
            {
                response.Message = "Invalid tatus id";
                response.AddError(_localizer["Status id is not valid."]);
            }

            var userId = LoggedInUserId.Value;

            if (!_permissionService.HasAccessToProject(userId, id))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You are not a memeber of project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            bool isProjectPersonal = selectedProject.TeamId == null ? true : false;

            if (isProjectPersonal && userId != selectedProject.CreatedBy)
            {
                response.AddError(_localizer["Updating personal project just can be done by creator."]);
                response.Message = "Update project access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedProject.StatusId == statusId)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastStatus = selectedProject.StatusId;

            selectedProject.StatusId = statusId;
            selectedProject.ModifiedOn = DateTime.UtcNow;
            selectedProject.ModifiedBy = userId;
            _vwsDbContext.Save();

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = id,
                Event = "Project status changed from {0} to {1} by {2}.",
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                ProjectHistoryId = newProjectHistory.Id,
                Body = ((SeedDataEnum.ProjectStatuses)lastStatus).ToString(),
                ShouldBeLocalized = true
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                ProjectHistoryId = newProjectHistory.Id,
                Body = ((SeedDataEnum.ProjectStatuses)selectedProject.StatusId).ToString(),
                ShouldBeLocalized = true
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                })
            });
            _vwsDbContext.Save();

            var users = _projectManager.GetProjectUsers(selectedProject.Id).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated status from <b>«{1}»</b> to <b>«{2}»</b> in project <b>«{3}»</b>.";
            string[] arguments = { LoggedInNickName, ((SeedDataEnum.ProjectStatuses)lastStatus).ToString(), ((SeedDataEnum.ProjectStatuses)selectedProject.StatusId).ToString(), selectedProject.Name };
            bool[] argumentLocalization = { false, true, true, false };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments, argumentLocalization);

            _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

            response.Message = "Project status updated successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public ProjectResponseModel Get(int Id)
        {
            Guid userId = LoggedInUserId.Value;

            var project = _projectManager.GetAllUserProjects(userId).FirstOrDefault(project => project.Id == Id);
            if (project == null)
                return null;

            var response = new ProjectResponseModel()
            {
                Id = project.Id,
                Description = project.Description,
                Color = project.Color,
                EndDate = project.EndDate,
                Guid = project.Guid,
                Name = project.Name,
                StartDate = project.StartDate,
                StatusId = project.StatusId,
                TeamId = project.TeamId,
                TeamName = project.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == project.TeamId).Name,
                ProjectImageGuid = project.ProjectImageGuid,
                DepartmentIds = project.ProjectDepartments.Select(projectDepartment => projectDepartment.DepartmentId).ToList(),
                NumberOfUpdates = _vwsDbContext.ProjectHistories.Where(history => history.ProjectId == project.Id).Count(),
                Users = _projectManager.GetProjectUsers(project.Id),
                NumberOfTasks = _projectManager.GetNumberOfProjectTasks(project.Id),
                SpentTimeInMinutes = _projectManager.GetProjectSpentTime(project.Id),
                CreatedBy = _userService.GetUser(project.CreatedBy),
                ModifiedBy = _userService.GetUser(project.ModifiedBy),
                CreatedOn = project.CreatedOn,
                ModifiedOn = project.ModifiedOn
            };

            return response;
        }

        [HttpGet]
        [Authorize]
        [Route("getActive")]
        public IEnumerable<ProjectResponseModel> GetActiveProject()
        {
            var response = new List<ProjectResponseModel>();
            Guid userId = LoggedInUserId.Value;

            var userProjects = _projectManager.GetAllUserProjects(userId).Where(project => project.StatusId == (byte)SeedDataEnum.ProjectStatuses.Active).ToList();

            var userProjectOrders = _vwsDbContext.UserProjectOrders.Include(userProjectOrder => userProjectOrder.Project)
                                                                   .Where(userProjectOrder => userProjectOrder.UserProfileId == userId && userProjectOrder.Project.StatusId == (byte)SeedDataEnum.ProjectStatuses.Active)
                                                                   .ToList();

            var validProjectOrdersProjects = userProjectOrders.Where(userProjectOrder => userProjects.Contains(userProjectOrder.Project))
                                                              .OrderBy(userProjectOrder => userProjectOrder.Order)
                                                              .Select(userProjectOrder => userProjectOrder.Project)
                                                              .ToList();

            var userProjectsNotIncluded = validProjectOrdersProjects.Count == userProjects.Count ? new List<Project>() : userProjects.Except(validProjectOrdersProjects);

            foreach (var project in validProjectOrdersProjects)
            {
                response.Add(new ProjectResponseModel()
                {
                    Id = project.Id,
                    Description = project.Description,
                    Color = project.Color,
                    EndDate = project.EndDate,
                    Guid = project.Guid,
                    Name = project.Name,
                    StartDate = project.StartDate,
                    StatusId = project.StatusId,
                    TeamId = project.TeamId,
                    TeamName = project.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == project.TeamId).Name,
                    ProjectImageGuid = project.ProjectImageGuid,
                    DepartmentIds = project.ProjectDepartments.Select(projectDepartment => projectDepartment.DepartmentId).ToList(),
                    NumberOfUpdates = _vwsDbContext.ProjectHistories.Where(history => history.ProjectId == project.Id).Count(),
                    Users = _projectManager.GetProjectUsers(project.Id),
                    NumberOfTasks = _projectManager.GetNumberOfProjectTasks(project.Id),
                    SpentTimeInMinutes = _projectManager.GetProjectSpentTime(project.Id),
                    CreatedBy = _userService.GetUser(project.CreatedBy),
                    ModifiedBy = _userService.GetUser(project.ModifiedBy),
                    CreatedOn = project.CreatedOn,
                    ModifiedOn = project.ModifiedOn
                });
            }
            foreach (var project in userProjectsNotIncluded)
            {
                response.Add(new ProjectResponseModel()
                {
                    Id = project.Id,
                    Description = project.Description,
                    Color = project.Color,
                    EndDate = project.EndDate,
                    Guid = project.Guid,
                    Name = project.Name,
                    StartDate = project.StartDate,
                    StatusId = project.StatusId,
                    TeamId = project.TeamId,
                    TeamName = project.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == project.TeamId).Name,
                    ProjectImageGuid = project.ProjectImageGuid,
                    DepartmentIds = project.ProjectDepartments.Select(projectDepartment => projectDepartment.DepartmentId).ToList(),
                    NumberOfUpdates = _vwsDbContext.ProjectHistories.Where(history => history.ProjectId == project.Id).Count(),
                    Users = _projectManager.GetProjectUsers(project.Id),
                    NumberOfTasks = _projectManager.GetNumberOfProjectTasks(project.Id),
                    SpentTimeInMinutes = _projectManager.GetProjectSpentTime(project.Id),
                    CreatedBy = _userService.GetUser(project.CreatedBy),
                    ModifiedBy = _userService.GetUser(project.ModifiedBy),
                    CreatedOn = project.CreatedOn,
                    ModifiedOn = project.ModifiedOn
                });
            }

            return response;
        }

        [HttpGet]
        [Authorize]
        [Route("getNumberOfActiveProjects")]
        public int GetNumberOfActiveProjects()
        {
            Guid userId = LoggedInUserId.Value;

            return _projectManager.GetAllUserProjects(userId).Where(project => project.StatusId == (byte)SeedDataEnum.ProjectStatuses.Active).Count(); 
        }

        [HttpGet]
        [Authorize]
        [Route("getHold")]
        public IEnumerable<ProjectResponseModel> GetHoldProject()
        {
            var response = new List<ProjectResponseModel>();
            Guid userId = LoggedInUserId.Value;

            var userProjects = _projectManager.GetAllUserProjects(userId).Where(project => project.StatusId == (byte)SeedDataEnum.ProjectStatuses.Hold).ToList();

            var userProjectOrders = _vwsDbContext.UserProjectOrders.Include(userProjectOrder => userProjectOrder.Project)
                                                                   .Where(userProjectOrder => userProjectOrder.UserProfileId == userId && userProjectOrder.Project.StatusId == (byte)SeedDataEnum.ProjectStatuses.Hold)
                                                                   .ToList();

            var validProjectOrdersProjects = userProjectOrders.Where(userProjectOrder => userProjects.Contains(userProjectOrder.Project))
                                                              .OrderBy(userProjectOrder => userProjectOrder.Order)
                                                              .Select(userProjectOrder => userProjectOrder.Project)
                                                              .ToList();

            var userProjectsNotIncluded = validProjectOrdersProjects.Count == userProjects.Count ? new List<Project>() : userProjects.Except(validProjectOrdersProjects);

            foreach (var project in validProjectOrdersProjects)
            {
                response.Add(new ProjectResponseModel()
                {
                    Id = project.Id,
                    Description = project.Description,
                    Color = project.Color,
                    EndDate = project.EndDate,
                    Guid = project.Guid,
                    Name = project.Name,
                    StartDate = project.StartDate,
                    StatusId = project.StatusId,
                    TeamId = project.TeamId,
                    TeamName = project.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == project.TeamId).Name,
                    ProjectImageGuid = project.ProjectImageGuid,
                    DepartmentIds = project.ProjectDepartments.Select(projectDepartment => projectDepartment.DepartmentId).ToList(),
                    NumberOfUpdates = _vwsDbContext.ProjectHistories.Where(history => history.ProjectId == project.Id).Count(),
                    Users = _projectManager.GetProjectUsers(project.Id),
                    NumberOfTasks = _projectManager.GetNumberOfProjectTasks(project.Id),
                    SpentTimeInMinutes = _projectManager.GetProjectSpentTime(project.Id),
                    CreatedBy = _userService.GetUser(project.CreatedBy),
                    ModifiedBy = _userService.GetUser(project.ModifiedBy),
                    CreatedOn = project.CreatedOn,
                    ModifiedOn = project.ModifiedOn
                });
            }
            foreach (var project in userProjectsNotIncluded)
            {
                response.Add(new ProjectResponseModel()
                {
                    Id = project.Id,
                    Description = project.Description,
                    Color = project.Color,
                    EndDate = project.EndDate,
                    Guid = project.Guid,
                    Name = project.Name,
                    StartDate = project.StartDate,
                    StatusId = project.StatusId,
                    TeamId = project.TeamId,
                    TeamName = project.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == project.TeamId).Name,
                    ProjectImageGuid = project.ProjectImageGuid,
                    DepartmentIds = project.ProjectDepartments.Select(projectDepartment => projectDepartment.DepartmentId).ToList(),
                    NumberOfUpdates = _vwsDbContext.ProjectHistories.Where(history => history.ProjectId == project.Id).Count(),
                    Users = _projectManager.GetProjectUsers(project.Id),
                    NumberOfTasks = _projectManager.GetNumberOfProjectTasks(project.Id),
                    SpentTimeInMinutes = _projectManager.GetProjectSpentTime(project.Id),
                    CreatedBy = _userService.GetUser(project.CreatedBy),
                    ModifiedBy = _userService.GetUser(project.ModifiedBy),
                    CreatedOn = project.CreatedOn,
                    ModifiedOn = project.ModifiedOn
                });
            }

            return response;
        }

        [HttpGet]
        [Authorize]
        [Route("getDoneOrArchived")]
        public IEnumerable<ProjectResponseModel> GetDoneOrArchivedProject()
        {
            var response = new List<ProjectResponseModel>();
            Guid userId = LoggedInUserId.Value;

            var userProjects = _projectManager.GetAllUserProjects(userId).Where(project => project.StatusId == (byte)SeedDataEnum.ProjectStatuses.DoneOrArchived).ToList();

            var userProjectOrders = _vwsDbContext.UserProjectOrders.Include(userProjectOrder => userProjectOrder.Project)
                                                                   .Where(userProjectOrder => userProjectOrder.UserProfileId == userId && userProjectOrder.Project.StatusId == (byte)SeedDataEnum.ProjectStatuses.DoneOrArchived)
                                                                   .ToList();

            var validProjectOrdersProjects = userProjectOrders.Where(userProjectOrder => userProjects.Contains(userProjectOrder.Project))
                                                              .OrderBy(userProjectOrder => userProjectOrder.Order)
                                                              .Select(userProjectOrder => userProjectOrder.Project)
                                                              .ToList();

            var userProjectsNotIncluded = validProjectOrdersProjects.Count == userProjects.Count ? new List<Project>() : userProjects.Except(validProjectOrdersProjects);

            foreach (var project in validProjectOrdersProjects)
            {
                response.Add(new ProjectResponseModel()
                {
                    Id = project.Id,
                    Description = project.Description,
                    Color = project.Color,
                    EndDate = project.EndDate,
                    Guid = project.Guid,
                    Name = project.Name,
                    StartDate = project.StartDate,
                    StatusId = project.StatusId,
                    TeamId = project.TeamId,
                    TeamName = project.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == project.TeamId).Name,
                    ProjectImageGuid = project.ProjectImageGuid,
                    DepartmentIds = project.ProjectDepartments.Select(projectDepartment => projectDepartment.DepartmentId).ToList(),
                    NumberOfUpdates = _vwsDbContext.ProjectHistories.Where(history => history.ProjectId == project.Id).Count(),
                    Users = _projectManager.GetProjectUsers(project.Id),
                    NumberOfTasks = _projectManager.GetNumberOfProjectTasks(project.Id),
                    SpentTimeInMinutes = _projectManager.GetProjectSpentTime(project.Id),
                    CreatedBy = _userService.GetUser(project.CreatedBy),
                    ModifiedBy = _userService.GetUser(project.ModifiedBy),
                    CreatedOn = project.CreatedOn,
                    ModifiedOn = project.ModifiedOn
                });
            }
            foreach (var project in userProjectsNotIncluded)
            {
                response.Add(new ProjectResponseModel()
                {
                    Id = project.Id,
                    Description = project.Description,
                    Color = project.Color,
                    EndDate = project.EndDate,
                    Guid = project.Guid,
                    Name = project.Name,
                    StartDate = project.StartDate,
                    StatusId = project.StatusId,
                    TeamId = project.TeamId,
                    TeamName = project.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == project.TeamId).Name,
                    ProjectImageGuid = project.ProjectImageGuid,
                    DepartmentIds = project.ProjectDepartments.Select(projectDepartment => projectDepartment.DepartmentId).ToList(),
                    NumberOfUpdates = _vwsDbContext.ProjectHistories.Where(history => history.ProjectId == project.Id).Count(),
                    Users = _projectManager.GetProjectUsers(project.Id),
                    NumberOfTasks = _projectManager.GetNumberOfProjectTasks(project.Id),
                    SpentTimeInMinutes = _projectManager.GetProjectSpentTime(project.Id),
                    CreatedBy = _userService.GetUser(project.CreatedBy),
                    ModifiedBy = _userService.GetUser(project.ModifiedBy),
                    CreatedOn = project.CreatedOn,
                    ModifiedOn = project.ModifiedOn
                });
            }

            return response;
        }

        [HttpGet]
        [Authorize]
        [Route("getTasks")]
        public async Task<IActionResult> GetProjectTasks(int id)
        {
            Guid userId = LoggedInUserId.Value;

            var response = new ResponseModel<List<TaskResponseModel>>();
            List<TaskResponseModel> result = new List<TaskResponseModel>();

            if (!_vwsDbContext.Projects.Any(p => p.Id == id && !p.IsDeleted))
            {
                response.Message = "Project not found";
                response.AddError(_localizer["Project not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToProject(userId, id))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You do not have access to project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var projectTasks = _vwsDbContext.GeneralTasks.Where(task => task.ProjectId == id && !task.IsArchived && !task.IsDeleted);
            projectTasks = projectTasks.OrderByDescending(task => task.CreatedOn);

            foreach (var projectTask in projectTasks)
            {
                result.Add(new TaskResponseModel()
                {
                    Id = projectTask.Id,
                    Title = projectTask.Title,
                    Description = projectTask.Description,
                    StartDate = projectTask.StartDate,
                    EndDate = projectTask.EndDate,
                    CreatedOn = projectTask.CreatedOn,
                    ModifiedOn = projectTask.ModifiedOn,
                    CreatedBy = _userService.GetUser(projectTask.CreatedBy),
                    ModifiedBy = _userService.GetUser(projectTask.ModifiedBy),
                    Guid = projectTask.Guid,
                    PriorityId = projectTask.TaskPriorityId,
                    PriorityTitle = _localizer[((SeedDataEnum.TaskPriority)projectTask.TaskPriorityId).ToString()],
                    UsersAssignedTo = _taskManager.GetAssignedTo(projectTask.Id),
                    ProjectId = projectTask.ProjectId,
                    TeamId = projectTask.TeamId,
                    TeamName = projectTask.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == projectTask.TeamId).Name,
                    ProjectName = projectTask.ProjectId == null ? null : _vwsDbContext.Projects.FirstOrDefault(project => project.Id == projectTask.ProjectId).Name,
                    StatusId = projectTask.TaskStatusId,
                    StatusTitle = _vwsDbContext.TaskStatuses.FirstOrDefault(statuse => statuse.Id == projectTask.TaskStatusId).Title,
                    CheckLists = _taskManager.GetCheckLists(projectTask.Id),
                    Tags = _taskManager.GetTaskTags(projectTask.Id),
                    Comments = await _taskManager.GetTaskComments(projectTask.Id),
                    Attachments = _taskManager.GetTaskAttachments(projectTask.Id),
                    IsUrgent = projectTask.IsUrgent
                });
            }
            response.Value = result;
            response.Message = "Project tasks returned successfull!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getProjectHistory")]
        public IActionResult GetProjectHistory(int id)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel<List<HistoryModel>>();

            var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == id);

            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.Message = "Project not found";
                response.AddError(_localizer["There is no project with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToProject(userId, id))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You are not a memeber of project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var events = new List<HistoryModel>();
            var projectEvents = _vwsDbContext.ProjectHistories.Where(projectHistory => projectHistory.ProjectId == id).OrderByDescending(projectHistory => projectHistory.EventTime);
            foreach (var projectEvent in projectEvents)
            {
                var parameters = _vwsDbContext.ProjectHistoryParameters.Where(param => param.ProjectHistoryId == projectEvent.Id)
                                                                       .OrderBy(param => param.Id)
                                                                       .ToList();
                for (int i = 0; i < parameters.Count(); i++)
                {
                    if (parameters[i].ActivityParameterTypeId == (byte)SeedDataEnum.ActivityParameterTypes.Text && parameters[i].ShouldBeLocalized)
                        parameters[i].Body = _localizer[parameters[i].Body];
                }
                events.Add(new HistoryModel()
                {
                    Message = _localizer[projectEvent.Event],
                    Parameters = parameters.Select(param => new HistoryParameterModel() { ParameterBody = param.Body, ParameterType = param.ActivityParameterTypeId }).ToList(),
                    Time = projectEvent.EventTime
                });
            }

            response.Message = "History returned successfully!";
            response.Value = events;
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("delete")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == id);
            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.AddError(_localizer["There is no project with given Id."]);
                response.Message = "Projet not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToProject(userId, id))
            {
                response.AddError(_localizer["You are not a memeber of project."]);
                response.Message = "Project access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            //if (selectedProject.TeamId == null && userId != selectedProject.CreatedBy)
            //{
            //    response.AddError(_localizer["Deleting personal project just can be done by creator."]);
            //    response.Message = "Delete project access denied";
            //    return StatusCode(StatusCodes.Status403Forbidden, response);
            //}

            if (userId != selectedProject.CreatedBy)
            {
                response.AddError(_localizer["Projects can only get deleted by the creator."]);
                response.Message = "Delete project access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var modificationTime = DateTime.UtcNow;

            selectedProject.IsDeleted = true;
            selectedProject.ModifiedBy = userId;
            selectedProject.ModifiedOn = modificationTime;
            _vwsDbContext.Save();

            DeleteProjectTasks(selectedProject.Id, selectedProject.ModifiedOn);
            DeleteProjectEvents(selectedProject.Id, selectedProject.ModifiedOn);

            var projectTasks = _vwsDbContext.GeneralTasks.Where(task => task.ProjectId == selectedProject.Id && !task.IsDeleted);
            foreach (var projectTask in projectTasks)
                _taskManager.StopRunningTimes(projectTask.Id, selectedProject.ModifiedOn);

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = id,
                Event = "Project deleted by {0}.",
                EventTime = modificationTime,
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                })
            });
            _vwsDbContext.Save();

            var users = _projectManager.GetProjectUsers(selectedProject.Id).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> deleted project <b>«{1}»</b>.";
            string[] arguments = { LoggedInNickName, selectedProject.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments);

            _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

            response.Message = "Project deleted successfully!";
            return Ok(response);
        }
        #endregion

        #region ProjectMemberAPIS
        [HttpPost]
        [Authorize]
        [Route("addUsersToProject")]
        public async Task<IActionResult> AddUsersToProject([FromBody] AddUserToProjectModel model)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            model.Users = model.Users.Distinct().ToList();

            var selectedProject = _vwsDbContext.Projects.Include(project => project.ProjectDepartments)
                                                        .FirstOrDefault(project => project.Id == model.ProjectId);

            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.AddError(_localizer["There is no project with given Id."]);
                response.Message = "Projet not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedProject.TeamId != null)
            {
                response.AddError(_localizer["Adding user is just for personal projects."]);
                response.Message = "Non-Personal project";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToProject(userId, model.ProjectId))
            {
                response.AddError(_localizer["You are not a memeber of project."]);
                response.Message = "Project access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var projectUsers = _permissionService.GetUsersHaveAccessToProject(selectedProject.Id);
            model.Users = model.Users.Except(projectUsers).ToList();

            foreach (var modelUser in model.Users)
            {
                if (!_vwsDbContext.UserProfiles.Any(profile => profile.UserId == modelUser))
                {
                    response.AddError(_localizer["Invalid users."]);
                    response.Message = "Invalid users";
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }
            }

            var usersCanBeAddedToProject = GetAvailableUsersToAddProject(selectedProject.Id);
            if (usersCanBeAddedToProject.Intersect(model.Users).Count() != model.Users.Count)
            {
                response.AddError(_localizer["Invalid users."]);
                response.Message = "Invalid users";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var user = await _vwsDbContext.GetUserProfileAsync(userId);

            var addTime = DateTime.UtcNow;
            foreach (var modelUser in model.Users)
            {

                var addedUser = await _vwsDbContext.GetUserProfileAsync(modelUser);

                var selectedProjectMember = _vwsDbContext.ProjectMembers.FirstOrDefault(projectMember => projectMember.UserProfileId == modelUser &&
                                                                                                         projectMember.ProjectId == model.ProjectId &&
                                                                                                        !projectMember.IsDeleted);

                ProjectHistory newProjectHistory;
                List<Guid> users;
                string emailMessage;

                if (selectedProjectMember != null && selectedProjectMember.IsPermittedByCreator == null)
                {
                    if (selectedProject.CreatedBy != userId)
                        continue;

                    selectedProjectMember.IsPermittedByCreator = true;
                    selectedProjectMember.PermittedOn = DateTime.UtcNow;
                    _vwsDbContext.Save();

                    #region HistoryAndEmail
                    newProjectHistory = new ProjectHistory()
                    {
                        ProjectId = selectedProjectMember.ProjectId,
                        Event = "{0} gave permission to user {1}.",
                        EventTime = selectedProjectMember.PermittedOn
                    };
                    _vwsDbContext.AddProjectHistory(newProjectHistory);
                    _vwsDbContext.Save();

                    _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
                    {
                        ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                        ProjectHistoryId = newProjectHistory.Id,
                        Body = JsonConvert.SerializeObject(new UserModel()
                        {
                            NickName = user.NickName,
                            ProfileImageGuid = user.ProfileImageGuid,
                            UserId = user.UserId
                        })
                    });
                    _vwsDbContext.Save();
                    _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
                    {
                        ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                        ProjectHistoryId = newProjectHistory.Id,
                        Body = JsonConvert.SerializeObject(new UserModel()
                        {
                            NickName = addedUser.NickName,
                            ProfileImageGuid = addedUser.ProfileImageGuid,
                            UserId = addedUser.UserId
                        })
                    });
                    _vwsDbContext.Save();

                    string[] permissionArgs = { LoggedInNickName, selectedProject.Name };
                    await _notificationService.SendSingleEmail((int)EmailTemplateEnum.NotificationEmail, "<b>«{0}»</b> gave permission to you for project <b>«{1}»</b>.", "Project Access", addedUser.UserId, permissionArgs);

                    users = _projectManager.GetProjectUsers(selectedProjectMember.ProjectId).Select(user => user.UserId).ToList();
                    users = users.Distinct().ToList();
                    users.Remove(LoggedInUserId.Value);

                    _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

                    users.Remove(addedUser.UserId);
                    emailMessage = "<b>«{0}»</b> gave permission to user <b>«{1}»</b> in project <b>«{2}»</b>.";
                    string[] permissionArguments = { LoggedInNickName, addedUser.NickName, selectedProjectMember.Project.Name };
                    await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", permissionArguments);
                    #endregion

                    response.Message = "User added successfully!";
                    return Ok(response);
                }

                var newPorjectMember = new ProjectMember()
                {
                    CreatedOn = addTime,
                    IsDeleted = false,
                    ProjectId = model.ProjectId,
                    UserProfileId = modelUser,
                    IsPermittedByCreator = (userId == selectedProject.CreatedBy) ? (bool?)true : null,
                };
                if (userId == selectedProject.CreatedBy)
                    newPorjectMember.PermittedOn = newPorjectMember.CreatedOn;
                await _vwsDbContext.AddProjectMemberAsync(newPorjectMember);
                _vwsDbContext.AddUsersActivity(new UsersActivity() { Time = addTime, TargetUserId = modelUser, OwnerUserId = LoggedInUserId.Value });
                _vwsDbContext.Save();

                #region HistoryAndEmail
                if (newPorjectMember.IsPermittedByCreator == true)
                {
                    newProjectHistory = new ProjectHistory()
                    {
                        ProjectId = model.ProjectId,
                        Event = "{0} added {1} to the project.",
                        EventTime = newPorjectMember.CreatedOn
                    };
                    _vwsDbContext.AddProjectHistory(newProjectHistory);
                    _vwsDbContext.Save();

                    _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
                    {
                        ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                        ProjectHistoryId = newProjectHistory.Id,
                        Body = JsonConvert.SerializeObject(new UserModel()
                        {
                            NickName = user.NickName,
                            ProfileImageGuid = user.ProfileImageGuid,
                            UserId = user.UserId
                        })
                    });
                    _vwsDbContext.Save();
                    _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
                    {
                        ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                        ProjectHistoryId = newProjectHistory.Id,
                        Body = JsonConvert.SerializeObject(new UserModel()
                        {
                            NickName = addedUser.NickName,
                            ProfileImageGuid = addedUser.ProfileImageGuid,
                            UserId = addedUser.UserId
                        })
                    });
                    _vwsDbContext.Save();

                    string[] args = { LoggedInNickName, selectedProject.Name };
                    await _notificationService.SendSingleEmail((int)EmailTemplateEnum.NotificationEmail, "<b>«{0}»</b> added you to project <b>«{1}»</b>.", "Project Access", addedUser.UserId, args);

                    users = _projectManager.GetProjectUsers(selectedProject.Id).Select(user => user.UserId).ToList();
                    users = users.Distinct().ToList();
                    users.Remove(LoggedInUserId.Value);

                    _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

                    users.Remove(userId);
                    emailMessage = "<b>«{0}»</b> added <b>«{1}»</b> to project <b>«{2}»</b>.";
                    string[] arguments = { LoggedInNickName, addedUser.NickName, selectedProject.Name };
                    await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments);
                }
                else
                {
                    newProjectHistory = new ProjectHistory()
                    {
                        ProjectId = model.ProjectId,
                        Event = "{0} requested to add {1} to the project.",
                        EventTime = newPorjectMember.CreatedOn
                    };
                    _vwsDbContext.AddProjectHistory(newProjectHistory);
                    _vwsDbContext.Save();

                    _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
                    {
                        ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                        ProjectHistoryId = newProjectHistory.Id,
                        Body = JsonConvert.SerializeObject(new UserModel()
                        {
                            NickName = user.NickName,
                            ProfileImageGuid = user.ProfileImageGuid,
                            UserId = user.UserId
                        })
                    });
                    _vwsDbContext.Save();
                    _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
                    {
                        ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                        ProjectHistoryId = newProjectHistory.Id,
                        Body = JsonConvert.SerializeObject(new UserModel()
                        {
                            NickName = addedUser.NickName,
                            ProfileImageGuid = addedUser.ProfileImageGuid,
                            UserId = addedUser.UserId
                        })
                    });
                    _vwsDbContext.Save();

                    string[] args = { LoggedInNickName, selectedProject.Name };
                    await _notificationService.SendSingleEmail((int)EmailTemplateEnum.NotificationEmail, "<b>«{0}»</b> requested to project creator to added you to project <b>«{1}»</b>.", "Project Access", addedUser.UserId, args);

                    users = _projectManager.GetProjectUsers(selectedProject.Id).Select(user => user.UserId).ToList();
                    users = users.Distinct().ToList();
                    users.Remove(LoggedInUserId.Value);
                    users.Remove(modelUser);
                    emailMessage = "<b>«{0}»</b> requested to project creator <b>«{1}»</b> to project <b>«{2}»</b>.";
                    string[] arguments = { LoggedInNickName, addedUser.NickName, selectedProject.Name };
                    await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments);

                    users.Add(modelUser);
                    _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);
                }
                #endregion
            }

            response.Message = "User added successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("permitUserAccess")]
        public async Task<IActionResult> PermitUserAccess(int projectMemberId)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            var selectedProjectMember = _vwsDbContext.ProjectMembers.Include(projectMember => projectMember.Project).FirstOrDefault(projectMember => projectMember.Id == projectMemberId);

            if (selectedProjectMember == null || selectedProjectMember.IsPermittedByCreator == false || selectedProjectMember.IsDeleted)
            {
                response.AddError(_localizer["There is not project member with such id."]);
                response.Message = "Project member not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedProjectMember.Project.CreatedBy != userId)
            {
                response.Message = "Not creator";
                response.AddError(_localizer["You are not project creator."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedProjectMember.Project.CreatedBy == userId)
            {
                response.Message = "Creator can not change his permission.";
                response.AddError(_localizer["You are the project creator and can not change your access permission."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var lastPermission = selectedProjectMember.IsPermittedByCreator;

            if (lastPermission == true)
            {
                response.Message = "User already is permitted.";
                response.AddError(_localizer["User already is permitted."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedProjectMember.IsPermittedByCreator = true;
            selectedProjectMember.PermittedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProjectMember.ProjectId,
                Event = "{0} gave permission to user {1}.",
                EventTime = selectedProjectMember.PermittedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            var permittedUser = await _vwsDbContext.GetUserProfileAsync(selectedProjectMember.UserProfileId);
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                })
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = permittedUser.NickName,
                    ProfileImageGuid = permittedUser.ProfileImageGuid,
                    UserId = permittedUser.UserId
                })
            });
            _vwsDbContext.Save();

            string[] args = { LoggedInNickName, selectedProjectMember.Project.Name };
            await _notificationService.SendSingleEmail((int)EmailTemplateEnum.NotificationEmail, "<b>«{0}»</b> gave permission to you for project <b>«{1}»</b>.", "Project Access", permittedUser.UserId, args);

            var users = _projectManager.GetProjectUsers(selectedProjectMember.ProjectId).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);

            _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

            users.Remove(permittedUser.UserId);
            string emailMessage = "<b>«{0}»</b> gave permission to user <b>«{1}»</b> in project <b>«{2}»</b>.";
            string[] arguments = { LoggedInNickName, permittedUser.NickName, selectedProjectMember.Project.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments);

            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("prohibitUserAccess")]
        public async Task<IActionResult> ProhibitUserAccess(int projectMemberId)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            var selectedProjectMember = _vwsDbContext.ProjectMembers.Include(projectMember => projectMember.Project).FirstOrDefault(projectMember => projectMember.Id == projectMemberId);

            if (selectedProjectMember == null || selectedProjectMember.IsPermittedByCreator == false || selectedProjectMember.IsDeleted)
            {
                response.AddError(_localizer["There is not project member with such id."]);
                response.Message = "Project member not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedProjectMember.Project.CreatedBy != userId)
            {
                response.Message = "Not creator";
                response.AddError(_localizer["You are not project creator."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedProjectMember.Project.CreatedBy == userId)
            {
                response.Message = "Creator can not change his permission.";
                response.AddError(_localizer["You are the project creator and can not change your access permission."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var lastPermission = selectedProjectMember.IsPermittedByCreator;

            if (lastPermission == true)
            {
                response.Message = "User already is permitted.";
                response.AddError(_localizer["User already is permitted."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedProjectMember.IsPermittedByCreator = false;
            selectedProjectMember.IsDeleted = true;
            selectedProjectMember.DeletedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProjectMember.ProjectId,
                Event = "{0} did not accepted user {1} to have access to project.",
                EventTime = (DateTime)selectedProjectMember.DeletedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            var prohibittedUser = await _vwsDbContext.GetUserProfileAsync(selectedProjectMember.UserProfileId);
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                })
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = prohibittedUser.NickName,
                    ProfileImageGuid = prohibittedUser.ProfileImageGuid,
                    UserId = prohibittedUser.UserId
                })
            });
            _vwsDbContext.Save();

            string[] args = { LoggedInNickName, selectedProjectMember.Project.Name };
            await _notificationService.SendSingleEmail((int)EmailTemplateEnum.NotificationEmail, "<b>«{0}»</b> did not give permission to you for project <b>«{1}»</b>.", "Project Access", prohibittedUser.UserId, args);

            var users = _projectManager.GetProjectUsers(selectedProjectMember.ProjectId).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            users.Remove(prohibittedUser.UserId);
            string emailMessage = "<b>«{0}»</b> did not give permission to user <b>«{1}»</b> in project <b>«{2}»</b>.";
            string[] arguments = { LoggedInNickName, prohibittedUser.NickName, selectedProjectMember.Project.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments);

            users.Add(prohibittedUser.UserId);
            _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getUsersCanBeAddedToProject")]
        public async Task<IActionResult> GetUsersCanBeAddedToProject(int? Id)
        {
            var response = new ResponseModel<List<UserModel>>();
            var users = new List<UserModel>();

            var userId = LoggedInUserId.Value;

            if (Id != null)
            {
                var selectedProject = _vwsDbContext.Projects.Include(project => project.ProjectDepartments)
                                                           .FirstOrDefault(project => project.Id == Id);

                if (selectedProject == null || selectedProject.IsDeleted)
                {
                    response.AddError(_localizer["There is no project with given Id."]);
                    response.Message = "Projet not found";
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }

                if (selectedProject.TeamId != null)
                {
                    response.AddError(_localizer["Adding user is just for personal projects."]);
                    response.Message = "Non-Personal project";
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }

                if (!_permissionService.HasAccessToProject(userId, (int)Id))
                {
                    response.AddError(_localizer["You are not a memeber of project."]);
                    response.Message = "Project access denied";
                    return StatusCode(StatusCodes.Status403Forbidden, response);
                }
            }

            var availableUsers = GetAvailableUsersToAddProject(Id);
            var usersOrders = _vwsDbContext.UsersOrders.Where(userOrder => userOrder.OwnerUserId == LoggedInUserId.Value).ToList();

            var validUsersFromUsersOrders = usersOrders.Where(usersOrder => availableUsers.Contains(usersOrder.TargetUserId))
                                                       .OrderBy(usersOrder => usersOrder.Order)
                                                       .Select(usersOrder => usersOrder.TargetUserId)
                                                       .ToList();

            var usersNotIncluded = validUsersFromUsersOrders.Count == availableUsers.Count ? new List<Guid>() : availableUsers.Except(validUsersFromUsersOrders);

            foreach (var availableUserId in validUsersFromUsersOrders)
            {
                var user = await _userManager.FindByIdAsync(availableUserId.ToString());
                var userProfile = await _vwsDbContext.GetUserProfileAsync(availableUserId);
                users.Add(new UserModel()
                {
                    UserId = availableUserId,
                    ProfileImageGuid = userProfile.ProfileImageGuid,
                    NickName = userProfile.NickName
                });
            }
            foreach (var availableUserId in usersNotIncluded)
            {
                var user = await _userManager.FindByIdAsync(availableUserId.ToString());
                var userProfile = await _vwsDbContext.GetUserProfileAsync(availableUserId);
                users.Add(new UserModel()
                {
                    UserId = availableUserId,
                    ProfileImageGuid = userProfile.ProfileImageGuid,
                    NickName = userProfile.NickName
                });
            }

            response.Value = users;
            response.Message = "Users returned successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getProjectMembers")]
        public IActionResult GetProjectMembers(int projectId)
        {
            var response = new ResponseModel<List<UserModel>>();
            var members = new List<UserModel>();

            var userId = LoggedInUserId.Value;

            var userProjects = _projectManager.GetAllUserProjects(LoggedInUserId.Value);

            if (!userProjects.Any(project => project.Id == projectId))
            {
                response.AddError(_localizer["There is no project with given Id."]);
                response.Message = "Project not found";
                return BadRequest(response);
            }

            List<UserProfile> users = new List<UserProfile>();

            var selectedProject = _vwsDbContext.Projects.Include(project => project.ProjectDepartments).FirstOrDefault(project => project.Id == projectId);
            if (selectedProject.TeamId == null)
            {
                users = _vwsDbContext.ProjectMembers.Include(projectMember => projectMember.UserProfile)
                                                   .Where(projectMember => projectMember.ProjectId == selectedProject.Id && !projectMember.IsDeleted)
                                                   .Select(projectMember => projectMember.UserProfile).ToList();
            }
            else if (selectedProject.ProjectDepartments.Count == 0)
            {
                users = _vwsDbContext.TeamMembers.Include(teamMember => teamMember.UserProfile)
                                                .Where(teamMember => teamMember.TeamId == selectedProject.TeamId && !teamMember.IsDeleted)
                                                .Select(teamMember => teamMember.UserProfile).ToList();
            }
            else
            {
                foreach (var projectDepartment in selectedProject.ProjectDepartments)
                {
                    var selectedDepartment = _vwsDbContext.Departments.FirstOrDefault(department => department.Id == projectDepartment.DepartmentId);
                    users.AddRange(_vwsDbContext.DepartmentMembers.Include(departmentMember => departmentMember.UserProfile)
                                                                 .Where(departmentMember => departmentMember.DepartmentId == selectedDepartment.Id && !departmentMember.IsDeleted)
                                                                 .Select(departmentMember => departmentMember.UserProfile));
                }
            }

            foreach (var user in users)
            {
                members.Add(new UserModel()
                {
                    UserId = user.UserId,
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid
                });
            }

            response.Message = "Members returned successfully!";
            response.Value = members;

            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getProjectUserRequests")]
        public IActionResult GetProjectUserRequests(int id)
        {
            var response = new ResponseModel<List<Object>>();
            var users = new List<Object>();

            var userId = LoggedInUserId.Value;

            var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == id);

            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.Message = "Project not found";
                response.AddError(_localizer["There is no project with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToProject(userId, id))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You are not a memeber of project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedProject.TeamId != null)
            {
                response.Message = "Non-personal project";
                response.AddError(_localizer["Project is not personal."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (userId != selectedProject.CreatedBy)
            {
                response.Message = "Not creator";
                response.AddError(_localizer["You are not project creator."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var requestedUsers = _vwsDbContext.ProjectMembers.Include(projectMember => projectMember.UserProfile)
                                                             .Where(projectMember => projectMember.ProjectId == id &&
                                                                                    !projectMember.IsDeleted &&
                                                                                    projectMember.IsPermittedByCreator == null);

            foreach (var user in requestedUsers)
            {
                users.Add(new
                {
                    Id = user.Id,
                    UserInfo = new UserModel()
                    {
                        ProfileImageGuid = user.UserProfile.ProfileImageGuid,
                        UserId = user.UserProfileId,
                        NickName = user.UserProfile.NickName
                    }
                });
            }

            response.Value = users;
            response.Message = "Users returned successfully!";

            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("removeUserFromProject")]
        public async Task<IActionResult> RemoveUserFromProject(int id, Guid userId)
        {
            var response = new ResponseModel();

            var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == id);
            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.Message = "Project not found";
                response.AddError(_localizer["There is no project with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToProject(LoggedInUserId.Value, id))
            {
                response.Message = "Project access denied";
                response.AddError(_localizer["You are not a memeber of project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            
            if (selectedProject.TeamId != null)
            {
                response.AddError(_localizer["Deleting user is just for personal projects."]);
                response.Message = "Non-Personal project";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedProjectMember = _vwsDbContext.ProjectMembers.FirstOrDefault(projectMember => projectMember.ProjectId == id && projectMember.UserProfileId == userId && !projectMember.IsDeleted && projectMember.IsPermittedByCreator != false);
            if (selectedProjectMember == null)
            {
                response.AddError(_localizer["There is no member with such id in given project."]);
                response.Message = "Project member not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedProjectMember.IsDeleted = true;
            selectedProjectMember.DeletedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "{0} removed from project by {1}.",
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            var removedUser = await _vwsDbContext.GetUserProfileAsync(userId);
            var user = await _vwsDbContext.GetUserProfileAsync(LoggedInUserId.Value);
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                })
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddProjectHistoryParameter(new ProjectHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                ProjectHistoryId = newProjectHistory.Id,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = removedUser.NickName,
                    ProfileImageGuid = removedUser.ProfileImageGuid,
                    UserId = removedUser.UserId
                })
            });
            _vwsDbContext.Save();

            string[] args = { LoggedInNickName, selectedProjectMember.Project.Name };
            await _notificationService.SendSingleEmail((int)EmailTemplateEnum.NotificationEmail, "<b>«{0}»</b> removed you from project <b>«{1}»</b>.", "Project Access", removedUser.UserId, args);

            var users = _projectManager.GetProjectUsers(selectedProject.Id).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            users.Remove(userId);
            string emailMessage = "<b>«{0}»</b> removed <b>«{1}»</b> from project <b>«{2}»</b>.";
            string[] arguments = { LoggedInNickName, removedUser.NickName, selectedProject.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Project Update", arguments);

            users.Add(userId);
            _notificationService.SendMultipleNotification(users, (byte)SeedDataEnum.NotificationTypes.Project, newProjectHistory.Id);

            response.Message = "User deleted successfully!";
            return Ok(response);
        }
        #endregion
    }
}
