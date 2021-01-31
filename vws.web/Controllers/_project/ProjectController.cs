﻿using System;
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

            DateTime creationTime = DateTime.Now;

            var newProject = new Project()
            {
                Name = model.Name,
                StatusId = 1,
                Description = model.Description,
                Color = model.Color,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Guid = Guid.NewGuid(),
                IsDelete = false
            };

            await vwsDbContext.AddProjectAsync(newProject);
            vwsDbContext.Save();

            var newProjectMember = new ProjectMember()
            {
                CreatedOn = creationTime,
                ProjectId = newProject.Id,
                UserProfileId = userId
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
                IsDelete = newProject.IsDelete
            };

            response.Value = newProjectResponse;
            response.Message = "Project created successfully!";
            return Ok(response);
        }
    }
}
