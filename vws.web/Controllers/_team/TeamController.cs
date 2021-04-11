using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._team;
using vws.web.Models._team;
using vws.web.Models;
using vws.web.Repositories;
using vws.web.Domain._file;
using Microsoft.EntityFrameworkCore;
using vws.web.Enums;
using vws.web.Models._department;
using vws.web.Services._department;
using vws.web.Services._team;
using static vws.web.EmailTemplates.EmailTemplateTypes;
using vws.web.EmailTemplates;
using Microsoft.Extensions.Configuration;
using System.Net;
using Newtonsoft.Json;
using vws.web.Services;
using vws.web.Models._project;
using vws.web.Services._project;
using vws.web.Models._task;
using vws.web.Services._task;

namespace vws.web.Controllers._team
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class TeamController : BaseController
    {
        #region Feilds
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDepartmentManagerService _departmentManager;
        private readonly IStringLocalizer<TeamController> _localizer;
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly IFileManager _fileManager;
        private readonly ITeamManagerService _teamManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly IPermissionService _permissionService;
        private readonly IProjectManagerService _projectManager;
        private readonly ITaskManagerService _taskManagerService;
        #endregion

        #region Ctor
        public TeamController(UserManager<ApplicationUser> userManager, IStringLocalizer<TeamController> localizer,
            IVWS_DbContext vwsDbContext, IFileManager fileManager, IDepartmentManagerService departmentManager,
            ITeamManagerService teamManager, IEmailSender emailSender, IConfiguration configuration, IPermissionService permissionService,
            IProjectManagerService projectManager, ITaskManagerService taskManagerService)
        {
            _userManager = userManager;
            _localizer = localizer;
            _vwsDbContext = vwsDbContext;
            _fileManager = fileManager;
            _departmentManager = departmentManager;
            _teamManager = teamManager;
            _emailSender = emailSender;
            _configuration = configuration;
            _permissionService = permissionService;
            _projectManager = projectManager;
            _taskManagerService = taskManagerService;
        }
        #endregion

        #region PrivateMethods
        private void CreateTeamTaskStatuses(int teamId)
        {
            _vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 2, ProjectId = null, UserProfileId = null, TeamId = teamId, Title = "To Do" });
            _vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 4, ProjectId = null, UserProfileId = null, TeamId = teamId, Title = "Doing" });
            _vwsDbContext.AddTaskStatus(new Domain._task.TaskStatus() { EvenOrder = 6, ProjectId = null, UserProfileId = null, TeamId = teamId, Title = "Done" });

            _vwsDbContext.Save();
        }

        private List<Guid> GetUserTeammates()
        {
            List<Team> userTeams = _vwsDbContext.GetUserTeams(LoggedInUserId.Value).ToList();
            return _vwsDbContext.TeamMembers
                               .Where(teamMember => userTeams.Select(userTeam => userTeam.Id).Contains(teamMember.TeamId) && !teamMember.IsDeleted)
                               .Select(teamMember => teamMember.UserProfileId).Distinct().Where(id => id != LoggedInUserId.Value).ToList();
        }

        private async Task<List<DepartmentResponseModel>> GetDepartments(int teamId)
        {
            var result = new List<DepartmentResponseModel>();

            var departments = _vwsDbContext.Departments.Where(department => department.TeamId == teamId && !department.IsDeleted);

            foreach (var department in departments)
            {
                result.Add(new DepartmentResponseModel()
                {
                    Id = department.Id,
                    Color = department.Color,
                    CreatedBy = (await _vwsDbContext.GetUserProfileAsync(department.CreatedBy)).NickName,
                    DepartmentImageGuid = department.DepartmentImageGuid,
                    Description = department.Description,
                    CreatedOn = department.CreatedOn,
                    Guid = department.Guid,
                    ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(department.ModifiedBy)).NickName,
                    ModifiedOn = department.ModifiedOn,
                    Name = department.Name,
                    TeamId = department.TeamId,
                    DepartmentMembers = await _departmentManager.GetDepartmentMembers(department.Id)
                });
            }

            return result;
        }

        private async Task CreateTeamDepartments(List<DepartmentBaseModel> models, int teamId)
        {
            foreach (var model in models)
            {
                DepartmentModel departmentModel = new DepartmentModel()
                {
                    Color = model.Color,
                    Description = model.Description,
                    Name = model.Name,
                    TeamId = teamId,
                    Users = model.Users
                };
                departmentModel.TeamId = teamId;
                await _departmentManager.CreateDepartment(departmentModel, LoggedInUserId.Value);
            }
        }

        private async Task SendJoinTeamInvitaionLinks(List<string> emails, int teamId)
        {
            string emailErrorMessage;
            Guid linkGuid = Guid.NewGuid();
            var newInviteLink = new TeamInviteLink()
            {
                TeamId = teamId,
                CreatedBy = LoggedInUserId.Value,
                ModifiedBy = LoggedInUserId.Value,
                CreatedOn = DateTime.Now,
                ModifiedOn = DateTime.Now,
                LinkGuid = linkGuid,
                IsRevoked = false
            };

            await _vwsDbContext.AddTeamInviteLinkAsync(newInviteLink);
            _vwsDbContext.Save();
            SendEmailModel emailModelTemplate = new SendEmailModel
            {
                FromEmail = _configuration["EmailSender:RegistrationEmail:EmailAddress"],
                ToEmail = "",
                Subject = "Join Team",
                Body = "https://app.seventask.com/en-US/inviteTeam?invitationCode=" + linkGuid.ToString(),
                //Body = EmailTemplateUtility.GetEmailTemplate((int)EmailTemplateEnum.EmailVerificationCode).Replace("{0}", randomCode),
                Credential = new NetworkCredential
                {
                    UserName = _configuration["EmailSender:RegistrationEmail:UserName"],
                    Password = _configuration["EmailSender:RegistrationEmail:Password"]
                },
                IsBodyHtml = false
            };
            Task.Run(async () => 
            {
                foreach (var email in emails)
                {
                    emailModelTemplate.ToEmail = email;
                    await _emailSender.SendEmailAsync(emailModelTemplate, out emailErrorMessage);
                }
            });
        }
        #endregion

        #region TeamAPIS
        [HttpPost]
        [Authorize]
        [Route("create")]
        public async Task<IActionResult> CreateTeam([FromBody] TeamModel model)
        {
            var response = new ResponseModel<TeamResponseModel>();
            Guid userId = LoggedInUserId.Value;
            model.Users = model.Users.Distinct().ToList();
            model.Users.Remove(userId);

            #region CheckModel
            var allDepartmentUsers = new List<Guid>();
            foreach (var department in model.Departments)
            {
                department.Users = department.Users.Distinct().ToList();
                allDepartmentUsers.AddRange(department.Users);
            }
            allDepartmentUsers = allDepartmentUsers.Distinct().ToList();
            if (!String.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
            {
                response.Message = "Team model data has problem.";
                response.AddError(_localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Name.Length > 500)
            {
                response.Message = "Team model data has problem.";
                response.AddError(_localizer["Length of title is more than 500 characters."]);
            }
            if (!String.IsNullOrEmpty(model.Color) && model.Color.Length > 6)
            {
                response.Message = "Team model data has problem.";
                response.AddError(_localizer["Length of color is more than 6 characters."]);
            }
            var teammates = GetUserTeammates();
            if (teammates.Intersect(model.Users).Count() != model.Users.Count)
            {
                response.Message = "Team model data has problem.";
                response.AddError(_localizer["Invalid team users."]);
            }
            if (allDepartmentUsers.Intersect(model.Users).Count() != allDepartmentUsers.Count)
            {
                response.Message = "Team model data has problem.";
                response.AddError(_localizer["Invalid department users."]);
            }
            var hasTeamWithSameName = _vwsDbContext.TeamMembers.Any(teamMember => teamMember.UserProfileId == userId &&
                                                                    teamMember.Team.Name == model.Name &&
                                                                    teamMember.Team.IsDeleted == false &&
                                                                    teamMember.IsDeleted == false);
            if (hasTeamWithSameName)
            {
                response.Message = "Team model data has problem.";
                response.AddError(_localizer["You are a member of a team with that name."]);
            }
            if (response.HasError)
                return StatusCode(StatusCodes.Status400BadRequest, response);
            #endregion

            var newTeam = await _teamManager.CreateTeam(model, userId);

            await CreateTeamDepartments(model.Departments, newTeam.Id);

            CreateTeamTaskStatuses(newTeam.Id);

            SendJoinTeamInvitaionLinks(model.EmailsForInvite, newTeam.Id);

            var newHistory = new TeamHistory()
            {
                TeamId = newTeam.Id,
                EventTime = newTeam.CreatedOn,
                Event = "Team {0} created by {1}."
            };
            _vwsDbContext.AddTeamHistory(newHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = newTeam.Name,
                TeamHistoryId = newHistory.Id
            });
            _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel() 
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TeamHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();

            var newTeamResponse = new TeamResponseModel()
            {
                Id = newTeam.Id,
                TeamTypeId = newTeam.TeamTypeId,
                Name = newTeam.Name,
                Description = newTeam.Description,
                Color = newTeam.Color,
                CreatedBy = (await _vwsDbContext.GetUserProfileAsync(newTeam.CreatedBy)).NickName,
                ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(newTeam.ModifiedBy)).NickName,
                CreatedOn = newTeam.CreatedOn,
                ModifiedOn = newTeam.ModifiedOn,
                Guid = newTeam.Guid,
                TeamImageGuid = newTeam.TeamImageGuid,
                NumberOfDepartments = _vwsDbContext.Departments.Where(department => department.TeamId == newTeam.Id && !department.IsDeleted).Count(),
                NumberOfMembers = _vwsDbContext.TeamMembers.Where(teamMember => teamMember.TeamId == newTeam.Id && !teamMember.IsDeleted).Count(),
                NumberOfTasks = _vwsDbContext.GeneralTasks.Where(task => task.TeamId == newTeam.Id && !task.IsDeleted).Count(),
                NumberOfProjects = _vwsDbContext.Projects.Where(project => project.TeamId == newTeam.Id && !project.IsDeleted).Count()
            };

            response.Value = newTeamResponse;
            response.Message = "Team created successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateTeamName")]
        public async Task<IActionResult> UpdateTeamName(int id, string newName)
        {
            var response = new ResponseModel();
            Guid userId = LoggedInUserId.Value;

            if (String.IsNullOrEmpty(newName) || newName.Length > 500)
            {
                response.Message = "Team model data has problem.";
                response.AddError(_localizer["Team name can not be empty and should have less than 500 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeam = await _vwsDbContext.GetTeamAsync(id);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.Message = "Team not found";
                response.AddError(_localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (!_permissionService.HasAccessToTeam(userId, id))
            {
                response.Message = "Access team is forbidden";
                response.AddError(_localizer["You are not a member of team."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedTeam.ModifiedBy = userId;
            selectedTeam.ModifiedOn = DateTime.Now;
            selectedTeam.Name = newName;
            _vwsDbContext.Save();

            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateTeamDescription")]
        public async Task<IActionResult> UpdateTeamDescription(int id, string newDescription)
        {
            var response = new ResponseModel();
            Guid userId = LoggedInUserId.Value;

            if (!String.IsNullOrEmpty(newDescription) && newDescription.Length > 2000)
            {
                response.Message = "Team model data has problem.";
                response.AddError(_localizer["Length of description is more than 2000 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeam = await _vwsDbContext.GetTeamAsync(id);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.Message = "Team not found";
                response.AddError(_localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (!_permissionService.HasAccessToTeam(userId, id))
            {
                response.Message = "Access team is forbidden";
                response.AddError(_localizer["You are not a member of team."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedTeam.ModifiedBy = userId;
            selectedTeam.ModifiedOn = DateTime.Now;
            selectedTeam.Description = newDescription;
            _vwsDbContext.Save();

            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateTeamColor")]
        public async Task<IActionResult> UpdateTeamColor(int id, string newColor)
        {
            var response = new ResponseModel();
            Guid userId = LoggedInUserId.Value;

            if (!String.IsNullOrEmpty(newColor) && newColor.Length > 6)
            {
                response.Message = "Team model data has problem.";
                response.AddError(_localizer["Length of color is more than 6 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeam = await _vwsDbContext.GetTeamAsync(id);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.Message = "Team not found";
                response.AddError(_localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (!_permissionService.HasAccessToTeam(userId, id))
            {
                response.Message = "Access team is forbidden";
                response.AddError(_localizer["You are not a member of team."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedTeam.ModifiedBy = userId;
            selectedTeam.ModifiedOn = DateTime.Now;
            selectedTeam.Color = newColor;
            _vwsDbContext.Save();

            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("uploadTeamImage")]
        public async Task<IActionResult> UploadTeamImage(IFormFile image, int teamId)
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

            var selectedTeam = await _vwsDbContext.GetTeamAsync(teamId);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.AddError(_localizer["There is no team with given Id."]);
                response.Message = "Team not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeamMember = await _vwsDbContext.GetTeamMemberAsync(teamId, userId);
            if (selectedTeamMember == null)
            {
                response.AddError(_localizer["You are not a member of team."]);
                response.Message = "Not member of team";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            ResponseModel<File> fileResponse;

            if (selectedTeam.TeamImage != null)
            {
                fileResponse = await _fileManager.WriteFile(uploadedImage, userId, "profileImages", (int)selectedTeam.TeamImageId, types.ToList());
                if (fileResponse.HasError)
                {
                    foreach (var error in fileResponse.Errors)
                        response.AddError(_localizer[error]);
                    response.Message = "Error in writing file";
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
                selectedTeam.TeamImage.RecentFileId = fileResponse.Value.Id;
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
                selectedTeam.TeamImageId = newFileContainer.Id;
                selectedTeam.TeamImageGuid = newFileContainer.Guid;
            }
            selectedTeam.ModifiedBy = LoggedInUserId.Value;
            selectedTeam.ModifiedOn = DateTime.Now;
            _vwsDbContext.Save();

            var newHistory = new TeamHistory()
            {
                TeamId = selectedTeam.Id,
                EventTime = selectedTeam.ModifiedOn,
                Event = "Team image updated to {0} by {1}."
            };
            _vwsDbContext.AddTeamHistory(newHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.File,
                Body = JsonConvert.SerializeObject(new FileModel()
                {
                    Extension = fileResponse.Value.Extension,
                    FileContainerGuid = fileResponse.Value.FileContainerGuid,
                    Name = fileResponse.Value.Name,
                    Size = fileResponse.Value.Size
                }),
                TeamHistoryId = newHistory.Id
            });
            _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TeamHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();

            response.Value = fileResponse.Value.FileContainerGuid;
            response.Message = "Team image added successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("isNameOfGroupUsed")]
        public bool IsNameOfGroupUsed(string name)
        {
            Guid userId = LoggedInUserId.Value;

            return _vwsDbContext.TeamMembers.Any(teamMember => teamMember.UserProfileId == userId &&
                                                teamMember.Team.Name == name &&
                                                teamMember.Team.IsDeleted == false &&
                                                teamMember.IsDeleted == false);
        }

        [HttpGet]
        [Authorize]
        [Route("getAll")]
        public async Task<IEnumerable<TeamResponseModel>> GetAllTeams()
        {
            Guid userId = LoggedInUserId.Value;

            List<TeamResponseModel> response = new List<TeamResponseModel>();

            var userTeams = _vwsDbContext.GetUserTeams(userId);

            foreach (var userTeam in userTeams)
            {
                response.Add(new TeamResponseModel()
                {
                    Id = userTeam.Id,
                    TeamTypeId = userTeam.TeamTypeId,
                    Name = userTeam.Name,
                    Description = userTeam.Description,
                    Color = userTeam.Color,
                    CreatedBy = (await _vwsDbContext.GetUserProfileAsync(userTeam.CreatedBy)).NickName,
                    ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(userTeam.ModifiedBy)).NickName,
                    CreatedOn = userTeam.CreatedOn,
                    ModifiedOn = userTeam.ModifiedOn,
                    Guid = userTeam.Guid,
                    TeamImageGuid = userTeam.TeamImageGuid,
                    NumberOfDepartments = _vwsDbContext.Departments.Where(department => department.TeamId == userTeam.Id && !department.IsDeleted).Count(),
                    NumberOfMembers = _vwsDbContext.TeamMembers.Where(teamMember => teamMember.TeamId == userTeam.Id && !teamMember.IsDeleted).Count(),
                    NumberOfTasks = _vwsDbContext.GeneralTasks.Where(task => task.TeamId == userTeam.Id && !task.IsDeleted).Count(),
                    NumberOfProjects = _vwsDbContext.Projects.Where(project => project.TeamId == userTeam.Id && !project.IsDeleted).Count()
                });
            }
            return response;
        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public async Task<IActionResult> GetTeam(int id)
        {
            Guid userId = LoggedInUserId.Value;

            var response = new ResponseModel<TeamResponseModel>();

            var selectedTeam = await _vwsDbContext.GetTeamAsync(id);

            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.AddError(_localizer["There is no team with given Id."]);
                response.Message = "Team not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var teamMember = _vwsDbContext.GetTeamMemberAsync(id, userId);
            if (teamMember == null)
            {
                response.AddError(_localizer["You are not a member of team."]);
                response.Message = "Team access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            response.Value = new TeamResponseModel()
            {
                Id = selectedTeam.Id,
                TeamTypeId = selectedTeam.TeamTypeId,
                Name = selectedTeam.Name,
                Description = selectedTeam.Description,
                Color = selectedTeam.Color,
                CreatedBy = (await _vwsDbContext.GetUserProfileAsync(selectedTeam.CreatedBy)).NickName,
                Guid = selectedTeam.Guid,
                ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(selectedTeam.ModifiedBy)).NickName,
                CreatedOn = selectedTeam.CreatedOn,
                ModifiedOn = selectedTeam.ModifiedOn,
                TeamImageGuid = selectedTeam.TeamImageGuid,
                NumberOfDepartments = _vwsDbContext.Departments.Where(department => department.TeamId == selectedTeam.Id && !department.IsDeleted).Count(),
                NumberOfMembers = _vwsDbContext.TeamMembers.Where(teamMember => teamMember.TeamId == selectedTeam.Id && !teamMember.IsDeleted).Count(),
                NumberOfTasks = _vwsDbContext.GeneralTasks.Where(task => task.TeamId == selectedTeam.Id && !task.IsDeleted).Count(),
                NumberOfProjects = _vwsDbContext.Projects.Where(project => project.TeamId == selectedTeam.Id && !project.IsDeleted).Count(),
                Users = await _teamManager.GetTeamMembers(selectedTeam.Id),
                Departments = await GetDepartments(selectedTeam.Id)
            };
            response.Message = "Team retured successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getDepartments")]
        public async Task<IActionResult> GetTeamDepartments(int id)
        {
            var response = new ResponseModel<List<DepartmentResponseModel>>();
            var departments = new List<DepartmentResponseModel>();
            var userId = LoggedInUserId.Value;

            var selectedTeam = await _vwsDbContext.GetTeamAsync(id);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.Message = "Team not found";
                response.AddError(_localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeamMember = await _vwsDbContext.GetTeamMemberAsync(id, userId);
            if (selectedTeamMember == null)
            {
                response.Message = "Access team is forbidden";
                response.AddError(_localizer["You are not a member of team."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var teamDepartments = _vwsDbContext.Departments.Where(department => department.TeamId == id && !department.IsDeleted);

            foreach (var teamDepartment in teamDepartments)
                departments.Add(new DepartmentResponseModel()
                {
                    Id = teamDepartment.Id,
                    Name = teamDepartment.Name,
                    DepartmentImageGuid = teamDepartment.DepartmentImageGuid,
                    Description = teamDepartment.Description,
                    Color = teamDepartment.Color,
                    CreatedBy = (await _vwsDbContext.GetUserProfileAsync(teamDepartment.CreatedBy)).NickName,
                    CreatedOn = teamDepartment.CreatedOn,
                    Guid = teamDepartment.Guid,
                    ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(teamDepartment.ModifiedBy)).NickName,
                    ModifiedOn = teamDepartment.ModifiedOn,
                    TeamId = teamDepartment.TeamId,
                    DepartmentMembers = await _departmentManager.GetDepartmentMembers(teamDepartment.Id)
                });

            response.Value = departments;
            response.Message = "Team departments returned successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getProjects")]
        public async Task<IActionResult> GetTeamProjects(int id)
        {
            var response = new ResponseModel<List<ProjectResponseModel>>();
            var projects = new List<ProjectResponseModel>();
            var userId = LoggedInUserId.Value;

            var selectedTeam = await _vwsDbContext.GetTeamAsync(id);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.Message = "Team not found";
                response.AddError(_localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTeam(userId, id))
            {
                response.Message = "Access team is forbidden";
                response.AddError(_localizer["You are not a member of team."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var teamProjects = _vwsDbContext.Projects.Where(project => project.TeamId == id && !project.IsDeleted);
            foreach (var teamProject in teamProjects)
                projects.Add(new ProjectResponseModel()
                {
                    Id = teamProject.Id,
                    Description = teamProject.Description,
                    Color = teamProject.Color,
                    EndDate = teamProject.EndDate,
                    Guid = teamProject.Guid,
                    Name = teamProject.Name,
                    StartDate = teamProject.StartDate,
                    StatusId = teamProject.StatusId,
                    TeamId = teamProject.TeamId,
                    TeamName = teamProject.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == teamProject.TeamId).Name,
                    ProjectImageGuid = teamProject.ProjectImageGuid,
                    DepartmentIds = teamProject.ProjectDepartments.Select(projectDepartment => projectDepartment.DepartmentId).ToList(),
                    NumberOfUpdates = _vwsDbContext.ProjectHistories.Where(history => history.ProjectId == teamProject.Id).Count(),
                    Users = _projectManager.GetProjectUsers(teamProject.Id),
                    NumberOfTasks = _projectManager.GetNumberOfProjectTasks(teamProject.Id)
                });

            response.Value = projects;
            response.Message = "Team departments returned successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getTasks")]
        public async Task<IActionResult> GetTeamTasks(int id)
        {
            var response = new ResponseModel<List<TaskResponseModel>>();
            var tasks = new List<TaskResponseModel>();
            var userId = LoggedInUserId.Value;

            var selectedTeam = await _vwsDbContext.GetTeamAsync(id);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.Message = "Team not found";
                response.AddError(_localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTeam(userId, id))
            {
                response.Message = "Access team is forbidden";
                response.AddError(_localizer["You are not a member of team."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var teamTasks = _vwsDbContext.GeneralTasks.Where(task => task.TeamId == id && !task.IsDeleted);
            foreach (var teamTask in teamTasks)
                tasks.Add(new TaskResponseModel()
                {
                    Id = teamTask.Id,
                    Title = teamTask.Title,
                    Description = teamTask.Description,
                    StartDate = teamTask.StartDate,
                    EndDate = teamTask.EndDate,
                    CreatedOn = teamTask.CreatedOn,
                    ModifiedOn = teamTask.ModifiedOn,
                    CreatedBy = (await _vwsDbContext.GetUserProfileAsync(teamTask.CreatedBy)).NickName,
                    ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(teamTask.ModifiedBy)).NickName,
                    Guid = teamTask.Guid,
                    PriorityId = teamTask.TaskPriorityId,
                    PriorityTitle = _localizer[((SeedDataEnum.TaskPriority)teamTask.TaskPriorityId).ToString()],
                    UsersAssignedTo = _taskManagerService.GetAssignedTo(teamTask.Id),
                    ProjectId = teamTask.ProjectId,
                    TeamId = teamTask.TeamId,
                    TeamName = teamTask.TeamId == null ? null : _vwsDbContext.Teams.FirstOrDefault(team => team.Id == teamTask.TeamId).Name,
                    ProjectName = teamTask.ProjectId == null ? null : _vwsDbContext.Projects.FirstOrDefault(project => project.Id == teamTask.ProjectId).Name,
                    StatusId = teamTask.TaskStatusId,
                    StatusTitle = _vwsDbContext.TaskStatuses.FirstOrDefault(statuse => statuse.Id == teamTask.TaskStatusId).Title,
                    CheckLists = _taskManagerService.GetCheckLists(teamTask.Id),
                    Tags = _taskManagerService.GetTaskTags(teamTask.Id),
                    Comments = await _taskManagerService.GetTaskComments(teamTask.Id),
                    Attachments = _taskManagerService.GetTaskAttachments(teamTask.Id)
                });

            response.Value = tasks;
            response.Message = "Team departments returned successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getTeamHistory")]
        public async Task<IActionResult> GetTeamHistory(int id)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel<List<HistoryModel>>();

            var selectedTeam = _vwsDbContext.Teams.FirstOrDefault(team => team.Id == id);

            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.Message = "Team not found";
                response.AddError(_localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeamMember = await _vwsDbContext.GetTeamMemberAsync(id, userId);
            if (selectedTeamMember == null)
            {
                response.Message = "Access team is forbidden";
                response.AddError(_localizer["You are not a member of team."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var events = new List<HistoryModel>();
            var teamEvents = _vwsDbContext.TeamHistories.Where(teamHistory => teamHistory.TeamId == id);
            foreach (var teamEvent in teamEvents)
            {
                var parameters = _vwsDbContext.TeamHistoryParameters.Where(param => param.TeamHistoryId == teamEvent.Id)
                                                                    .OrderBy(param => param.Id)
                                                                    .ToList();
                for (int i = 0; i < parameters.Count(); i++)
                {
                    if (parameters[i].ActivityParameterTypeId == (byte)SeedDataEnum.ActivityParameterTypes.Text && parameters[i].ShouldBeLocalized)
                        parameters[i].Body = _localizer[parameters[i].Body];
                }
                events.Add(new HistoryModel()
                {
                    Message = _localizer[teamEvent.Event],
                    Parameters = parameters.Select(param => new HistoryParameterModel() { ParameterBody = param.Body, ParameterType = param.ActivityParameterTypeId }).ToList(),
                    Time = teamEvent.EventTime
                });
            }

            response.Message = "History returned successfully!";
            response.Value = events;
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("delete")]
        public async Task<IActionResult> DeleteTeam(int teamId)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedTeam = await _vwsDbContext.GetTeamAsync(teamId);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.AddError(_localizer["There is no team with given Id."]);
                response.Message = "Team not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeamMember = await _vwsDbContext.GetTeamMemberAsync(teamId, userId);
            if (selectedTeamMember == null)
            {
                response.AddError(_localizer["You are not a member of team."]);
                response.Message = "Not member of team";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var deletionTime = DateTime.Now;

            selectedTeam.IsDeleted = true;
            selectedTeam.ModifiedBy = userId;
            selectedTeam.ModifiedOn = deletionTime;

            var teamProjects = _vwsDbContext.Projects.Where(project => project.TeamId == teamId &&
                                                                      !project.IsDeleted);

            var teamDepartments = _vwsDbContext.Departments.Where(department => department.TeamId == teamId &&
                                                                               !department.IsDeleted);

            foreach (var teamProject in teamProjects)
            {
                teamProject.IsDeleted = true;
                teamProject.ModifiedBy = userId;
                teamProject.ModifiedOn = deletionTime;
            }

            foreach (var teamDepartment in teamDepartments)
            {
                teamDepartment.IsDeleted = true;
                teamDepartment.ModifiedBy = userId;
                teamDepartment.ModifiedOn = deletionTime;
            }
            _vwsDbContext.Save();

            var newHistory = new TeamHistory()
            {
                TeamId = selectedTeam.Id,
                EventTime = selectedTeam.ModifiedOn,
                Event = "{0} deleted team."
            };
            _vwsDbContext.AddTeamHistory(newHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TeamHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();

            response.Message = "Team deleted successfully!";
            return Ok(response);
        }
        #endregion

        #region InviteLinkAPIS
        [HttpPost]
        [Authorize]
        [Route("createInviteLink")]
        public async Task<IActionResult> CreateInviteLink(int teamId)
        {
            var response = new ResponseModel<TeamInviteLinkResponseModel>();

            var selectedTeam = await _vwsDbContext.GetTeamAsync(teamId);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.Message = "Team not found";
                response.AddError(_localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            Guid userId = LoggedInUserId.Value;

            var teamMember = await _vwsDbContext.GetTeamMemberAsync(teamId, userId);
            if (teamMember == null)
            {
                response.Message = "You are not member of team";
                response.AddError(_localizer["You are not a member of team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            DateTime creationTime = DateTime.Now;

            Guid inviteLinkGuid = Guid.NewGuid();

            var newInviteLink = new TeamInviteLink()
            {
                TeamId = teamId,
                CreatedBy = userId,
                ModifiedBy = userId,
                CreatedOn = creationTime,
                ModifiedOn = creationTime,
                LinkGuid = inviteLinkGuid,
                IsRevoked = false
            };

            await _vwsDbContext.AddTeamInviteLinkAsync(newInviteLink);
            _vwsDbContext.Save();

            var newHistory = new TeamHistory()
            {
                TeamId = teamId,
                EventTime = creationTime,
                Event = "{0} created new invite link with id {1}."
            };
            _vwsDbContext.AddTeamHistory(newHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId) ;
            _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TeamHistoryId = newHistory.Id
            });
            _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = newInviteLink.LinkGuid.ToString(),
                TeamHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();

            response.Value = new TeamInviteLinkResponseModel()
            {
                Id = newInviteLink.Id,
                TeamName = (await _vwsDbContext.GetTeamAsync(newInviteLink.TeamId)).Name,
                IsRevoked = newInviteLink.IsRevoked,
                LinkGuid = newInviteLink.LinkGuid.ToString(),
                CreatedBy = (await _vwsDbContext.GetUserProfileAsync(newInviteLink.CreatedBy)).NickName,
                ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(newInviteLink.ModifiedBy)).NickName,
                CreatedOn = newInviteLink.CreatedOn,
                ModifiedOn = newInviteLink.ModifiedOn
            };

            response.Message = "Invite link created successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("revokeLink")]
        public async Task<IActionResult> RevokeLink(int id)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedInviteLink = await _vwsDbContext.GetTeamInviteLinkByIdAsync(id);

            if (selectedInviteLink == null || selectedInviteLink.Team.IsDeleted || selectedInviteLink.IsRevoked)
            {
                response.Message = "Link not found";
                response.AddError(_localizer["Link does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (selectedInviteLink.CreatedBy != userId || !_vwsDbContext.TeamMembers.Any(teamMember => teamMember.UserProfileId == userId && teamMember.IsDeleted == false))
            {
                response.Message = "Team access forbidden";
                response.AddError(_localizer["You don't have access to this team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedInviteLink.IsRevoked = true;
            _vwsDbContext.Save();

            var newHistory = new TeamHistory()
            {
                TeamId = selectedInviteLink.TeamId,
                EventTime = DateTime.Now,
                Event = "Invite link with id {0} revoked by {1}."
            };
            _vwsDbContext.AddTeamHistory(newHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedInviteLink.LinkGuid.ToString(),
                TeamHistoryId = newHistory.Id
            });
            _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TeamHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();

            response.Message = "Task updated successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getLinks")]
        public async Task<IEnumerable<TeamInviteLinkResponseModel>> GetInviteLinks()
        {
            Guid userId = LoggedInUserId.Value;

            List<TeamInviteLinkResponseModel> response = new List<TeamInviteLinkResponseModel>();

            var userTeamInviteLinks = _vwsDbContext.TeamInviteLinks.Include(teamInviteLink => teamInviteLink.Team)
                                                                  .Where(teamInviteLink => teamInviteLink.CreatedBy == userId &&
                                                                            teamInviteLink.IsRevoked == false &&
                                                                            teamInviteLink.Team.IsDeleted == false);

            var teamMembers = _vwsDbContext.TeamMembers.Where(teamMemeber => teamMemeber.UserProfileId == userId && teamMemeber.IsDeleted == false);

            foreach (var userTeamInviteLink in userTeamInviteLinks)
            {
                if (teamMembers.Any(teamMember => teamMember.TeamId == userTeamInviteLink.TeamId))
                {
                    response.Add(new TeamInviteLinkResponseModel()
                    {
                        Id = userTeamInviteLink.Id,
                        TeamName = (await _vwsDbContext.GetTeamAsync(userTeamInviteLink.TeamId)).Name,
                        IsRevoked = userTeamInviteLink.IsRevoked,
                        LinkGuid = userTeamInviteLink.LinkGuid.ToString(),
                        CreatedBy = (await _vwsDbContext.GetUserProfileAsync(userTeamInviteLink.CreatedBy)).NickName,
                        ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(userTeamInviteLink.ModifiedBy)).NickName,
                        CreatedOn = userTeamInviteLink.CreatedOn,
                        ModifiedOn = userTeamInviteLink.ModifiedOn
                    });
                }
            }
            return response;
        }
        #endregion

        #region TeamMemberAPIS
        [HttpPost]
        [Authorize]
        [Route("join")]
        public async Task<IActionResult> JoinTeam(string guid)
        {
            var response = new ResponseModel();

            Guid linkGuid = new Guid(guid);

            Guid userId = LoggedInUserId.Value;

            var selectedTeamLink = await _vwsDbContext.GetTeamInviteLinkByLinkGuidAsync(linkGuid);

            if (selectedTeamLink == null || selectedTeamLink.Team.IsDeleted || selectedTeamLink.IsRevoked)
            {
                response.Message = "Invalid link";
                response.AddError(_localizer["Link is not valid."]);
                return StatusCode(StatusCodes.Status406NotAcceptable, response);
            }

            if ((await _vwsDbContext.GetTeamMemberAsync(selectedTeamLink.TeamId, userId)) != null)
            {
                response.Message = "User already joined";
                response.AddError(_localizer["You are already joined the team."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var newTeamMember = new TeamMember()
            {
                TeamId = selectedTeamLink.TeamId,
                CreatedOn = DateTime.Now,
                UserProfileId = userId
            };
            await _vwsDbContext.AddTeamMemberAsync(newTeamMember);
            _vwsDbContext.Save();

            var newHistory = new TeamHistory()
            {
                TeamId = newTeamMember.TeamId,
                EventTime = newTeamMember.CreatedOn,
                Event = "{0} joined the team using invite link with guid {1}."
            };
            _vwsDbContext.AddTeamHistory(newHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                TeamHistoryId = newHistory.Id
            });
            _vwsDbContext.AddTeamHistoryParameter(new TeamHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedTeamLink.LinkGuid.ToString(),
                TeamHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();

            response.Message = "User added to team successfully!";
            return Ok(response);
        }
        #endregion

        #region TeammateAPIS
        [HttpGet]
        [Authorize]
        [Route("getTeammates")]
        public async Task<IActionResult> GetTeammates(int id)
        {
            var response = new ResponseModel<List<UserModel>>();
            var teammatesList = new List<UserModel>();

            var selectedTeam = await _vwsDbContext.GetTeamAsync(id);
            var userId = LoggedInUserId.Value;

            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.Message = "Team not found";
                response.AddError(_localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeamMember = await _vwsDbContext.GetTeamMemberAsync(id, userId);
            if (selectedTeamMember == null)
            {
                response.Message = "Access team is forbidden";
                response.AddError(_localizer["You are not a member of team."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            List<UserProfile> userTeamMates = _vwsDbContext.TeamMembers
                .Include(teamMember => teamMember.UserProfile)
                .Where(teamMember => teamMember.TeamId == id && teamMember.IsDeleted == false)
                .Select(teamMember => teamMember.UserProfile).Distinct().ToList();

            foreach (var teamMate in userTeamMates)
            {
                UserProfile userProfile = await _vwsDbContext.GetUserProfileAsync(teamMate.UserId);
                teammatesList.Add(new UserModel()
                {
                    UserId = teamMate.UserId,
                    NickName = userProfile.NickName,
                    ProfileImageGuid = userProfile.ProfileImageGuid
                });
            }

            response.Message = "Team mates are given successfully!";
            response.Value = teammatesList;
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getAllTeamMates")]
        public async Task<ICollection<UserModel>> GetAllTeamMates()
        {
            var result = new List<UserModel>();

            var userTeamMates = GetUserTeammates();

            foreach (var userId in userTeamMates)
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                var userProfile = await _vwsDbContext.GetUserProfileAsync(userId);
                result.Add(new UserModel()
                {
                    UserId = userId,
                    ProfileImageGuid = userProfile.ProfileImageGuid,
                    NickName = userProfile.NickName
                });
            }

            return result;
        }
        #endregion
    }
}
