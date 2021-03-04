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
using vws.web.Domain._task;
using vws.web.Enums;
using vws.web.Models;
using vws.web.Models._task;

namespace vws.web.Controllers._task
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class TaskController : BaseController
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IStringLocalizer<TaskController> localizer;
        private readonly IVWS_DbContext vwsDbContext;

        public TaskController(UserManager<ApplicationUser> _userManager, IStringLocalizer<TaskController> _localizer,
            IVWS_DbContext _vwsDbContext)
        {
            userManager = _userManager;
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
        }

        [HttpPost]
        [Authorize]
        [Route("create")]
        public async Task<IActionResult> CreateTask([FromBody] TaskModel model)
        {
            var response = new ResponseModel<TaskResponseModel>();

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
            if (!Enum.IsDefined(typeof(SeedDataEnum.TaskPriority), model.PriorityId))
            {
                response.Message = "Task model data has problem.";
                response.AddError(localizer["Priority id is not defined."]);
            }

            if (response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            Guid userId = LoggedInUserId.Value;

            DateTime creationTime = DateTime.Now;

            var newTask = new GeneralTask()
            {
                Title = model.Title,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                CreatedBy = userId,
                ModifiedBy = userId,
                CreatedOn = creationTime,
                ModifiedOn = creationTime,
                Guid = Guid.NewGuid(),
                TaskPriorityId = model.PriorityId
            };

            await vwsDbContext.AddTaskAsync(newTask);
            vwsDbContext.Save();

            var newTaskResponseModel = new TaskResponseModel()
            {
                Id = newTask.Id,
                Title = newTask.Title,
                CreatedOn = newTask.CreatedOn,
                Description = newTask.Description,
                EndDate = newTask.EndDate,
                StartDate = newTask.StartDate,
                Guid = newTask.Guid,
                ModifiedOn = newTask.ModifiedOn,
                ModifiedBy = (await userManager.FindByIdAsync(newTask.ModifiedBy.ToString())).UserName,
                CreatedBy = (await userManager.FindByIdAsync(newTask.CreatedBy.ToString())).UserName,
                PriorityId = newTask.TaskPriorityId,
                PriorityTitle = localizer[((SeedDataEnum.TaskPriority)newTask.TaskPriorityId).ToString()]
            };

            response.Value = newTaskResponseModel;
            response.Message = "Task created successfully!";
            return Ok(response);

        }

        [HttpPut]
        [Authorize]
        [Route("update")]
        public async Task<IActionResult> UpdateTask([FromBody] UpdateTaskModel model)
        {
            var response = new ResponseModel<TaskResponseModel>();

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
            if (!Enum.IsDefined(typeof(SeedDataEnum.TaskPriority), model.PriorityId))
            {
                response.Message = "Task model data has problem.";
                response.AddError(localizer["Priority id is not defined."]);
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
                return StatusCode(StatusCodes.Status400BadRequest, response);
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
            selectedTask.TaskPriorityId = model.PriorityId;

            vwsDbContext.Save();

            var updatedTaskResponseModel = new TaskResponseModel()
            {
                Id = selectedTask.Id,
                Title = selectedTask.Title,
                CreatedOn = selectedTask.CreatedOn,
                Description = selectedTask.Description,
                EndDate = selectedTask.EndDate,
                StartDate = selectedTask.StartDate,
                Guid = selectedTask.Guid,
                ModifiedOn = selectedTask.ModifiedOn,
                ModifiedBy = (await userManager.FindByIdAsync(selectedTask.ModifiedBy.ToString())).UserName,
                CreatedBy = (await userManager.FindByIdAsync(selectedTask.CreatedBy.ToString())).UserName,
                PriorityId = selectedTask.TaskPriorityId,
                PriorityTitle = localizer[((SeedDataEnum.TaskPriority)selectedTask.TaskPriorityId).ToString()]
            };

            response.Value = updatedTaskResponseModel;
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

            var userTasks = vwsDbContext.GeneralTasks.Include(task => task.TaskPriority).Where(task => task.CreatedBy == userId);
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
                    ModifiedBy = (await userManager.FindByIdAsync(userTask.ModifiedBy.ToString())).UserName,
                    Guid = userTask.Guid,
                    PriorityId = userTask.TaskPriorityId,
                    PriorityTitle = localizer[userTask.TaskPriority.Name]
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
                        ModifiedBy = (await userManager.FindByIdAsync(userTask.ModifiedBy.ToString())).UserName,
                        Guid = userTask.Guid,
                        PriorityId = userTask.TaskPriorityId,
                        PriorityTitle = localizer[userTask.TaskPriority.Name]
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
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (selectedTask.CreatedBy != userId)
            {
                response.Message = "Task access forbidden";
                response.AddError(localizer["You don't have access to this task."]);
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            selectedTask.IsDeleted = true;
            selectedTask.ModifiedBy = userId;
            selectedTask.ModifiedOn = DateTime.Now;

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
                return StatusCode(StatusCodes.Status400BadRequest, response);
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
                return StatusCode(StatusCodes.Status400BadRequest, response);
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
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if(vwsDbContext.TaskAssigns.Any(taskAssign => taskAssign.UserProfileId == selectedUserId &&
                                             taskAssign.GeneralTaskId == model.TaskId &&
                                             taskAssign.IsDeleted == false))
            {
                response.AddError(localizer["Task is assigned to user before."]);
                response.Message = "Task assigned before";
                return StatusCode(StatusCodes.Status400BadRequest, response);
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

        [HttpGet]
        [Authorize]
        [Route("getUsersAssignedTo")]
        public async Task<IActionResult> GetUsersAssignedTo(long id)
        {
            var response = new ResponseModel<List<UserModel>>();
            var assignedUsersList = new List<UserModel>();

            var userId = LoggedInUserId.Value;

            var selectedTask = await vwsDbContext.GetTaskAsync(id);

            if(selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if(selectedTask.CreatedBy != userId)
            {
                var assignedTask = vwsDbContext.TaskAssigns.FirstOrDefault(taskAssign => taskAssign.UserProfileId == userId &&
                                                                                         taskAssign.GeneralTaskId == id &&
                                                                                         taskAssign.IsDeleted == false);

                if (assignedTask == null)
                {
                    response.Message = "Task access forbidden";
                    response.AddError(localizer["You don't have access to this task."]);
                    return StatusCode(StatusCodes.Status403Forbidden, response);
                }
            }

            var assignedUsers = vwsDbContext.TaskAssigns.Include(taskAssign => taskAssign.UserProfile)
                                                        .Where(taskAssign => taskAssign.GeneralTaskId == id && !taskAssign.IsDeleted)
                                                        .Select(taskAssign => taskAssign.UserProfile);

            foreach(var user in assignedUsers)
            {
                assignedUsersList.Add(new UserModel()
                {
                    ProfileImageId = user.ProfileImageId,
                    UserId = user.UserId,
                    UserName = (await userManager.FindByIdAsync(user.UserId.ToString())).UserName
                });
            }

            response.Message = "Users returned successfully!";
            response.Value = assignedUsersList;
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("deleteUserAssignedTo")]
        public async Task<IActionResult> DeleteUserAssignedTo(long taskId, Guid userId)
        {
            var response = new ResponseModel();

            var selectedTask = await vwsDbContext.GetTaskAsync(taskId);

            if (selectedTask == null || selectedTask.IsDeleted)
            {
                response.Message = "Task not found";
                response.AddError(localizer["Task does not exist."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (selectedTask.CreatedBy != LoggedInUserId.Value)
            {
                var assignedTask = vwsDbContext.TaskAssigns.FirstOrDefault(taskAssign => taskAssign.UserProfileId == LoggedInUserId.Value &&
                                                                                         taskAssign.GeneralTaskId == taskId &&
                                                                                         taskAssign.IsDeleted == false);

                if (assignedTask == null)
                {
                    response.Message = "Task access forbidden";
                    response.AddError(localizer["You don't have access to this task."]);
                    return StatusCode(StatusCodes.Status403Forbidden, response);
                }
            }

            var selectedUserAssignedTask = vwsDbContext.TaskAssigns.FirstOrDefault(taskAssign => taskAssign.UserProfileId == userId &&
                                                                                   taskAssign.GeneralTaskId == taskId &&
                                                                                   taskAssign.IsDeleted == false);

            if (selectedUserAssignedTask == null)
            {
                response.Message = "User does not have access already!";
                response.AddError(localizer["User you selected does not have access to task already."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            selectedUserAssignedTask.IsDeleted = true;
            selectedUserAssignedTask.DeletedBy = LoggedInUserId.Value;
            selectedUserAssignedTask.DeletedOn = DateTime.Now;

            vwsDbContext.Save();

            response.Message = "User unassigned from task successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getTaskPriorities")]
        public ICollection<Object> GetTaskPriorities()
        {
            List<Object> result = new List<Object>();

            foreach (var priority in Enum.GetValues(typeof(SeedDataEnum.TaskPriority)))
                result.Add(new { Id = (byte)priority, Name = priority.ToString() });

            return result;
        }
    }
}
