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
using vws.web.Models;
using vws.web.Repositories;

namespace vws.web.Controllers
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
            var response = new ResponseModel();

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

            if (response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            Guid userId = LoggedInUserId.Value;

            DateTime creationTime = DateTime.Now;

            var newTeam = new Team()
            {
                Name = model.Name,
                TeamTypeId = model.TeamTypeId,
                Description = model.Description,
                Color = model.Color,
                CreatedOn = creationTime,
                CreatedBy = userId,
                ModifiedOn = creationTime,
                ModifiedBy = userId
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
            if(selectedTeam == null)
            {
                response.Message = "Team not found";
                response.AddError(localizer["There is no team with given Id."]);
                return StatusCode(StatusCodes.Status404NotFound, response);
            }

            Guid userId = LoggedInUserId.Value;

            var teamMember = await vwsDbContext.GetTeamMemberAsync(teamId, userId);
            if(teamMember == null)
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
                IsInvoked = false
            };

            await vwsDbContext.AddTeamInviteLinkAsync(newInviteLink);
            vwsDbContext.Save();

            response.Value = inviteLinkGuid.ToString();
            response.Message = "Invite link created successfully!";
            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        [Route("joinTeam")]
        public async Task<IActionResult> JoinTeam(string guid)
        {
            var response = new ResponseModel();

            Guid linkGuid = new Guid(guid);

            Guid userId = LoggedInUserId.Value;

            var selectedTeamLink = await vwsDbContext.GetTeamInviteLink(linkGuid);

            if(selectedTeamLink == null)
            {
                response.Message = "Unvalid link";
                response.AddError(localizer["Link is not valid."]);
                return StatusCode(StatusCodes.Status406NotAcceptable, response);
            }

            if((await vwsDbContext.GetTeamMemberAsync(selectedTeamLink.TeamId, userId)) != null)
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
    }
}
