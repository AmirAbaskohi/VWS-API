using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._department;
using vws.web.Domain._project;
using vws.web.Models;
using vws.web.Models._department;

namespace vws.web.Controllers._department
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class DepartmentController : BaseController
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IConfiguration configuration;
        private readonly IStringLocalizer<DepartmentController> localizer;
        private readonly IVWS_DbContext vwsDbContext;

        public DepartmentController(UserManager<ApplicationUser> _userManager, RoleManager<IdentityRole> _roleManager,
            SignInManager<ApplicationUser> _signInManager, IStringLocalizer<DepartmentController> _localizer,
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
        public async Task<IActionResult> CreateDepartment([FromBody] DepartmentModel model)
        {
            var response = new ResponseModel<DepartmentResponseModel>();

            if (!String.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
            {
                response.Message = "Department model data has problem.";
                response.AddError(localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Name.Length > 500)
            {
                response.Message = "Department model data has problem.";
                response.AddError(localizer["Length of name is more than 500 characters."]);
            }
            if (!String.IsNullOrEmpty(model.Color) && model.Color.Length > 6)
            {
                response.Message = "Department model data has problem.";
                response.AddError(localizer["Length of color is more than 6 characters."]);
            }
            if (response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            var selectedTeam = await vwsDbContext.GetTeamAsync(model.TeamId);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.AddError(localizer["There is no team with such id."]);
                response.Message = "Team not found";
                return StatusCode(StatusCodes.Status404NotFound, response);
            }

            var userId = LoggedInUserId.Value;

            var selectedTeamMember = await vwsDbContext.GetTeamMemberAsync(model.TeamId, userId);
            if (selectedTeamMember == null)
            {
                response.AddError(localizer["You are not member of team."]);
                response.Message = "Team access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var departmentNames = vwsDbContext.Departments.Where(department => department.TeamId == model.TeamId && department.IsDeleted).Select(department => department.Name);

            if (departmentNames.Contains(model.Name))
            {
                response.AddError(localizer["There is already a department with given name, in given team."]);
                response.Message = "Name of department is used";
                return StatusCode(StatusCodes.Status208AlreadyReported, response);
            }

            var newDepartment = new Department()
            {
                Name = model.Name,
                Description = model.Description,
                IsDeleted = false,
                Color = model.Color,
                TeamId = model.TeamId,
                Guid = Guid.NewGuid()
            };

            await vwsDbContext.AddDepartmentAsync(newDepartment);
            vwsDbContext.Save();

            var newDepartmentMember = new DepartmentMember()
            {
                IsDeleted = false,
                CreatedOn = DateTime.Now,
                UserProfileId = userId,
                DepartmentId = newDepartment.Id
            };

            await vwsDbContext.AddDepartmentMemberAsync(newDepartmentMember);
            vwsDbContext.Save();

            var departmentResponse = new DepartmentResponseModel()
            {
                IsDeleted = newDepartment.IsDeleted,
                Id = newDepartment.Id,
                Color = newDepartment.Color,
                Name = newDepartment.Name,
                TeamId = newDepartment.TeamId
            };

            response.Value = departmentResponse;
            response.Message = "Department added successfully";
            return Ok(response);
        }
    }
}
