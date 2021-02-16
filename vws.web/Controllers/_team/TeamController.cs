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
using vws.web.Domain._team;
using vws.web.Models._team;
using vws.web.Models;
using vws.web.Repositories;
using vws.web.Domain._file;
using Microsoft.EntityFrameworkCore;
using vws.web.Enums;
using vws.web.Models._department;

namespace vws.web.Controllers._team
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class TeamController : BaseController
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IEmailSender emailSender;
        private readonly IConfiguration configuration;
        private readonly IPasswordHasher<ApplicationUser> passwordHasher;
        private readonly IStringLocalizer<TeamController> localizer;
        private readonly IVWS_DbContext vwsDbContext;
        private readonly IFileManager fileManager;

        public TeamController(UserManager<ApplicationUser> _userManager, RoleManager<IdentityRole> _roleManager,
            SignInManager<ApplicationUser> _signInManager, IConfiguration _configuration, IEmailSender _emailSender,
            IPasswordHasher<ApplicationUser> _passwordHasher, IStringLocalizer<TeamController> _localizer,
            IVWS_DbContext _vwsDbContext, IFileManager _fileManager)
        {
            userManager = _userManager;
            roleManager = _roleManager;
            signInManager = _signInManager;
            configuration = _configuration;
            emailSender = _emailSender;
            passwordHasher = _passwordHasher;
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
            fileManager = _fileManager;
        }

        [HttpPost]
        [Authorize]
        [Route("create")]
        public async Task<IActionResult> CreateTeam([FromBody] TeamModel model)
        {
            var response = new ResponseModel<TeamResponseModel>();

            if (!String.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
            {
                response.Message = "Team model data has problem.";
                response.AddError(localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Name.Length > 500)
            {
                response.Message = "Team model data has problem.";
                response.AddError(localizer["Length of title is more than 500 characters."]);
            }
            if (!String.IsNullOrEmpty(model.Color) && model.Color.Length > 6)
            {
                response.Message = "Team model data has problem.";
                response.AddError(localizer["Length of color is more than 6 characters."]);
            }

            if (response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            Guid userId = LoggedInUserId.Value;

            var hasTeamWithSameName = vwsDbContext.TeamMembers.Any(teamMember => teamMember.UserProfileId == userId &&
                                                                    teamMember.Team.Name == model.Name &&
                                                                    teamMember.Team.IsDeleted == false &&
                                                                    teamMember.HasUserLeft == false);
            if (hasTeamWithSameName)
            {
                response.Message = "Team model data has problem.";
                response.AddError(localizer["You are a member of a team with that name."]);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            DateTime creationTime = DateTime.Now;

            var newTeam = new Team()
            {
                Name = model.Name,
                TeamTypeId = (byte)SeedDataEnum.TeamTypes.Team,
                Description = model.Description,
                Color = model.Color,
                CreatedOn = creationTime,
                CreatedBy = userId,
                ModifiedOn = creationTime,
                ModifiedBy = userId,
                Guid = Guid.NewGuid()
            };

            await vwsDbContext.AddTeamAsync(newTeam);
            vwsDbContext.Save();

            var newTeamMember = new TeamMember()
            {
                TeamId = newTeam.Id,
                UserProfileId = userId,
                CreatedOn = DateTime.Now
            };

            await vwsDbContext.AddTeamMemberAsync(newTeamMember);
            vwsDbContext.Save();

            var newTeamResponse = new TeamResponseModel()
            {
                Id = newTeam.Id,
                TeamTypeId = newTeam.TeamTypeId,
                Name = newTeam.Name,
                Description = newTeam.Description,
                Color = newTeam.Color,
                CreatedBy = (await userManager.FindByIdAsync(newTeam.CreatedBy.ToString())).UserName,
                ModifiedBy = (await userManager.FindByIdAsync(newTeam.ModifiedBy.ToString())).UserName,
                CreatedOn = newTeam.CreatedOn,
                ModifiedOn = newTeam.ModifiedOn,
                Guid = newTeam.Guid,
                TeamImageId = newTeam.TeamImageId
            };

            response.Value = newTeamResponse;
            response.Message = "Team created successfully!";
            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        [Route("createInviteLink")]
        public async Task<IActionResult> CreateInviteLink(int teamId)
        {
            var response = new ResponseModel<TeamInviteLinkResponseModel>();

            var selectedTeam = await vwsDbContext.GetTeamAsync(teamId);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.Message = "Team not found";
                response.AddError(localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            Guid userId = LoggedInUserId.Value;

            var teamMember = await vwsDbContext.GetTeamMemberAsync(teamId, userId);
            if (teamMember == null)
            {
                response.Message = "You are not member of team";
                response.AddError(localizer["You are not a member of team."]);
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

            await vwsDbContext.AddTeamInviteLinkAsync(newInviteLink);
            vwsDbContext.Save();

            response.Value = new TeamInviteLinkResponseModel()
            {
                Id = newInviteLink.Id,
                TeamName = (await vwsDbContext.GetTeamAsync(newInviteLink.TeamId)).Name,
                IsRevoked = newInviteLink.IsRevoked,
                LinkGuid = newInviteLink.LinkGuid.ToString(),
                CreatedBy = (await userManager.FindByIdAsync(newInviteLink.CreatedBy.ToString())).UserName,
                ModifiedBy = (await userManager.FindByIdAsync(newInviteLink.ModifiedBy.ToString())).UserName,
                CreatedOn = newInviteLink.CreatedOn,
                ModifiedOn = newInviteLink.ModifiedOn
            };

            response.Message = "Invite link created successfully!";
            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        [Route("join")]
        public async Task<IActionResult> JoinTeam(string guid)
        {
            var response = new ResponseModel();

            Guid linkGuid = new Guid(guid);

            Guid userId = LoggedInUserId.Value;

            var selectedTeamLink = await vwsDbContext.GetTeamInviteLinkByLinkGuidAsync(linkGuid);

            if (selectedTeamLink == null || selectedTeamLink.Team.IsDeleted || selectedTeamLink.IsRevoked)
            {
                response.Message = "Invalid link";
                response.AddError(localizer["Link is not valid."]);
                return StatusCode(StatusCodes.Status406NotAcceptable, response);
            }

            if ((await vwsDbContext.GetTeamMemberAsync(selectedTeamLink.TeamId, userId)) != null)
            {
                response.Message = "User already joined";
                response.AddError(localizer["You are already joined the team."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var newTeamMember = new TeamMember()
            {
                TeamId = selectedTeamLink.TeamId,
                CreatedOn = DateTime.Now,
                UserProfileId = userId
            };

            await vwsDbContext.AddTeamMemberAsync(newTeamMember);

            vwsDbContext.Save();

            response.Message = "User added to team successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getLinks")]
        public async Task<IEnumerable<TeamInviteLinkResponseModel>> GetInviteLinks()
        {
            Guid userId = LoggedInUserId.Value;

            List<TeamInviteLinkResponseModel> response = new List<TeamInviteLinkResponseModel>();

            var userTeamInviteLinks = vwsDbContext.TeamInviteLinks.Include(teamInviteLink => teamInviteLink.Team)
                                                                  .Where(teamInviteLink => teamInviteLink.CreatedBy == userId &&
                                                                            teamInviteLink.IsRevoked == false && 
                                                                            teamInviteLink.Team.IsDeleted == false);

            var teamMembers = vwsDbContext.TeamMembers.Where(teamMemeber => teamMemeber.UserProfileId == userId && teamMemeber.HasUserLeft == false);

            foreach (var userTeamInviteLink in userTeamInviteLinks)
            {
                if (teamMembers.Any(teamMember => teamMember.TeamId == userTeamInviteLink.TeamId))
                {
                    response.Add(new TeamInviteLinkResponseModel()
                    {
                        Id = userTeamInviteLink.Id,
                        TeamName = (await vwsDbContext.GetTeamAsync(userTeamInviteLink.TeamId)).Name,
                        IsRevoked = userTeamInviteLink.IsRevoked,
                        LinkGuid = userTeamInviteLink.LinkGuid.ToString(),
                        CreatedBy = (await userManager.FindByIdAsync(userTeamInviteLink.CreatedBy.ToString())).UserName,
                        ModifiedBy = (await userManager.FindByIdAsync(userTeamInviteLink.ModifiedBy.ToString())).UserName,
                        CreatedOn = userTeamInviteLink.CreatedOn,
                        ModifiedOn = userTeamInviteLink.ModifiedOn
                    });
                }
            }
            return response;
        }

        [HttpGet]
        [Authorize]
        [Route("getAll")]
        public async Task<IEnumerable<TeamResponseModel>> GetAllTeams()
        {
            Guid userId = LoggedInUserId.Value;

            List<TeamResponseModel> response = new List<TeamResponseModel>();

            var userTeams = vwsDbContext.GetUserTeams(userId);

            foreach (var userTeam in userTeams)
            {
                response.Add(new TeamResponseModel()
                {
                    Id = userTeam.Id,
                    TeamTypeId = userTeam.TeamTypeId,
                    Name = userTeam.Name,
                    Description = userTeam.Description,
                    Color = userTeam.Color,
                    CreatedBy = (await userManager.FindByIdAsync(userTeam.CreatedBy.ToString())).UserName,
                    ModifiedBy = (await userManager.FindByIdAsync(userTeam.ModifiedBy.ToString())).UserName,
                    CreatedOn = userTeam.CreatedOn,
                    ModifiedOn = userTeam.ModifiedOn,
                    Guid = userTeam.Guid,
                    TeamImageId = userTeam.TeamImageId
                });
            }
            return response;
        }

        [HttpPut]
        [Authorize]
        [Route("revokeLink")]
        public async Task<IActionResult> RevokeLink(int id)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedInviteLink = await vwsDbContext.GetTeamInviteLinkByIdAsync(id);

            if (selectedInviteLink == null || selectedInviteLink.Team.IsDeleted || selectedInviteLink.IsRevoked)
            {
                response.Message = "Link not found";
                response.AddError(localizer["Link does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (selectedInviteLink.CreatedBy != userId || !vwsDbContext.TeamMembers.Any(teamMember => teamMember.UserProfileId == userId && teamMember.HasUserLeft == false))
            {
                response.Message = "Team access forbidden";
                response.AddError(localizer["You don't have access to this team."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedInviteLink.IsRevoked = true;

            vwsDbContext.Save();

            response.Message = "Task updated successfully!";
            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        [Route("isNameOfGroupUsed")]
        public bool IsNameOfGroupUsed(string name)
        {
            Guid userId = LoggedInUserId.Value;

            return vwsDbContext.TeamMembers.Any(teamMember => teamMember.UserProfileId == userId &&
                                                teamMember.Team.Name == name &&
                                                teamMember.Team.IsDeleted == false &&
                                                teamMember.HasUserLeft == false);
        }

        [HttpPut]
        [Authorize]
        [Route("updateTeam")]
        public async Task<IActionResult> UpdateTeam([FromBody] UpdateTeamModel model)
        {
            var response = new ResponseModel<TeamResponseModel>();

            if (!String.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
            {
                response.Message = "Team model data has problem.";
                response.AddError(localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Name.Length > 500)
            {
                response.Message = "Team model data has problem.";
                response.AddError(localizer["Length of title is more than 500 characters."]);
            }
            if (!String.IsNullOrEmpty(model.Color) && model.Color.Length > 6)
            {
                response.Message = "Team model data has problem.";
                response.AddError(localizer["Length of color is more than 6 characters."]);
            }

            if (response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            Guid userId = LoggedInUserId.Value;

            var selectedTeam = await vwsDbContext.GetTeamAsync(model.Id);
            if(selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.Message = "Team not found";
                response.AddError(localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            var selectedTeamMember = await vwsDbContext.GetTeamMemberAsync(model.Id, userId);
            if(selectedTeamMember == null)
            {
                response.Message = "Access team is forbidden";
                response.AddError(localizer["You are not a member of team."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedTeam.Color = model.Color;
            selectedTeam.Description = model.Description;
            selectedTeam.ModifiedBy = userId;
            selectedTeam.ModifiedOn = DateTime.Now;
            selectedTeam.Name = model.Name;
            vwsDbContext.Save();

            var updatedTeamResponse = new TeamResponseModel()
            {
                Id = selectedTeam.Id,
                TeamTypeId = selectedTeam.TeamTypeId,
                Name = selectedTeam.Name,
                Description = selectedTeam.Description,
                Color = selectedTeam.Color,
                CreatedBy = (await userManager.FindByIdAsync(selectedTeam.CreatedBy.ToString())).UserName,
                ModifiedBy = (await userManager.FindByIdAsync(selectedTeam.ModifiedBy.ToString())).UserName,
                CreatedOn = selectedTeam.CreatedOn,
                ModifiedOn = selectedTeam.ModifiedOn,
                Guid = selectedTeam.Guid,
                TeamImageId = selectedTeam.TeamImageId
            };

            response.Message = "Team updated successfully";
            response.Value = updatedTeamResponse;
            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        [Route("uploadTeamImage")]
        public async Task<IActionResult> UploadTeamImage(IFormFile image, int teamId)
        {
            var response = new ResponseModel();

            string[] types = { "png", "jpg", "jpeg" };

            var files = Request.Form.Files.ToList();

            Guid userId = LoggedInUserId.Value;

            if (files.Count > 1)
            {
                response.AddError(localizer["There is more than one file."]);
                response.Message = "Too many files passed";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (files.Count == 0 && image == null)
            {
                response.AddError(localizer["You did not upload an image."]);
                response.Message = "There is no image";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            var uploadedImage = files.Count == 0 ? image : files[0];

            var selectedTeam = await vwsDbContext.GetTeamAsync(teamId);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.AddError(localizer["There is no team with given Id."]);
                response.Message = "Team not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeamMember = await vwsDbContext.GetTeamMemberAsync(teamId, userId);
            if (selectedTeamMember == null)
            {
                response.AddError(localizer["You are not a member of team."]);
                response.Message = "Not member of team";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            ResponseModel<File> fileResponse;

            if (selectedTeam.TeamImage != null)
            {
                fileResponse = await fileManager.WriteFile(uploadedImage, userId, "profileImages", (int)selectedTeam.TeamImageId, types.ToList());
                if (fileResponse.HasError)
                {
                    foreach (var error in fileResponse.Errors)
                        response.AddError(localizer[error]);
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
                await vwsDbContext.AddFileContainerAsync(newFileContainer);
                vwsDbContext.Save();
                fileResponse = await fileManager.WriteFile(uploadedImage, userId, "profileImages", newFileContainer.Id, types.ToList());
                if (fileResponse.HasError)
                {
                    foreach (var error in fileResponse.Errors)
                        response.AddError(localizer[error]);
                    response.Message = "Error in writing file";
                    vwsDbContext.DeleteFileContainer(newFileContainer);
                    vwsDbContext.Save();
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
                newFileContainer.RecentFileId = fileResponse.Value.Id;
                selectedTeam.TeamImageId = newFileContainer.Id;
            }
            selectedTeam.ModifiedBy = LoggedInUserId.Value;
            selectedTeam.ModifiedOn = DateTime.Now;
            vwsDbContext.Save();
            response.Message = "Team image added successfully!";
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("delete")]
        public async Task<IActionResult> DeleteTeam(int teamId)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedTeam = await vwsDbContext.GetTeamAsync(teamId);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.AddError(localizer["There is no team with given Id."]);
                response.Message = "Team not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeamMember = await vwsDbContext.GetTeamMemberAsync(teamId, userId);
            if (selectedTeamMember == null)
            {
                response.AddError(localizer["You are not a member of team."]);
                response.Message = "Not member of team";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var deletionTime = DateTime.Now;

            selectedTeam.IsDeleted = true;
            selectedTeam.ModifiedBy = userId;
            selectedTeam.ModifiedOn = deletionTime;

            var teamProjects = vwsDbContext.Projects.Where(project => project.TeamId == teamId &&
                                                                      !project.IsDeleted);

            var teamDepartments = vwsDbContext.Departments.Where(department => department.TeamId == teamId &&
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

            vwsDbContext.Save();

            response.Message = "Team deleted successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getTeammates")]
        public async Task<IActionResult> GetTeammates(int id)
        {
            var response = new ResponseModel<List<UserModel>>();
            var teammatesList = new List<UserModel>();

            var selectedTeam = await vwsDbContext.GetTeamAsync(id);
            var userId = LoggedInUserId.Value;

            if(selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.Message = "Team not found";
                response.AddError(localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeamMember = await vwsDbContext.GetTeamMemberAsync(id, userId);
            if (selectedTeamMember == null)
            {
                response.Message = "Access team is forbidden";
                response.AddError(localizer["You are not a member of team."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            List<UserProfile> userTeamMates = vwsDbContext.TeamMembers
                .Include(teamMember => teamMember.UserProfile)
                .Where(teamMember => teamMember.TeamId == id && teamMember.HasUserLeft == false)
                .Select(teamMember => teamMember.UserProfile).Distinct().ToList();

            foreach(var teamMate in userTeamMates)
            {
                var userName = (await userManager.FindByIdAsync(teamMate.UserId.ToString())).UserName;
                teammatesList.Add(new UserModel()
                {
                    UserName = userName,
                    UserId = teamMate.UserId,
                    ProfileImageId = teamMate.ProfileImageId
                });
            }

            response.Message = "Team mates are given successfully!";
            response.Value = teammatesList;
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public async Task<IActionResult> GetTeam(int id)
        {
            Guid userId = LoggedInUserId.Value;

            var response = new ResponseModel<TeamResponseModel>();

            var selectedTeam = await vwsDbContext.GetTeamAsync(id);

            if(selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.AddError(localizer["There is no team with given Id."]);
                response.Message = "Team not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var teamMember = vwsDbContext.GetTeamMemberAsync(id, userId);
            if(teamMember == null)
            {
                response.AddError(localizer["You are not a member of team."]);
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
                CreatedBy = (await userManager.FindByIdAsync(selectedTeam.CreatedBy.ToString())).UserName,
                Guid = selectedTeam.Guid,
                ModifiedBy = (await userManager.FindByIdAsync(selectedTeam.ModifiedBy.ToString())).UserName,
                CreatedOn = selectedTeam.CreatedOn,
                ModifiedOn = selectedTeam.ModifiedOn,
                TeamImageId = selectedTeam.TeamImageId
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

            var selectedTeam = await vwsDbContext.GetTeamAsync(id);

            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.Message = "Team not found";
                response.AddError(localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeamMember = await vwsDbContext.GetTeamMemberAsync(id, userId);
            if (selectedTeamMember == null)
            {
                response.Message = "Access team is forbidden";
                response.AddError(localizer["You are not a member of team."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var teamDepartments = vwsDbContext.Departments.Where(department => department.TeamId == id && !department.IsDeleted);

            foreach(var teamDepartment in teamDepartments)
                departments.Add(new DepartmentResponseModel()
                {
                    Id = teamDepartment.Id,
                    Name = teamDepartment.Name,
                    DepartmentImageId = teamDepartment.DepartmentImageId,
                    Description = teamDepartment.Description,
                    Color = teamDepartment.Color,
                    CreatedBy = (await userManager.FindByIdAsync(teamDepartment.CreatedBy.ToString())).UserName,
                    CreatedOn = teamDepartment.CreatedOn,
                    Guid = teamDepartment.Guid,
                    ModifiedBy = (await userManager.FindByIdAsync(teamDepartment.ModifiedBy.ToString())).UserName,
                    ModifiedOn = teamDepartment.ModifiedOn,
                    TeamId = teamDepartment.TeamId
                });

            response.Value = departments;
            response.Message = "Team departments returned successfully!";
            return Ok(response);
        }
    }
}
