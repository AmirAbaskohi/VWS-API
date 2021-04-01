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
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._file;
using vws.web.Domain._project;
using vws.web.Domain._team;
using vws.web.Enums;
using vws.web.Models;
using vws.web.Models._project;
using vws.web.Repositories;
using vws.web.Services;
using vws.web.Services._chat;

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
        #endregion

        #region Ctor
        public ProjectController(UserManager<ApplicationUser> userManager, IStringLocalizer<ProjectController> localizer,
            IVWS_DbContext vwsDbContext, IFileManager fileManager, IPermissionService permissionService)
        {
            _userManager = userManager;
            _localizer = localizer;
            _vwsDbContext = vwsDbContext;
            _fileManager = fileManager;
            _permissionService = permissionService;
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
                                                                                      !projectMember.IsDeleted)
                                                          .Select(projectMember => projectMember.UserProfileId).ToList();

            List<Team> userTeams = _vwsDbContext.GetUserTeams(LoggedInUserId.Value).ToList();
            List<Guid> userTeamMates = _vwsDbContext.TeamMembers
                .Where(teamMember => userTeams.Select(userTeam => userTeam.Id).Contains(teamMember.TeamId) && !teamMember.IsDeleted)
                .Select(teamMember => teamMember.UserProfileId).Distinct().Where(id => id != LoggedInUserId.Value).ToList();

            availableUsers = userTeamMates.Except(projectUsers).ToList();

            return availableUsers;
        }

        private List<UserModel> GetProjectUsers(int projectId)
        {
            var selectedProject = _vwsDbContext.Projects.Include(project => project.ProjectDepartments)
                                                       .FirstOrDefault(project => project.Id == projectId);

            List<UserProfile> projectUsers = new List<UserProfile>();

            if (selectedProject.TeamId != null)
            {
                if (selectedProject.ProjectDepartments.Count == 0)
                {
                    projectUsers = _vwsDbContext.TeamMembers.Include(teamMember => teamMember.UserProfile)
                                                           .Where(teamMember => teamMember.TeamId == (int)selectedProject.TeamId &&
                                                                                                     !teamMember.IsDeleted)
                                                           .Select(teamMember => teamMember.UserProfile)
                                                           .ToList();
                }

                else
                {
                    foreach (var departmentId in selectedProject.ProjectDepartments.Select(pd => pd.DepartmentId))
                    {
                        projectUsers.AddRange(_vwsDbContext.DepartmentMembers.Include(departmentMember => departmentMember.UserProfile)
                                                                            .Where(departmentMember => departmentMember.DepartmentId == departmentId &&
                                                                                                       !departmentMember.IsDeleted)
                                                                            .Select(departmentMember => departmentMember.UserProfile)
                                                                            .ToList());
                    }
                }
            }

            else
            {
                projectUsers = _vwsDbContext.ProjectMembers.Include(projectMember => projectMember.UserProfile)
                                                          .Where(projectMember => !projectMember.IsDeleted &&
                                                                                  projectMember.IsPermittedByCreator == true &&
                                                                                  projectMember.ProjectId == projectId)
                                                          .Select(projectMember => projectMember.UserProfile)
                                                          .ToList();
            }

            List<UserModel> users = new List<UserModel>();

            foreach (var user in projectUsers)
            {
                users.Add(new UserModel()
                {
                    UserId = user.UserId,
                    ProfileImageGuid = user.ProfileImageGuid,
                    NickName = user.NickName
                });
            }

            return users.Distinct().ToList();
        }

        private List<Project> GetAllUserProjects(Guid userId)
        {
            List<Project> userProjects = new List<Project>();

            List<int> userTeams = _vwsDbContext.GetUserTeams(userId).Select(team => team.Id).ToList();
            List<int> userDepartments = _vwsDbContext.DepartmentMembers.Include(departmentMember => departmentMember.Department)
                                                                      .Where(departmentMember => !departmentMember.IsDeleted &&
                                                                                                 departmentMember.UserProfileId == userId &&
                                                                                                 !departmentMember.Department.IsDeleted)
                                                                      .Select(departmentMember => departmentMember.DepartmentId)
                                                                      .ToList();

            userProjects.AddRange(_vwsDbContext.Projects.Include(project => project.ProjectDepartments)
                                                       .Where(project => project.IsDeleted == false &&
                                                                         project.TeamId != null &&
                                                                         project.ProjectDepartments.Count == 0 &&
                                                                         userTeams.Contains((int)project.TeamId))
                                                       .ToList());

            foreach (var project in _vwsDbContext.Projects.Include(project => project.ProjectDepartments))
            {
                if (project.IsDeleted == false && project.TeamId != null &&
                   project.ProjectDepartments.Count != 0 &&
                   userTeams.Contains((int)project.TeamId) &&
                   project.ProjectDepartments.Select(pd => pd.DepartmentId).Intersect(userDepartments).Any())
                {
                    userProjects.Add(project);
                }
            }

            userProjects.AddRange(_vwsDbContext.ProjectMembers.Include(projectMember => projectMember.Project)
                                                             .Where(projectMember => projectMember.UserProfileId == userId &&
                                                                                     !projectMember.IsDeleted &&
                                                                                     projectMember.IsPermittedByCreator == true &&
                                                                                     !projectMember.Project.IsDeleted)
                                                             .Select(projectMember => projectMember.Project));

            return userProjects;
        }

        private async Task AddProjectUsers(int projectId, List<Guid> users)
        {
            var creationTime = DateTime.Now;
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
            }
            _vwsDbContext.Save();
        }

        private long GetNumberOfProjectTasks(int id)
        {
            return _vwsDbContext.GeneralTasks.Where(task => task.ProjectId == id && !task.IsDeleted).Count();
        }

        private void CreateProjectTaskStatuses(int projectId)
        {
            _vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 2, ProjectId = projectId, UserProfileId = null, TeamId = null, Title = "To Do" });
            _vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 4, ProjectId = projectId, UserProfileId = null, TeamId = null, Title = "Doing" });
            _vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 6, ProjectId = projectId, UserProfileId = null, TeamId = null, Title = "Done"});

            _vwsDbContext.Save();
        }
        #endregion

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

            model.Users = model.Users.Distinct().ToList();
            model.Users.Remove(userId);

            DateTime creationTime = DateTime.Now;

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
                CreateBy = userId,
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
                Event = "Project created by {0}.",
                CommaSepratedParameters = (await _vwsDbContext.GetUserProfileAsync(userId)).NickName,
                EventTime = creationTime
            };

            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            foreach (var departmentId in model.DepartmentIds)
                _vwsDbContext.AddProjectDepartment(new ProjectDepartment { DepartmentId = departmentId, ProjectId = newProject.Id });

            if (newProject.TeamId == null)
            {
                model.Users.Add(LoggedInUserId.Value);
                await AddProjectUsers(newProject.Id, model.Users);
            }

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
                Users = GetProjectUsers(newProject.Id),
                NumberOfTasks = GetNumberOfProjectTasks(newProject.Id)
            };

            response.Value = newProjectResponse;
            response.Message = "Project created successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateName")]
        public IActionResult UpdateName(int id, string newName)
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

            if (String.IsNullOrEmpty(newName))
                response.AddError(_localizer["Name can not be empty."]);

            if (!String.IsNullOrEmpty(newName) && newName.Length > 500)
                response.AddError(_localizer["Length of title is more than 500 characters."]);

            if (response.HasError)
            {
                response.Message = "Invalid model.";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedProject.Name = newName;
            selectedProject.ModifiedOn = DateTime.Now;
            selectedProject.ModifiedBy = userId;
            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "Project name updated to {0} by {1}.",
                CommaSepratedParameters = selectedProject.Name + "," + LoggedInNickName,
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            response.Message = "Name updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateDescription")]
        public IActionResult UpdateDescription(int id, string newDescription)
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

            if (!String.IsNullOrEmpty(newDescription) && newDescription.Length > 2000)
            {
                response.AddError(_localizer["Length of description is more than 2000 characters."]);
                response.Message = "Invalid model.";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedProject.Description = newDescription;
            selectedProject.ModifiedOn = DateTime.Now;
            selectedProject.ModifiedBy = userId;
            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "Project description updated by {0}.",
                CommaSepratedParameters = LoggedInNickName,
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            response.Message = "Description updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateColor")]
        public IActionResult UpdateColor(int id, string newColor)
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

            if (!String.IsNullOrEmpty(newColor) && newColor.Length > 6)
            {
                response.AddError(_localizer["Length of color is more than 6 characters."]);
                response.Message = "Invalid model.";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedProject.Color = newColor;
            selectedProject.ModifiedOn = DateTime.Now;
            selectedProject.ModifiedBy = userId;
            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "Project color updated by {0}.",
                CommaSepratedParameters = LoggedInNickName,
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            response.Message = "Color updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateStartDate")]
        public IActionResult UpdateStartDate(int id, DateTime? newStartDate)
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

            selectedProject.StartDate = newStartDate;
            selectedProject.ModifiedOn = DateTime.Now;
            selectedProject.ModifiedBy = userId;
            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "Project start date updated by {0}.",
                CommaSepratedParameters = LoggedInNickName,
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            response.Message = "Start date updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateEndDate")]
        public IActionResult UpdateEndDate(int id, DateTime? newEndDate)
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

            selectedProject.EndDate = newEndDate;
            selectedProject.ModifiedOn = DateTime.Now;
            selectedProject.ModifiedBy = userId;
            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "Project end date updated by {0}.",
                CommaSepratedParameters = LoggedInNickName,
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            response.Message = "End date updated successfully!";
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

            var selectedProjectMember = _vwsDbContext.ProjectMembers.FirstOrDefault(projectMember => projectMember.ProjectId == id && projectMember.UserProfileId == userId);
            if (selectedProjectMember == null || selectedProject.IsDeleted)
            {
                response.AddError(_localizer["There is no member with such id in given project."]);
                response.Message = "Project member not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            _vwsDbContext.DeleteProjectMember(selectedProjectMember);
            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "{0} removed from project by {1}.",
                CommaSepratedParameters = (await _vwsDbContext.GetUserProfileAsync(userId)).NickName + "," + LoggedInNickName,
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            response.Message = "User deleted successfully!";
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
            selectedProject.ModifiedOn = DateTime.Now;
            selectedProject.ModifiedBy = userId;
            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = selectedProject.Id,
                Event = "Project team and departments updated by {0}.",
                CommaSepratedParameters = LoggedInNickName,
                EventTime = selectedProject.ModifiedOn
            };
            _vwsDbContext.AddProjectHistory(newProjectHistory);
            _vwsDbContext.Save();

            response.Message = "Project team and departments updated successfully!";
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

            if (selectedProject.TeamId == null && userId != selectedProject.CreateBy)
            {
                response.AddError(_localizer["Deleting personal project just can be done by creator."]);
                response.Message = "Delete project access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var modificationTime = DateTime.Now;

            selectedProject.IsDeleted = true;
            selectedProject.ModifiedBy = userId;
            selectedProject.ModifiedOn = modificationTime;

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = id,
                Event = "Project deleted by {0}.",
                EventTime = modificationTime,
                CommaSepratedParameters = (await _vwsDbContext.GetUserProfileAsync(userId)).NickName
            };

            _vwsDbContext.AddProjectHistory(newProjectHistory);

            _vwsDbContext.Save();

            response.Message = "Project deleted successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public ProjectResponseModel Get(int Id)
        {
            Guid userId = LoggedInUserId.Value;

            var project = GetAllUserProjects(userId).FirstOrDefault(project => project.Id == Id);
            if (project == null)
                return new ProjectResponseModel();

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
                Users = GetProjectUsers(project.Id),
                NumberOfTasks = GetNumberOfProjectTasks(project.Id)
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

            var userProjects = GetAllUserProjects(userId).Where(project => project.StatusId == (byte)SeedDataEnum.ProjectStatuses.Active);

            foreach (var project in userProjects)
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
                    Users = GetProjectUsers(project.Id),
                    NumberOfTasks = GetNumberOfProjectTasks(project.Id)
                });
            }

            return response;
        }

        [HttpGet]
        [Authorize]
        [Route("getHold")]
        public IEnumerable<ProjectResponseModel> GetHoldProject()
        {
            var response = new List<ProjectResponseModel>();
            Guid userId = LoggedInUserId.Value;

            var userProjects = GetAllUserProjects(userId).Where(project => project.StatusId == (byte)SeedDataEnum.ProjectStatuses.Hold);

            foreach (var project in userProjects)
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
                    Users = GetProjectUsers(project.Id),
                    NumberOfTasks = GetNumberOfProjectTasks(project.Id)
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

            var userProjects = GetAllUserProjects(userId).Where(project => project.StatusId == (byte)SeedDataEnum.ProjectStatuses.DoneOrArchived);

            foreach (var project in userProjects)
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
                    Users = GetProjectUsers(project.Id),
                    NumberOfTasks = GetNumberOfProjectTasks(project.Id)
                });
            }

            return response;
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

            foreach (var availableUserId in availableUsers)
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

            var userProjects = GetAllUserProjects(LoggedInUserId.Value);

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

        [HttpPost]
        [Authorize]
        [Route("addUserToProject")]
        public async Task<IActionResult> AddUserToProject([FromBody] AddUserToProjectModel model)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

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

            var selectedProjectMember = _vwsDbContext.ProjectMembers.FirstOrDefault(projectMember => projectMember.UserProfileId == model.UserId &&
                                                                                         projectMember.ProjectId == model.ProjectId &&
                                                                                         !projectMember.IsDeleted);

            if (selectedProjectMember != null && selectedProjectMember.IsPermittedByCreator == true)
            {
                response.AddError(_localizer["User you want to add, is already a member of selected project."]);
                response.Message = "Already member of project";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            else if (selectedProjectMember != null && selectedProjectMember.IsPermittedByCreator == null)
            {
                if (selectedProject.CreateBy != userId)
                {
                    response.AddError(_localizer["User you want to add, is already a member of selected project."]);
                    response.Message = "Already member of project";
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }

                selectedProjectMember.IsPermittedByCreator = true;
                selectedProjectMember.PermittedOn = DateTime.Now;

                _vwsDbContext.Save();

                response.Message = "User added successfully!";
                return Ok(response);
            }

            var availableUsers = GetAvailableUsersToAddProject(selectedProject.Id);
            if (!availableUsers.Contains(model.UserId))
            {
                response.AddError(_localizer["This user can not be added to project."]);
                response.Message = "Can not be added project";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var newPorjectMember = new ProjectMember()
            {
                CreatedOn = DateTime.Now,
                IsDeleted = false,
                ProjectId = model.ProjectId,
                UserProfileId = model.UserId,
                IsPermittedByCreator = (userId == selectedProject.CreateBy) ? (bool?)true : null
            };

            await _vwsDbContext.AddProjectMemberAsync(newPorjectMember);

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = model.ProjectId,
                Event = "{0} added {1} to the project.",
                EventTime = newPorjectMember.CreatedOn,
                CommaSepratedParameters = (await _vwsDbContext.GetUserProfileAsync(userId)).NickName + "," + (await _vwsDbContext.GetUserProfileAsync(model.UserId)).NickName
            };

            _vwsDbContext.AddProjectHistory(newProjectHistory);

            _vwsDbContext.Save();

            response.Message = "User added successfully!";
            return Ok(response);
        }

        [HttpPost]
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

            if (isProjectPersonal && userId != selectedProject.CreateBy)
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
                var time = DateTime.Now;
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
            selectedProject.ModifiedOn = DateTime.Now;

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = projectId,
                Event = "Added new project image for project by {0}.",
                EventTime = selectedProject.ModifiedOn,
                CommaSepratedParameters = (await _vwsDbContext.GetUserProfileAsync(userId)).NickName
            };

            _vwsDbContext.AddProjectHistory(newProjectHistory);

            _vwsDbContext.Save();

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

            if (isProjectPersonal && userId != selectedProject.CreateBy)
            {
                response.AddError(_localizer["Updating personal project just can be done by creator."]);
                response.Message = "Update project access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var lastStatus = selectedProject.StatusId;

            selectedProject.StatusId = statusId;
            selectedProject.ModifiedOn = DateTime.Now;
            selectedProject.ModifiedBy = userId;

            var newProjectHistory = new ProjectHistory()
            {
                ProjectId = id,
                Event = "Project status changed from {0} to {1} by {2}.",
                EventTime = selectedProject.ModifiedOn,
                CommaSepratedParameters = (SeedDataEnum.ProjectStatuses)lastStatus +
                                          "," + (SeedDataEnum.ProjectStatuses)selectedProject.StatusId +
                                          "," + (await _vwsDbContext.GetUserProfileAsync(userId)).NickName
            };

            _vwsDbContext.AddProjectHistory(newProjectHistory);

            _vwsDbContext.Save();

            response.Message = "Project status updated successfully!";
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

            if (userId != selectedProject.CreateBy)
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

        [HttpPut]
        [Authorize]
        [Route("changeProjectMemberPermisssion")]
        public IActionResult ChangeProjectMemberPermisssion(int projectMemberId, bool hasAccess)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            var selectedProjecMember = _vwsDbContext.ProjectMembers.Include(projectMember => projectMember.Project).FirstOrDefault(projectMember => projectMember.Id == projectMemberId);

            if (selectedProjecMember == null || selectedProjecMember.IsDeleted)
            {
                response.AddError(_localizer["There is not project member with such id."]);
                response.Message = "Project member not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedProjecMember.Project.CreateBy != userId)
            {
                response.Message = "Not creator";
                response.AddError(_localizer["You are not project creator."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedProjecMember.Project.CreateBy == userId)
            {
                response.Message = "Creator can not change his permission.";
                response.AddError(_localizer["You are the project creator and can not change your access permission."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedProjecMember.IsPermittedByCreator = hasAccess;
            _vwsDbContext.Save();

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

            if (selectedProject.TeamId == null && selectedProject.CreateBy != userId)
            {
                response.Message = "Not creator";
                response.AddError(_localizer["You are not project creator."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var events = new List<HistoryModel>();
            var projectEvents = _vwsDbContext.ProjectHistories.Where(projectHistory => projectHistory.ProjectId == id);
            foreach (var projectEvent in projectEvents)
            {
                var parameters = projectEvent.CommaSepratedParameters.Split(',');
                events.Add(new HistoryModel()
                {
                    Message = String.Format(_localizer[projectEvent.Event], parameters),
                    Time = projectEvent.EventTime
                });
            }

            response.Message = "History returned successfully!";
            response.Value = events;
            return Ok(response);
        }
    }
}
