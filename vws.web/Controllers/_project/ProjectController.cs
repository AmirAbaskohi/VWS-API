using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._project;
using vws.web.Enums;
using vws.web.Models;
using vws.web.Models._project;

namespace vws.web.Controllers._project
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class ProjectController : BaseController
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IConfiguration configuration;
        private readonly IStringLocalizer<ProjectController> localizer;
        private readonly IVWS_DbContext vwsDbContext;

        public ProjectController(UserManager<ApplicationUser> _userManager, RoleManager<IdentityRole> _roleManager,
            SignInManager<ApplicationUser> _signInManager, IStringLocalizer<ProjectController> _localizer,
            IVWS_DbContext _vwsDbContext, IConfiguration _configuration)
        {
            userManager = _userManager;
            roleManager = _roleManager;
            signInManager = _signInManager;
            configuration = _configuration;
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
        }

        [HttpPost]
        [Authorize]
        [Route("create")]
        public async Task<IActionResult> CreateProject([FromBody] ProjectModel model)
        {
            var response = new ResponseModel<ProjectResponseModel>();

            if (!String.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
            {
                response.Message = "Project model data has problem.";
                response.AddError(localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Name.Length > 500)
            {
                response.Message = "Project model data has problem.";
                response.AddError(localizer["Length of title is more than 500 characters."]);
            }
            if (!String.IsNullOrEmpty(model.Color) && model.Color.Length > 6)
            {
                response.Message = "Project model data has problem.";
                response.AddError(localizer["Length of color is more than 6 characters."]);
            }
            if (model.StartDate.HasValue && model.EndDate.HasValue)
            {
                if (model.StartDate > model.EndDate)
                {
                    response.Message = "Project model data has problem.";
                    response.AddError(localizer["Start Date should be before End Date."]);
                }
            }
            if (response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            Guid userId = LoggedInUserId.Value;

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
                IsDeleted = false
            };

            await vwsDbContext.AddProjectAsync(newProject);
            vwsDbContext.Save();

            var newProjectMember = new ProjectMember()
            {
                CreatedOn = creationTime,
                ProjectId = newProject.Id,
                UserProfileId = userId,
                IsDeleted = false
            };

            await vwsDbContext.AddProjectMemberAsync(newProjectMember);
            vwsDbContext.Save();

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
                IsDelete = newProject.IsDeleted
            };

            response.Value = newProjectResponse;
            response.Message = "Project created successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("update")]
        public IActionResult UpdateProject([FromBody] UpdateProjectModel model)
        {
            var response = new ResponseModel<ProjectResponseModel>();

            if (!String.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
            {
                response.Message = "Project model data has problem.";
                response.AddError(localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Name.Length > 500)
            {
                response.Message = "Project model data has problem.";
                response.AddError(localizer["Length of title is more than 500 characters."]);
            }
            if (!String.IsNullOrEmpty(model.Color) && model.Color.Length > 6)
            {
                response.Message = "Project model data has problem.";
                response.AddError(localizer["Length of color is more than 6 characters."]);
            }
            if (model.StartDate.HasValue && model.EndDate.HasValue)
            {
                if (model.StartDate > model.EndDate)
                {
                    response.Message = "Project model data has problem.";
                    response.AddError(localizer["Start Date should be before End Date."]);
                }
            }
            if (response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            Guid userId = LoggedInUserId.Value;

            var selectedProject = vwsDbContext.Projects.FirstOrDefault(project => project.Id == model.Id);
            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.Message = "Project not found";
                response.AddError(localizer["There is no project with given Id."]);
                return StatusCode(StatusCodes.Status404NotFound, response);
            }

            var selectedProjectMember = vwsDbContext.ProjectMembers.FirstOrDefault(projectMember =>
                                                                    projectMember.UserProfileId == userId &&
                                                                    projectMember.IsDeleted == false &&
                                                                    projectMember.ProjectId == model.Id);
            if (selectedProject == null)
            {
                response.Message = "Project access denied";
                response.AddError(localizer["You are not a memeber of project."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (model.StartDate.HasValue && !(model.EndDate.HasValue) && selectedProject.EndDate.HasValue)
            {
                if (model.StartDate > selectedProject.EndDate)
                {
                    response.Message = "Project model data has problem";
                    response.AddError(localizer["Start Date should be before End Date."]);
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
            }
            if (model.EndDate.HasValue && !(model.StartDate.HasValue) && selectedProject.StartDate.HasValue)
            {
                if (model.EndDate < selectedProject.StartDate)
                {
                    response.Message = "Project model data has problem";
                    response.AddError(localizer["Start Date should be before End Date."]);
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
            }

            selectedProject.Name = model.Name;
            selectedProject.Description = model.Description;
            selectedProject.EndDate = model.EndDate;
            selectedProject.StartDate = model.StartDate;
            selectedProject.Color = model.Color;
            vwsDbContext.Save();

            response.Message = "Project updated successfully!";
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("delete")]
        public IActionResult DeleteProject(int id)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedProject = vwsDbContext.Projects.FirstOrDefault(project => project.Id == id);
            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.AddError(localizer["There is no project with given Id."]);
                response.Message = "Projet not found";
                return StatusCode(StatusCodes.Status404NotFound, response);
            }

            var selectedProjectMember = vwsDbContext.ProjectMembers.FirstOrDefault(projectMember =>
                                                                    projectMember.UserProfileId == userId &&
                                                                    projectMember.IsDeleted == false &&
                                                                    projectMember.ProjectId == id);
            if (selectedProjectMember == null)
            {
                response.AddError(localizer["You are not a memeber of project."]);
                response.Message = "Project access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedProject.IsDeleted = true;
            vwsDbContext.Save();

            response.Message = "Project deleted successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getActive")]
        public IEnumerable<ProjectResponseModel> GetActiveProject()
        {
            var response = new List<ProjectResponseModel>();
            Guid userId = LoggedInUserId.Value;

            HashSet<int> projectIds = vwsDbContext.ProjectMembers
                                      .Where(projectMember => projectMember.UserProfileId == userId && projectMember.IsDeleted == false)
                                      .Select(projectMember => projectMember.ProjectId).ToHashSet<int>();

            var userProjects = vwsDbContext.Projects.Where(project => projectIds.Contains(project.Id) && project.IsDeleted == false && project.StatusId == (byte)SeedDataEnum.ProjectStatuses.Active);

            foreach (var project in userProjects)
            {
                response.Add(new ProjectResponseModel()
                {
                    Id = project.Id,
                    Description = project.Description,
                    Color = project.Color,
                    EndDate = project.EndDate,
                    Guid = project.Guid,
                    IsDelete = project.IsDeleted,
                    Name = project.Name,
                    StartDate = project.StartDate,
                    StatusId = project.StatusId
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

            HashSet<int> projectIds = vwsDbContext.ProjectMembers
                                      .Where(projectMember => projectMember.UserProfileId == userId && projectMember.IsDeleted == false)
                                      .Select(projectMember => projectMember.ProjectId).ToHashSet<int>();

            var userProjects = vwsDbContext.Projects.Where(project => projectIds.Contains(project.Id) && project.IsDeleted == false && project.StatusId == (byte)SeedDataEnum.ProjectStatuses.Hold);

            foreach (var project in userProjects)
            {
                response.Add(new ProjectResponseModel()
                {
                    Id = project.Id,
                    Description = project.Description,
                    Color = project.Color,
                    EndDate = project.EndDate,
                    Guid = project.Guid,
                    IsDelete = project.IsDeleted,
                    Name = project.Name,
                    StartDate = project.StartDate,
                    StatusId = project.StatusId
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

            HashSet<int> projectIds = vwsDbContext.ProjectMembers
                                      .Where(projectMember => projectMember.UserProfileId == userId && projectMember.IsDeleted == false)
                                      .Select(projectMember => projectMember.ProjectId).ToHashSet<int>();

            var userProjects = vwsDbContext.Projects.Where(project => projectIds.Contains(project.Id) && project.IsDeleted == false && project.StatusId == (byte)SeedDataEnum.ProjectStatuses.DoneOrArchived);

            foreach (var project in userProjects)
            {
                response.Add(new ProjectResponseModel()
                {
                    Id = project.Id,
                    Description = project.Description,
                    Color = project.Color,
                    EndDate = project.EndDate,
                    Guid = project.Guid,
                    IsDelete = project.IsDeleted,
                    Name = project.Name,
                    StartDate = project.StartDate,
                    StatusId = project.StatusId
                });
            }

            return response;
        }

        [HttpPost]
        [Authorize]
        [Route("addTeammateToProject")]
        public async Task<IActionResult> AddTeamMateToProject([FromBody] AddTeamMateToProjectModel model)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;
            var selectedUserId = new Guid(model.UserId);

            var selectedProject = vwsDbContext.Projects.FirstOrDefault(project => project.Id == model.ProjectId);

            if (selectedProject == null || selectedProject.IsDeleted)
            {
                response.AddError(localizer["There is no project with given Id."]);
                response.Message = "Projet not found";
                return StatusCode(StatusCodes.Status404NotFound, response);
            }

            var selectedTeam = await vwsDbContext.GetTeamAsync(model.TeamId);

            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.AddError(localizer["There is no team with given Id."]);
                response.Message = "Team not found";
                return StatusCode(StatusCodes.Status404NotFound, response);
            }

            if (!vwsDbContext.UserProfiles.Any(profile => profile.UserId == selectedUserId))
            {
                response.AddError(localizer["There is no user with given Id."]);
                response.Message = "User not found";
                return StatusCode(StatusCodes.Status404NotFound, response);
            }

            var selectedTeamMember = await vwsDbContext.GetTeamMemberAsync(model.TeamId, userId);
            if(selectedTeamMember == null)
            {
                response.AddError(localizer["You are not a member of team."]);
                response.Message = "Not member of team";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedTeamMember = await vwsDbContext.GetTeamMemberAsync(model.TeamId, selectedUserId);
            if(selectedTeamMember == null)
            {
                response.AddError(localizer["User you want to to add, is not a member of selected team."]);
                response.Message = "Not member of team";
                return StatusCode(StatusCodes.Status406NotAcceptable, response);
            }

            if(!vwsDbContext.ProjectMembers.Any(projectMember => projectMember.UserProfileId == userId && projectMember.ProjectId == model.ProjectId && projectMember.IsDeleted == false))
            {
                response.AddError(localizer["You are not a memeber of project."]);
                response.Message = "Project access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (vwsDbContext.ProjectMembers.Any(projectMember => projectMember.UserProfileId == selectedUserId && projectMember.ProjectId == model.ProjectId && projectMember.IsDeleted == false))
            {
                response.AddError(localizer["User you want to add, is already a member of selected project."]);
                response.Message = "User added before";
                return StatusCode(StatusCodes.Status208AlreadyReported, response);
            }

            var newProjectMember = new ProjectMember()
            {
                CreatedOn = DateTime.Now,
                IsDeleted = false,
                ProjectId = model.ProjectId,
                UserProfileId = selectedUserId
            };

            await vwsDbContext.AddProjectMemberAsync(newProjectMember);

            response.Message = "User added to project successfully!";
            return Ok(response);
        }
    }
}
