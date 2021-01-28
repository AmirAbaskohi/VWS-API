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

        public TeamController(UserManager<ApplicationUser> _userManager, RoleManager<IdentityRole> _roleManager,
            SignInManager<ApplicationUser> _signInManager, IConfiguration _configuration, IEmailSender _emailSender,
            IPasswordHasher<ApplicationUser> _passwordHasher, IStringLocalizer<TeamController> _localizer,
            IVWS_DbContext _vwsDbContext)
        {
            userManager = _userManager;
            roleManager = _roleManager;
            signInManager = _signInManager;
            configuration = _configuration;
            emailSender = _emailSender;
            passwordHasher = _passwordHasher;
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
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

            var hasTeamWithSameName = vwsDbContext.TeamMembers.Any(teamMember => teamMember.UserProfileId == userId && teamMember.Team.Name == model.Name);
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
                TeamTypeId = 1,
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
                ModifiedOn = newTeam.ModifiedOn
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
            var response = new ResponseModel<string>();

            var selectedTeam = await vwsDbContext.GetTeamAsync(teamId);
            if (selectedTeam == null)
            {
                response.Message = "Team not found";
                response.AddError(localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status404NotFound, response);
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

            response.Value = inviteLinkGuid.ToString();
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

            if (selectedTeamLink == null || selectedTeamLink.IsRevoked == true)
            {
                response.Message = "Unvalid link";
                response.AddError(localizer["Link is not valid."]);
                return StatusCode(StatusCodes.Status406NotAcceptable, response);
            }

            if ((await vwsDbContext.GetTeamMemberAsync(selectedTeamLink.TeamId, userId)) != null)
            {
                response.Message = "User already joined";
                response.AddError(localizer["You are already joined the team."]);
                return StatusCode(StatusCodes.Status208AlreadyReported, response);
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

            var userTeamInviteLinks = vwsDbContext.TeamInviteLinks.Where(teamInviteLink => teamInviteLink.CreatedBy == userId);
            foreach (var userTeamInviteLink in userTeamInviteLinks)
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
            return response;
        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public async Task<IEnumerable<TeamResponseModel>> GetTeam()
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
                    ModifiedOn = userTeam.ModifiedOn
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

            if (selectedInviteLink == null)
            {
                response.Message = "Link not found";
                response.AddError(localizer["Link does not exist."]);
                return StatusCode(StatusCodes.Status404NotFound, response);
            }
            if (selectedInviteLink.CreatedBy != userId)
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

            return vwsDbContext.TeamMembers.Any(teamMember => teamMember.UserProfileId == userId && teamMember.Team.Name == name);
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
            if(selectedTeam == null)
            {
                response.Message = "Team not found";
                response.AddError(localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status404NotFound, response);
            }
            var selectedTeamMember = await vwsDbContext.GetTeamMemberAsync(model.Id, userId);
            if(selectedTeam == null)
            {
                response.Message = "Team not found";
                response.AddError(localizer["You are not a member of team."]);
                return StatusCode(StatusCodes.Status404NotFound, response);
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
                ModifiedOn = selectedTeam.ModifiedOn
            };

            response.Message = "Team updated successfully";
            response.Value = updatedTeamResponse;
            return Ok(response);
        }
    }
}
