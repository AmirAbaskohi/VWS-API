using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Models;
using vws.web.Repositories;
using Microsoft.EntityFrameworkCore;
using vws.web.Models._department;
using vws.web.Domain._department;
using System.Collections.Generic;
using vws.web.Domain._file;

namespace vws.web.Controllers._department
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class DepartmentController : BaseController
    {
        private readonly IStringLocalizer<DepartmentController> localizer;
        private readonly IVWS_DbContext vwsDbContext;
        private readonly IFileManager fileManager;
        private readonly UserManager<ApplicationUser> userManager;

        public DepartmentController(IStringLocalizer<DepartmentController> _localizer, IVWS_DbContext _vwsDbContext,
                                    IFileManager _fileManager, UserManager<ApplicationUser> _userManager)
        {
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
            fileManager = _fileManager;
            userManager = _userManager;
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
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var userId = LoggedInUserId.Value;

            var selectedTeamMember = await vwsDbContext.GetTeamMemberAsync(model.TeamId, userId);
            if (selectedTeamMember == null)
            {
                response.AddError(localizer["You are not member of team."]);
                response.Message = "Team access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var departmentNames = vwsDbContext.Departments.Where(department => department.TeamId == model.TeamId && department.IsDeleted == false).Select(department => department.Name);

            if (departmentNames.Contains(model.Name))
            {
                response.AddError(localizer["There is already a department with given name, in given team."]);
                response.Message = "Name of department is used";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var time = DateTime.Now;

            var newDepartment = new Department()
            {
                Name = model.Name,
                Description = model.Description,
                IsDeleted = false,
                Color = model.Color,
                TeamId = model.TeamId,
                Guid = Guid.NewGuid(),
                CreatedBy = userId,
                ModifiedBy = userId,
                CreatedOn = time,
                ModifiedOn = time
            };

            await vwsDbContext.AddDepartmentAsync(newDepartment);
            vwsDbContext.Save();

            var newDepartmentMember = new DepartmentMember()
            {
                IsDeleted = false,
                CreatedOn = time,
                UserProfileId = userId,
                DepartmentId = newDepartment.Id
            };

            await vwsDbContext.AddDepartmentMemberAsync(newDepartmentMember);
            vwsDbContext.Save();

            var departmentResponse = new DepartmentResponseModel()
            {
                Id = newDepartment.Id,
                Color = newDepartment.Color,
                Name = newDepartment.Name,
                TeamId = newDepartment.TeamId,
                Guid = newDepartment.Guid,
                Description = newDepartment.Description,
                CreatedBy = (await userManager.FindByIdAsync(newDepartment.CreatedBy.ToString())).UserName,
                ModifiedBy = (await userManager.FindByIdAsync(newDepartment.ModifiedBy.ToString())).UserName,
                CreatedOn = newDepartment.CreatedOn,
                ModifiedOn = newDepartment.ModifiedOn,
                DepartmentImageId = newDepartment.DepartmentImageId
            };

            response.Value = departmentResponse;
            response.Message = "Department added successfully";
            return Ok(response);
        }


        [HttpPut]
        [Authorize]
        [Route("update")]
        public async Task<IActionResult> UpdateDepartment([FromBody] UpdateDepartmentModel model)
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
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var userId = LoggedInUserId.Value;

            var selectedTeamMember = await vwsDbContext.GetTeamMemberAsync(model.TeamId, userId);
            if (selectedTeamMember == null)
            {
                response.AddError(localizer["You are not member of team."]);
                response.Message = "Team access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var selectedDepartment = vwsDbContext.Departments.FirstOrDefault(department => department.Id == model.Id);
            if (selectedDepartment == null || selectedDepartment.IsDeleted)
            {
                response.AddError(localizer["There is no department with such id."]);
                response.Message = "Department not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!vwsDbContext.DepartmentMembers.Any(departmentMember => departmentMember.DepartmentId == model.Id &&
                                                   departmentMember.UserProfileId == userId && departmentMember.IsDeleted == false))
            {
                response.AddError(localizer["You are not member of given department."]);
                response.Message = "Department access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (selectedDepartment.TeamId != model.TeamId)
            {
                var departmentNames = vwsDbContext.Departments.Where(department => department.TeamId == model.TeamId && department.IsDeleted == false).Select(department => department.Name);

                if (departmentNames.Contains(model.Name))
                {
                    response.AddError(localizer["There is already a department with given name, in given team."]);
                    response.Message = "Name of department is used";
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }
            }

            selectedDepartment.TeamId = model.TeamId;
            selectedDepartment.Name = model.Name;
            selectedDepartment.ModifiedBy = userId;
            selectedDepartment.ModifiedOn = DateTime.Now;
            selectedDepartment.Color = model.Color;
            selectedDepartment.Description = model.Description;

            vwsDbContext.Save();

            var departmentResponse = new DepartmentResponseModel()
            {
                Color = selectedDepartment.Color,
                Id = selectedDepartment.Id,
                Name = selectedDepartment.Name,
                TeamId = selectedDepartment.TeamId,
                Guid = selectedDepartment.Guid,
                Description = selectedDepartment.Description,
                CreatedBy = (await userManager.FindByIdAsync(selectedDepartment.CreatedBy.ToString())).UserName,
                ModifiedBy = (await userManager.FindByIdAsync(selectedDepartment.ModifiedBy.ToString())).UserName,
                CreatedOn = selectedDepartment.CreatedOn,
                ModifiedOn = selectedDepartment.ModifiedOn,
                DepartmentImageId = selectedDepartment.DepartmentImageId
            };

            response.Message = "Department updated successfully!";
            response.Value = departmentResponse;
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("delete")]
        public IActionResult DeleteDepartment(int id)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            var selectedDepartment = vwsDbContext.Departments.FirstOrDefault(department => department.Id == id);

            if (selectedDepartment == null || selectedDepartment.IsDeleted)
            {
                response.AddError(localizer["There is no department with such id."]);
                response.Message = "Department not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!vwsDbContext.DepartmentMembers.Any(departmentMember => departmentMember.DepartmentId == id &&
                                                   departmentMember.UserProfileId == userId && departmentMember.IsDeleted == false))
            {
                response.AddError(localizer["You are not member of given department."]);
                response.Message = "Department access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var departmentProjects = vwsDbContext.ProjectDepartments.Where(projectDepartment => projectDepartment.DepartmentId == id);
            foreach (var departmentProject in departmentProjects)
                vwsDbContext.DeleteProjectDepartment(departmentProject);

            selectedDepartment.IsDeleted = true;
            selectedDepartment.ModifiedBy = userId;
            selectedDepartment.ModifiedOn = DateTime.Now;

            vwsDbContext.Save();

            response.Message = "Department deleted successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getAll")]
        public async Task<IEnumerable<DepartmentResponseModel>> GetAllDepartments()
        {
            List<DepartmentResponseModel> response = new List<DepartmentResponseModel>();

            var userId = LoggedInUserId.Value;

            var userDepartments = vwsDbContext.GetUserDepartments(userId).ToList();

            foreach (var userDepartment in userDepartments)
            {
                response.Add(new DepartmentResponseModel()
                {
                    Color = userDepartment.Color,
                    Id = userDepartment.Id,
                    Name = userDepartment.Name,
                    TeamId = userDepartment.TeamId,
                    Guid = userDepartment.Guid,
                    Description = userDepartment.Description,
                    CreatedBy = (await userManager.FindByIdAsync(userDepartment.CreatedBy.ToString())).UserName,
                    ModifiedBy = (await userManager.FindByIdAsync(userDepartment.ModifiedBy.ToString())).UserName,
                    CreatedOn = userDepartment.CreatedOn,
                    ModifiedOn = userDepartment.ModifiedOn,
                    DepartmentImageId = userDepartment.DepartmentImageId
                });
            }

            return response;
        }

        [HttpPost]
        [Authorize]
        [Route("addTeammateToDepartment")]
        public async Task<IActionResult> AddTeamMateToDepartment([FromBody] AddTeammateToDepartmentModel model)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;
            var selectedUserId = new Guid(model.UserId);

            var selectedDepartment = vwsDbContext.Departments.FirstOrDefault(department => department.Id == model.DepartmentId);

            if (selectedDepartment == null || selectedDepartment.IsDeleted)
            {
                response.AddError(localizer["There is no department with such id."]);
                response.Message = "Department not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeam = await vwsDbContext.GetTeamAsync(model.TeamId);

            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.AddError(localizer["There is no team with given Id."]);
                response.Message = "Team not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!vwsDbContext.UserProfiles.Any(profile => profile.UserId == selectedUserId))
            {
                response.AddError(localizer["There is no user with given Id."]);
                response.Message = "User not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeamMember = await vwsDbContext.GetTeamMemberAsync(model.TeamId, userId);
            if(selectedTeamMember == null)
            {
                response.AddError(localizer["You are not member of team."]);
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

            if(!vwsDbContext.DepartmentMembers.Any(departmentMember => departmentMember.UserProfileId == userId && departmentMember.DepartmentId == model.DepartmentId && !departmentMember.IsDeleted))
            {
                response.AddError(localizer["You are not member of given department."]);
                response.Message = "Department access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (vwsDbContext.DepartmentMembers.Any(departmentMember => departmentMember.UserProfileId == selectedUserId && departmentMember.DepartmentId == model.DepartmentId && !departmentMember.IsDeleted))
            {
                response.AddError(localizer["User you want to add, is already a member of selected department."]);
                response.Message = "User added before";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var newDepartmentMember = new DepartmentMember()
            {
                CreatedOn = DateTime.Now,
                IsDeleted = false,
                DepartmentId = model.DepartmentId,
                UserProfileId = selectedUserId
            };

            await vwsDbContext.AddDepartmentMemberAsync(newDepartmentMember);
            vwsDbContext.Save();

            response.Message = "User added to department successfully!";
            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        [Route("uploadDepartmentImage")]
        public async Task<IActionResult> UploadDepartmentImage(IFormFile image, int departmentId)
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

            var selectedDepartment = vwsDbContext.Departments.Include(department => department.DepartmentImage).FirstOrDefault(department => department.Id == departmentId);
            if (selectedDepartment == null || selectedDepartment.IsDeleted)
            {
                response.AddError(localizer["There is no department with such id."]);
                response.Message = "Department not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedDepartmentMember = vwsDbContext.DepartmentMembers.FirstOrDefault(departmentMember => departmentMember.UserProfileId == userId &&
                                                                                                    departmentMember.DepartmentId == departmentId &&
                                                                                                    !departmentMember.IsDeleted);
            if (selectedDepartmentMember == null)
            {
                response.AddError(localizer["You are not member of given department."]);
                response.Message = "Not member of department";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            ResponseModel<File> fileResponse;

            if (selectedDepartment.DepartmentImage != null)
            {
                fileResponse = await fileManager.WriteFile(uploadedImage, userId, "profileImages", (int)selectedDepartment.DepartmentImageId, types.ToList());
                if (fileResponse.HasError)
                {
                    foreach (var error in fileResponse.Errors)
                        response.AddError(localizer[error]);
                    response.Message = "Error in writing file";
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
                selectedDepartment.DepartmentImage.RecentFileId = fileResponse.Value.Id;
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
                selectedDepartment.DepartmentImageId = newFileContainer.Id;
            }
            selectedDepartment.ModifiedBy = LoggedInUserId.Value;
            selectedDepartment.ModifiedOn = DateTime.Now;
            vwsDbContext.Save();
            response.Message = "Department image added successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getCoDepartments")]
        public async Task<IActionResult> GetCoDepartments(int departnmentId)
        {
            var response = new ResponseModel<List<UserModel>>();
            var coDepartmentsList = new List<UserModel>();

            var selectedDepartment = vwsDbContext.Departments.FirstOrDefault(department => department.Id == departnmentId);
            var userId = LoggedInUserId.Value;

            if (selectedDepartment == null || selectedDepartment.IsDeleted)
            {
                response.Message = "Department not found";
                response.AddError(localizer["There is no department with such id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedDepartmentMember = vwsDbContext.DepartmentMembers.FirstOrDefault(departmentMember => departmentMember.UserProfileId == userId &&
                                                                                                             departmentMember.IsDeleted == false &&
                                                                                                             departmentMember.DepartmentId == departnmentId);

            if (selectedDepartmentMember == null)
            {
                response.Message = "Not member of department";
                response.AddError(localizer["You are not member of given department."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            List<UserProfile> userCoDepartments = vwsDbContext.DepartmentMembers
                .Include(departmentMember => departmentMember.UserProfile)
                .Where(departmentMember => departmentMember.DepartmentId == departnmentId && departmentMember.IsDeleted == false)
                .Select(departmentMember => departmentMember.UserProfile).Distinct().ToList();

            foreach (var userCoDepartment in userCoDepartments)
            {
                var userName = (await userManager.FindByIdAsync(userCoDepartment.UserId.ToString())).UserName;
                coDepartmentsList.Add(new UserModel()
                {
                    UserName = userName,
                    UserId = userCoDepartment.UserId,
                    ProfileImageId = userCoDepartment.ProfileImageId
                });
            }

            response.Message = "Co-departments are given successfully!";
            response.Value = coDepartmentsList;
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public async Task<IActionResult> GetDepartment(int id)
        {
            Guid userId = LoggedInUserId.Value;

            var response = new ResponseModel<DepartmentResponseModel>();

            var selectedDepartment = vwsDbContext.Departments.FirstOrDefault(department => department.Id == id);

            if (selectedDepartment == null || selectedDepartment.IsDeleted)
            {
                response.AddError(localizer["There is no department with such id."]);
                response.Message = "Department not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var departmentMember = vwsDbContext.DepartmentMembers.FirstOrDefault(departmentMember => departmentMember.DepartmentId == id &&
                                                                                 departmentMember.UserProfileId == userId &&
                                                                                 departmentMember.IsDeleted == false);

            if (departmentMember == null)
            {
                response.AddError(localizer["You are not member of given department."]);
                response.Message = "Not member of department";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            response.Value = new DepartmentResponseModel()
            {
                Id = selectedDepartment.Id,
                Name = selectedDepartment.Name,
                Color = selectedDepartment.Color,
                Guid = selectedDepartment.Guid,
                Description = selectedDepartment.Description,
                CreatedBy = (await userManager.FindByIdAsync(selectedDepartment.CreatedBy.ToString())).UserName,
                ModifiedBy = (await userManager.FindByIdAsync(selectedDepartment.ModifiedBy.ToString())).UserName,
                CreatedOn = selectedDepartment.CreatedOn,
                ModifiedOn = selectedDepartment.ModifiedOn,
                DepartmentImageId = selectedDepartment.DepartmentImageId
            };
            response.Message = "Department retured successfully!";
            return Ok(response);
        }
    }
}
