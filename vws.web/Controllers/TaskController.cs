using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using vws.web.Domain._task;
using vws.web.Models;
using vws.web.Repositories;

namespace vws.web.Controllers
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IEmailSender emailSender;
        private readonly IConfiguration configuration;
        private readonly IPasswordHasher<ApplicationUser> passwordHasher;
        private readonly IStringLocalizer<TaskController> localizer;
        private readonly IVWS_DbContext vwsDbContext;

        public TaskController(UserManager<ApplicationUser> _userManager, RoleManager<IdentityRole> _roleManager,
            SignInManager<ApplicationUser> _signInManager, IConfiguration _configuration, IEmailSender _emailSender,
            IPasswordHasher<ApplicationUser> _passwordHasher, IStringLocalizer<TaskController> _localizer,
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

        private async Task<Guid> GetUserId(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            return new Guid(user.Id);
        }

        [HttpPost]
        [Authorize]
        [Route("create")]
        public async Task<IActionResult> CreateTask([FromBody] TaskModel model)
        {
            var response = new ResponseModel();

            if(model.Description.Length > 2000)
            {
                response.Status = "Error";
                response.Message = "Task model data has problem.";
                response.AddError(localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Title.Length > 500)
            {
                response.Status = "Error";
                response.Message = "Task model data has problem.";
                response.AddError(localizer["Length of title is more than 500 characters."]);
            }
            if(model.StartDate.HasValue && model.EndDate.HasValue)
            {
                if(model.StartDate > model.EndDate)
                {
                    response.Status = "Error";
                    response.Message = "Task model data has problem.";
                    response.AddError(localizer["Start Date should be before End Date."]);
                }
            }

            if(response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            string userEmail = User.Claims.First(claim => claim.Type == "UserEmail").Value;
            Guid userId = await GetUserId(userEmail);

            var newTask = new GeneralTask()
            {
                Title = model.Title,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                CreatedBy = userId,
                ModifiedBy = userId,
                CreatedOn = DateTime.Now,
                ModifiedOn = DateTime.Now
            };

            vwsDbContext.AddTask(newTask);
            vwsDbContext.Save();

            response.Status = "Success";
            response.Message = "Task created successfully!";
            return Ok(response);

        }

        [HttpPut]
        [Authorize]
        [Route("update")]
        public async Task<IActionResult> UpdateTask([FromBody] UpdateTaskModel model)
        {
            var response = new ResponseModel();

            if (model.Description.Length > 2000)
            {
                response.Status = "Error";
                response.Message = "Task model data has problem";
                response.AddError(localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Title.Length > 500)
            {
                response.Status = "Error";
                response.Message = "Task model data has problem";
                response.AddError(localizer["Length of title is more than 500 characters."]);
            }
            if (model.StartDate.HasValue && model.EndDate.HasValue)
            {
                if (model.StartDate > model.EndDate)
                {
                    response.Status = "Error";
                    response.Message = "Task model data has problem";
                    response.AddError(localizer["Start Date should be before End Date."]);
                }
            }

            if (response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            string userEmail = User.Claims.First(claim => claim.Type == "UserEmail").Value;
            Guid userId = await GetUserId(userEmail);

            var selectedTask = vwsDbContext.GeneralTasks.FirstOrDefault(task => task.Id == model.TaskId);

            if(selectedTask == null)
            {
                response.Status = "Error";
                response.Message = "Task not found";
                response.AddError(localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status404NotFound, response);
            }
            if(selectedTask.CreatedBy != userId)
            {
                response.Status = "Error";
                response.Message = "Task access forbidden";
                response.AddError(localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if(model.StartDate.HasValue && !(model.EndDate.HasValue) && selectedTask.EndDate.HasValue)
            {
                if(model.StartDate > selectedTask.EndDate)
                {
                    response.Status = "Error";
                    response.Message = "Task model data has problem";
                    response.AddError(localizer["Start Date should be before End Date."]);
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
            }
            if (model.EndDate.HasValue && !(model.StartDate.HasValue) && selectedTask.StartDate.HasValue)
            {
                if (model.EndDate < selectedTask.StartDate)
                {
                    response.Status = "Error";
                    response.Message = "Task model data has problem";
                    response.AddError(localizer["Start Date should be before End Date."]);
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
            }

            selectedTask.StartDate = model.StartDate;
            selectedTask.EndDate = model.EndDate;
            selectedTask.ModifiedBy = userId;
            selectedTask.ModifiedOn = DateTime.Now;
            selectedTask.Title = model.Title;
            selectedTask.Description = model.Description;

            vwsDbContext.Save();

            response.Status = "Success";
            response.Message = "Task updated successfully!";
            return Ok(response);

        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public async Task<IEnumerable<TaskResponseModel>> GetTasks()
        {
            string userEmail = User.Claims.First(claim => claim.Type == "UserEmail").Value;
            Guid userId = await GetUserId(userEmail);

            List<TaskResponseModel> response = new List<TaskResponseModel>();

            var userTasks = vwsDbContext.GeneralTasks.Where(task => task.CreatedBy == userId);
            foreach (var userTask in userTasks)
            {
                if (userTask.IsDeleted || userTask.IsArchived)
                    continue;

                response.Add(new TaskResponseModel()
                {
                    Title = userTask.Title,
                    Description = userTask.Description,
                    StartDate = userTask.StartDate,
                    EndDate = userTask.EndDate,
                    CreatedOn = userTask.CreatedOn,
                    ModifiedOn = userTask.ModifiedOn,
                    CreatedBy = (await userManager.FindByIdAsync(userTask.CreatedBy.ToString())).UserName,
                    ModifiedBy = (await userManager.FindByIdAsync(userTask.ModifiedBy.ToString())).UserName
                });
            }
            return response;
        }

        [HttpDelete]
        [Authorize]
        [Route("delete")]
        public async Task<IActionResult> DeleteTask(long taskId)
        {
            var response = new ResponseModel();

            string userEmail = User.Claims.First(claim => claim.Type == "UserEmail").Value;
            Guid userId = await GetUserId(userEmail);

            var selectedTask = vwsDbContext.GeneralTasks.FirstOrDefault(task => task.Id == taskId);

            if (selectedTask == null)
            {
                response.Status = "Error";
                response.Message = "Task not found";
                response.AddError(localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status404NotFound, response);
            }
            if (selectedTask.CreatedBy != userId)
            {
                response.Status = "Error";
                response.Message = "Task access forbidden";
                response.AddError(localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedTask.IsDeleted = true;
            
            vwsDbContext.Save();

            response.Status = "Success";
            response.Message = "Task updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("archive")]
        public async Task<IActionResult> ArchiveTask(long taskId)
        {
            var response = new ResponseModel();

            string userEmail = User.Claims.First(claim => claim.Type == "UserEmail").Value;
            Guid userId = await GetUserId(userEmail);

            var selectedTask = vwsDbContext.GeneralTasks.FirstOrDefault(task => task.Id == taskId);

            if (selectedTask == null)
            {
                response.Status = "Error";
                response.Message = "Task not found";
                response.AddError(localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status404NotFound, response);
            }
            if (selectedTask.CreatedBy != userId)
            {
                response.Status = "Error";
                response.Message = "Task access forbidden";
                response.AddError(localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedTask.IsArchived = true;

            vwsDbContext.Save();

            response.Status = "Success";
            response.Message = "Task updated successfully!";
            return Ok(response);
        }
    }
}
