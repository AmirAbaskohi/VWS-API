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
using vws.web.Models._task;
using vws.web.Repositories;

namespace vws.web.Controllers._task
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class TaskController : BaseController
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

        [HttpPost]
        [Authorize]
        [Route("create")]
        public async Task<IActionResult> CreateTask([FromBody] TaskModel model)
        {
            var response = new ResponseModel();

            if (!String.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
            {
                response.Message = "Task model data has problem.";
                response.AddError(localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Title.Length > 500)
            {
                response.Message = "Task model data has problem.";
                response.AddError(localizer["Length of title is more than 500 characters."]);
            }
            if (model.StartDate.HasValue && model.EndDate.HasValue)
            {
                if (model.StartDate > model.EndDate)
                {
                    response.Message = "Task model data has problem.";
                    response.AddError(localizer["Start Date should be before End Date."]);
                }
            }

            if (response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            Guid userId = LoggedInUserId.Value;

            DateTime creationTime = DateTime.Now;

            var newTask = new GeneralTask()
            {
                Title = model.Title,
                Description = model.Description ?? String.Empty,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                CreatedBy = userId,
                ModifiedBy = userId,
                CreatedOn = creationTime,
                ModifiedOn = creationTime,
                Guid = Guid.NewGuid()
            };

            await vwsDbContext.AddTaskAsync(newTask);
            vwsDbContext.Save();

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
                response.Message = "Task model data has problem";
                response.AddError(localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Title.Length > 500)
            {
                response.Message = "Task model data has problem";
                response.AddError(localizer["Length of title is more than 500 characters."]);
            }
            if (model.StartDate.HasValue && model.EndDate.HasValue)
            {
                if (model.StartDate > model.EndDate)
                {
                    response.Message = "Task model data has problem";
                    response.AddError(localizer["Start Date should be before End Date."]);
                }
            }

            if (response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            string userEmail = User.Claims.First(claim => claim.Type == "UserEmail").Value;
            Guid userId = LoggedInUserId.Value;

            var selectedTask = await vwsDbContext.GetTaskAsync(model.TaskId);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status404NotFound, response);
            }
            if (selectedTask.CreatedBy != userId)
            {
                response.Message = "Task access forbidden";
                response.AddError(localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (model.StartDate.HasValue && !(model.EndDate.HasValue) && selectedTask.EndDate.HasValue)
            {
                if (model.StartDate > selectedTask.EndDate)
                {
                    response.Message = "Task model data has problem";
                    response.AddError(localizer["Start Date should be before End Date."]);
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
            }
            if (model.EndDate.HasValue && !(model.StartDate.HasValue) && selectedTask.StartDate.HasValue)
            {
                if (model.EndDate < selectedTask.StartDate)
                {
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

            response.Message = "Task updated successfully!";
            return Ok(response);

        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public async Task<IEnumerable<TaskResponseModel>> GetTasks()
        {
            Guid userId = LoggedInUserId.Value;

            List<TaskResponseModel> response = new List<TaskResponseModel>();

            var userTasks = vwsDbContext.GeneralTasks.Where(task => task.CreatedBy == userId);
            foreach (var userTask in userTasks)
            {
                if (userTask.IsDeleted || userTask.IsArchived)
                    continue;

                response.Add(new TaskResponseModel()
                {
                    Id = userTask.Id,
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

        [HttpGet]
        [Authorize]
        [Route("getArchived")]
        public async Task<IEnumerable<TaskResponseModel>> GetArchivedTasks()
        {
            Guid userId = LoggedInUserId.Value;

            List<TaskResponseModel> response = new List<TaskResponseModel>();

            var userTasks = vwsDbContext.GeneralTasks.Where(task => task.CreatedBy == userId);
            foreach (var userTask in userTasks)
            {
                if (userTask.IsArchived && !userTask.IsDeleted)
                {
                    response.Add(new TaskResponseModel()
                    {
                        Id = userTask.Id,
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
            }
            return response;
        }

        [HttpDelete]
        [Authorize]
        [Route("delete")]
        public async Task<IActionResult> DeleteTask(long taskId)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedTask = await vwsDbContext.GetTaskAsync(taskId);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status404NotFound, response);
            }
            if (selectedTask.CreatedBy != userId)
            {
                response.Message = "Task access forbidden";
                response.AddError(localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedTask.IsDeleted = true;

            vwsDbContext.Save();

            response.Message = "Task updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("archive")]
        public async Task<IActionResult> ArchiveTask(long taskId)
        {
            var response = new ResponseModel();

            Guid userId = LoggedInUserId.Value;

            var selectedTask = await vwsDbContext.GetTaskAsync(taskId);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status404NotFound, response);
            }
            if (selectedTask.CreatedBy != userId)
            {
                response.Message = "Task access forbidden";
                response.AddError(localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedTask.IsArchived = true;

            vwsDbContext.Save();

            response.Message = "Task updated successfully!";
            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        [Route("assignTask")]
        public async Task<IActionResult> AssignTask([FromBody] AssignTaskModel model)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();
            var selectedUserId = new Guid(model.UserId);

            var selectedTask = await vwsDbContext.GetTaskAsync(model.TaskId);
            if(selectedTask == null || selectedTask.IsDeleted)
            {
                response.AddError(localizer["Task does not exist."]);
                response.Message = "Task not found";
                return StatusCode(StatusCodes.Status404NotFound, response);
            }

            if(selectedTask.CreatedBy != userId)
            {
                response.AddError(localizer["You don't have access to this task."]);
                response.Message = "Task access forbidden";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if(!vwsDbContext.UserProfiles.Any(profile => profile.UserId == selectedUserId))
            {
                response.AddError(localizer["User does not exist."]);
                response.Message = "User not found";
                return StatusCode(StatusCodes.Status404NotFound, response);
            }

            if(vwsDbContext.TaskAssigns.Any(taskAssign => taskAssign.UserProfileId == selectedUserId &&
                                             taskAssign.GeneralTaskId == model.TaskId &&
                                             taskAssign.IsDeleted == false))
            {
                response.AddError(localizer["Task is assigned to user before."]);
                response.Message = "Task assigned before";
                return StatusCode(StatusCodes.Status208AlreadyReported, response);
            }

            var newTaskAssign = new TaskAssign()
            {
                Guid = Guid.NewGuid(),
                GeneralTaskId = model.TaskId,
                UserProfileId = selectedUserId,
                IsDeleted = false,
                CreatedBy = userId,
                CreatedOn = DateTime.Now
            };

            await vwsDbContext.AddTaskAssignAsync(newTaskAssign);
            vwsDbContext.Save();

            response.Message = "Task assigned successfully!";
            return Ok(response);
        }
    }
}
