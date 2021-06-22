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
using vws.web.Services._department;
using vws.web.Services;
using vws.web.Enums;
using Newtonsoft.Json;
using static vws.web.EmailTemplates.EmailTemplateTypes;

namespace vws.web.Controllers._department
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class DepartmentController : BaseController
    {
        #region Feilds
        private readonly IStringLocalizer<DepartmentController> _localizer;
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly IFileManager _fileManager;
        private readonly IDepartmentManagerService _departmentManager;
        private readonly IImageService _imageService;
        private readonly IPermissionService _permissionService;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        #endregion
        
        #region Ctor
        public DepartmentController(IStringLocalizer<DepartmentController> localizer, IVWS_DbContext vwsDbContext,
                                    IFileManager fileManager, IDepartmentManagerService departmentManager,
                                    IImageService imageService, IPermissionService permissionService,
                                    INotificationService notificationService, IUserService userService)
        {
            _localizer = localizer;
            _vwsDbContext = vwsDbContext;
            _fileManager = fileManager;
            _departmentManager = departmentManager;
            _imageService = imageService;
            _permissionService = permissionService;
            _notificationService = notificationService;
            _userService = userService;
        }
        #endregion

        #region DepartmentAPIS
        [HttpPost]
        [Authorize]
        [Route("create")]
        public async Task<IActionResult> CreateDepartment([FromBody] DepartmentModel model)
        {
            var response = new ResponseModel<DepartmentResponseModel>();
            model.Users = model.Users.Distinct().ToList();

            #region CheckModel
            if (!String.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
            {
                response.Message = "Department model data has problem.";
                response.AddError(_localizer["Length of description is more than 2000 characters."]);
            }
            if (model.Name.Length > 500)
            {
                response.Message = "Department model data has problem.";
                response.AddError(_localizer["Length of name is more than 500 characters."]);
            }
            if (!String.IsNullOrEmpty(model.Color) && model.Color.Length > 6)
            {
                response.Message = "Department model data has problem.";
                response.AddError(_localizer["Length of color is more than 6 characters."]);
            }
            var selectedTeam = await _vwsDbContext.GetTeamAsync(model.TeamId);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.AddError(_localizer["Department model data has problem."]);
                response.Message = "Team not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (response.HasError)
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            #endregion

            var userId = LoggedInUserId.Value;

            #region CheckAccess
            var selectedTeamMember = await _vwsDbContext.GetTeamMemberAsync(model.TeamId, userId);
            if (selectedTeamMember == null)
            {
                response.AddError(_localizer["You are not member of team."]);
                response.Message = "Team access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            #endregion

            #region CheckDepartmentName
            var departmentNames = _vwsDbContext.Departments.Where(department => department.TeamId == model.TeamId && department.IsDeleted == false).Select(department => department.Name);
            if (departmentNames.Contains(model.Name))
            {
                response.AddError(_localizer["There is already a department with given name, in given team."]);
                response.Message = "Name of department is used";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            #endregion

            #region CheckUsers
            foreach (var user in model.Users)
                if (!_vwsDbContext.TeamMembers.Any(teamMember => teamMember.UserProfileId == user && teamMember.TeamId == model.TeamId && !teamMember.IsDeleted))
                {
                    response.AddError(_localizer["Invalid users to add to department."]);
                    response.Message = "Invalid users to add to team.";
                    return StatusCode(StatusCodes.Status400BadRequest, response);
                }
            #endregion

            var addedDepartment = await _departmentManager.CreateDepartment(model, userId);

            #region History
            var newHistory = new DepartmentHistory()
            {
                DepartmentId = addedDepartment.Id,
                EventTime = addedDepartment.ModifiedOn,
                EventBody = "{0} department created by {1} under {2} team."
            };
            _vwsDbContext.AddDepartmentHistory(newHistory);
            _vwsDbContext.Save();

            var creator = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = addedDepartment.Name,
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(creator),
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedTeam.Name,
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = (await _departmentManager.GetDepartmentMembers(addedDepartment.Id)).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> created new department with name <b>«{1}»</b> undeer <b>«{2}»</b> team.";
            string[] arguments = { LoggedInNickName, addedDepartment.Name, selectedTeam.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Department Create", arguments);

            var departmentResponse = new DepartmentResponseModel()
            {
                Id = addedDepartment.Id,
                Color = addedDepartment.Color,
                Name = addedDepartment.Name,
                TeamId = addedDepartment.TeamId,
                Guid = addedDepartment.Guid,
                Description = addedDepartment.Description,
                CreatedBy = (await _vwsDbContext.GetUserProfileAsync(addedDepartment.CreatedBy)).NickName,
                ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(addedDepartment.ModifiedBy)).NickName,
                CreatedOn = addedDepartment.CreatedOn,
                ModifiedOn = addedDepartment.ModifiedOn,
                DepartmentImageGuid = addedDepartment.DepartmentImageGuid,
                Users = await _departmentManager.GetDepartmentMembers(addedDepartment.Id)
            };

            response.Value = departmentResponse;
            response.Message = "Department added successfully";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateName")]
        public async Task<IActionResult> UpdateName(int id, StringModel model)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();
            var newName = model.Value;

            if (String.IsNullOrEmpty(newName) || newName.Length > 500)
            {
                response.Message = "Model data has problem.";
                response.AddError(_localizer["Name can not be empty or have more than 500 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedDepartment = _vwsDbContext.Departments.FirstOrDefault(department => department.Id == id);
            if (selectedDepartment == null || selectedDepartment.IsDeleted)
            {
                response.AddError(_localizer["There is no department with such id."]);
                response.Message = "Department not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToDepartment(userId, id))
            {
                response.AddError(_localizer["You are not member of given department."]);
                response.Message = "Department access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (newName == selectedDepartment.Name)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastName = selectedDepartment.Name;
            selectedDepartment.ModifiedBy = userId;
            selectedDepartment.ModifiedOn = DateTime.UtcNow;
            selectedDepartment.Name = newName;
            _vwsDbContext.Save();

            #region History
            var newHistory = new DepartmentHistory()
            {
                DepartmentId = selectedDepartment.Id,
                EventTime = selectedDepartment.ModifiedOn,
                EventBody = "{0} updated department's name from {1} to {2}."
            };
            _vwsDbContext.AddDepartmentHistory(newHistory);
            _vwsDbContext.Save();

            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(_userService.GetUser(LoggedInUserId.Value)),
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastName,
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedDepartment.Name,
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = (await _departmentManager.GetDepartmentMembers(selectedDepartment.Id)).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated <b>«{1}»</b> department's name to <b>«{2}»</b>.";
            string[] arguments = { LoggedInNickName, lastName, selectedDepartment.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Department Update", arguments);

            response.Message = "Name updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateDescription")]
        public async Task<IActionResult> UpdateDescription(int id, StringModel model)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();
            var newDescription = model.Value;

            if (!String.IsNullOrEmpty(newDescription) && newDescription.Length > 2000)
            {
                response.Message = "Model data has problem.";
                response.AddError(_localizer["Length of description is more than 2000 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedDepartment = _vwsDbContext.Departments.FirstOrDefault(department => department.Id == id);
            if (selectedDepartment == null || selectedDepartment.IsDeleted)
            {
                response.AddError(_localizer["There is no department with such id."]);
                response.Message = "Department not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToDepartment(userId, id))
            {
                response.AddError(_localizer["You are not member of given department."]);
                response.Message = "Department access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (newDescription == selectedDepartment.Description)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastDescription = selectedDepartment.Description;
            selectedDepartment.ModifiedBy = userId;
            selectedDepartment.ModifiedOn = DateTime.UtcNow;
            selectedDepartment.Description = newDescription;
            _vwsDbContext.Save();

            #region History
            var newHistory = new DepartmentHistory()
            {
                DepartmentId = selectedDepartment.Id,
                EventTime = selectedDepartment.ModifiedOn,
                EventBody = "{0} updated department's description from {1} to {2}."
            };
            _vwsDbContext.AddDepartmentHistory(newHistory);
            _vwsDbContext.Save();

            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(_userService.GetUser(LoggedInUserId.Value)),
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastDescription,
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedDepartment.Description,
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = (await _departmentManager.GetDepartmentMembers(selectedDepartment.Id)).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated <b>«{1}»</b> department's description from <b>«{2}»</b> to <b>«{3}»</b>.";
            string[] arguments = { LoggedInNickName, selectedDepartment.Name,lastDescription, selectedDepartment.Description };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Department Update", arguments);

            response.Message = "Description updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateColor")]
        public async Task<IActionResult> UpdateColor(int id, StringModel model)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();
            var newColor = model.Value;

            if (!String.IsNullOrEmpty(newColor) && newColor.Length > 6)
            {
                response.Message = "Model data has problem.";
                response.AddError(_localizer["Length of color is more than 6 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedDepartment = _vwsDbContext.Departments.FirstOrDefault(department => department.Id == id);
            if (selectedDepartment == null || selectedDepartment.IsDeleted)
            {
                response.AddError(_localizer["There is no department with such id."]);
                response.Message = "Department not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToDepartment(userId, id))
            {
                response.AddError(_localizer["You are not member of given department."]);
                response.Message = "Department access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (newColor == selectedDepartment.Color)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastColor = selectedDepartment.Color;
            selectedDepartment.Color = newColor;
            selectedDepartment.ModifiedBy = userId;
            selectedDepartment.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newHistory = new DepartmentHistory()
            {
                DepartmentId = selectedDepartment.Id,
                EventTime = selectedDepartment.ModifiedOn,
                EventBody = "{0} updated department's color from {1} to {2}."
            };
            _vwsDbContext.AddDepartmentHistory(newHistory);
            _vwsDbContext.Save();

            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(_userService.GetUser(LoggedInUserId.Value)),
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastColor,
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedDepartment.Color,
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = (await _departmentManager.GetDepartmentMembers(selectedDepartment.Id)).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated <b>«{1}»</b> department's color from <b>«{2}»</b> to <b>«{3}»</b>.";
            string[] arguments = { LoggedInNickName, selectedDepartment.Name, lastColor, selectedDepartment.Color };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Department Update", arguments);

            response.Message = "Color updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateTeam")]
        public async Task<IActionResult> UpdateTeam(int id, int newTeamId)
        {
            var userId = LoggedInUserId.Value;
            var response = new ResponseModel();

            var selectedTeam = await _vwsDbContext.GetTeamAsync(newTeamId);
            if (selectedTeam == null || selectedTeam.IsDeleted)
            {
                response.AddError(_localizer["There is no team with such id."]);
                response.Message = "Team not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToTeam(userId, newTeamId))
            {
                response.AddError(_localizer["You are not member of team."]);
                response.Message = "Team access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var selectedDepartment = _vwsDbContext.Departments.FirstOrDefault(department => department.Id == id);
            if (selectedDepartment == null || selectedDepartment.IsDeleted)
            {
                response.AddError(_localizer["There is no department with such id."]);
                response.Message = "Department not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToDepartment(userId, id))
            {
                response.AddError(_localizer["You are not member of given department."]);
                response.Message = "Department access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var departmentNames = _vwsDbContext.Departments.Where(department => department.TeamId == newTeamId && !department.IsDeleted).Select(department => department.Name);
            if (departmentNames.Contains(selectedDepartment.Name))
            {
                response.AddError(_localizer["There is already a department with given name, in given team."]);
                response.Message = "Name of department is used";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (newTeamId == selectedDepartment.TeamId)
            {
                response.Message = "Duplicate data";
                return Ok(response);
            }

            var lastTeamName = _vwsDbContext.Teams.FirstOrDefault(team => team.Id == selectedDepartment.TeamId).Name;
            selectedDepartment.ModifiedBy = userId;
            selectedDepartment.ModifiedOn = DateTime.UtcNow;
            selectedDepartment.TeamId = newTeamId;
            _vwsDbContext.Save();

            #region History
            var newHistory = new DepartmentHistory()
            {
                DepartmentId = selectedDepartment.Id,
                EventTime = selectedDepartment.ModifiedOn,
                EventBody = "{0} updated department's team from {1} to {2}."
            };
            _vwsDbContext.AddDepartmentHistory(newHistory);
            _vwsDbContext.Save();

            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(_userService.GetUser(LoggedInUserId.Value)),
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = lastTeamName,
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedTeam.Name,
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = (await _departmentManager.GetDepartmentMembers(selectedDepartment.Id)).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated <b>«{1}»</b> department's team from <b>«{2}»</b> to <b>«{3}»</b>.";
            string[] arguments = { LoggedInNickName, selectedDepartment.Name, lastTeamName, selectedTeam.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Department Update", arguments);

            response.Message = "Team updated successfully!";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("uploadDepartmentImage")]
        public async Task<IActionResult> UploadDepartmentImage(int id, IFormFile image)
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

            var selectedDepartment = _vwsDbContext.Departments.Include(department => department.DepartmentImage)
                                                             .Include(department => department.Team)
                                                             .FirstOrDefault(department => department.Id == id);
            if (selectedDepartment == null || selectedDepartment.IsDeleted || selectedDepartment.Team.IsDeleted)
            {
                response.AddError(_localizer["There is no department with such id."]);
                response.Message = "Department not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_permissionService.HasAccessToDepartment(userId, id))
            {
                response.AddError(_localizer["You are not member of given department."]);
                response.Message = "Not member of department";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (!_imageService.IsImage(uploadedImage))
            {
                response.AddError(_localizer["Invalid file."]);
                response.Message = "Invalid file";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_imageService.IsImageSquare(uploadedImage))
            {
                response.AddError(_localizer["Image must be square."]);
                response.Message = "Invalid aspect ratio";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            ResponseModel<File> fileResponse;

            if (selectedDepartment.DepartmentImage != null)
            {
                fileResponse = await _fileManager.WriteFile(uploadedImage, userId, "profileImages", (int)selectedDepartment.DepartmentImageId, types.ToList());
                if (fileResponse.HasError)
                {
                    foreach (var error in fileResponse.Errors)
                        response.AddError(_localizer[error]);
                    response.Message = "Error in writing file";
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
                selectedDepartment.DepartmentImage.RecentFileId = fileResponse.Value.Id;
            }
            else
            {
                var time = DateTime.UtcNow;
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
                selectedDepartment.DepartmentImageId = newFileContainer.Id;
                selectedDepartment.DepartmentImageGuid = newFileContainer.Guid;
            }
            selectedDepartment.ModifiedBy = LoggedInUserId.Value;
            selectedDepartment.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            _imageService.SaveInOtherQualities(fileResponse.Value);

            #region History
            var newHistory = new DepartmentHistory()
            {
                DepartmentId = selectedDepartment.Id,
                EventTime = selectedDepartment.ModifiedOn,
                EventBody = "Department's image updated to {0} by {1}."
            };
            _vwsDbContext.AddDepartmentHistory(newHistory);
            _vwsDbContext.Save();

            var user = await _vwsDbContext.GetUserProfileAsync(userId);
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.File,
                Body = JsonConvert.SerializeObject(new FileModel()
                {
                    Extension = fileResponse.Value.Extension,
                    FileContainerGuid = fileResponse.Value.FileContainerGuid,
                    Name = fileResponse.Value.Name,
                    Size = fileResponse.Value.Size
                }),
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(new UserModel()
                {
                    NickName = user.NickName,
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId
                }),
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = (await _departmentManager.GetDepartmentMembers(selectedDepartment.Id)).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> updated department's image to <b>«{1}»</b> in your department with name <b>«{2}»</b>.";
            string[] arguments = { LoggedInNickName, $"<a href='{Request.Scheme}://{Request.Host}/en-US/File/get?id={fileResponse.Value.FileContainerGuid}'>{fileResponse.Value.Name}</a>", selectedDepartment.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Department Update", arguments);

            response.Value = fileResponse.Value.FileContainerGuid;
            response.Message = "Department image added successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getAll")]
        public async Task<IEnumerable<DepartmentResponseModel>> GetAllDepartments()
        {
            List<DepartmentResponseModel> response = new List<DepartmentResponseModel>();

            var userId = LoggedInUserId.Value;

            var userDepartments = _departmentManager.GetAllUserDepartments(userId);

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
                    CreatedBy = (await _vwsDbContext.GetUserProfileAsync(userDepartment.CreatedBy)).NickName,
                    ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(userDepartment.ModifiedBy)).NickName,
                    CreatedOn = userDepartment.CreatedOn,
                    ModifiedOn = userDepartment.ModifiedOn,
                    DepartmentImageGuid = userDepartment.DepartmentImageGuid,
                    Users = await _departmentManager.GetDepartmentMembers(userDepartment.Id)
                });
            }

            return response;
        }

        [HttpGet]
        [Authorize]
        [Route("get")]
        public async Task<IActionResult> GetDepartment(int id)
        {
            Guid userId = LoggedInUserId.Value;

            var response = new ResponseModel<DepartmentResponseModel>();

            var selectedDepartment = _vwsDbContext.Departments.Include(department => department.Team).FirstOrDefault(department => department.Id == id);

            if (selectedDepartment == null || selectedDepartment.IsDeleted || selectedDepartment.Team.IsDeleted)
            {
                response.AddError(_localizer["There is no department with such id."]);
                response.Message = "Department not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var departmentMember = _vwsDbContext.DepartmentMembers.FirstOrDefault(departmentMember => departmentMember.DepartmentId == id &&
                                                                                 departmentMember.UserProfileId == userId &&
                                                                                 departmentMember.IsDeleted == false);

            if (departmentMember == null)
            {
                response.AddError(_localizer["You are not member of given department."]);
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
                CreatedBy = (await _vwsDbContext.GetUserProfileAsync(selectedDepartment.CreatedBy)).NickName,
                ModifiedBy = (await _vwsDbContext.GetUserProfileAsync(selectedDepartment.ModifiedBy)).NickName,
                CreatedOn = selectedDepartment.CreatedOn,
                ModifiedOn = selectedDepartment.ModifiedOn,
                DepartmentImageGuid = selectedDepartment.DepartmentImageGuid,
                Users = await _departmentManager.GetDepartmentMembers(selectedDepartment.Id)
            };
            response.Message = "Department retured successfully!";
            return Ok(response);
        }

        [HttpDelete]
        [Authorize]
        [Route("delete")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var response = new ResponseModel();

            var userId = LoggedInUserId.Value;

            var selectedDepartment = _vwsDbContext.Departments.Include(department => department.Team).FirstOrDefault(department => department.Id == id);

            if (selectedDepartment == null || selectedDepartment.IsDeleted || selectedDepartment.Team.IsDeleted)
            {
                response.AddError(_localizer["There is no department with such id."]);
                response.Message = "Department not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!_vwsDbContext.DepartmentMembers.Any(departmentMember => departmentMember.DepartmentId == id &&
                                                   departmentMember.UserProfileId == userId && departmentMember.IsDeleted == false))
            {
                response.AddError(_localizer["You are not member of given department."]);
                response.Message = "Department access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            var departmentProjects = _vwsDbContext.ProjectDepartments.Where(projectDepartment => projectDepartment.DepartmentId == id);
            foreach (var departmentProject in departmentProjects)
                _vwsDbContext.DeleteProjectDepartment(departmentProject);

            selectedDepartment.IsDeleted = true;
            selectedDepartment.ModifiedBy = userId;
            selectedDepartment.ModifiedOn = DateTime.UtcNow;
            _vwsDbContext.Save();

            #region History
            var newHistory = new DepartmentHistory()
            {
                DepartmentId = selectedDepartment.Id,
                EventTime = selectedDepartment.ModifiedOn,
                EventBody = "{0} deleted {1} department."
            };
            _vwsDbContext.AddDepartmentHistory(newHistory);
            _vwsDbContext.Save();

            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(_userService.GetUser(LoggedInUserId.Value)),
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.Text,
                Body = selectedDepartment.Name,
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = (await _departmentManager.GetDepartmentMembers(selectedDepartment.Id)).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> deleted your department with name <b>«{1}»</b>.";
            string[] arguments = { LoggedInNickName, selectedDepartment.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Department Update", arguments);

            response.Message = "Department deleted successfully!";
            return Ok(response);
        }
        #endregion

        #region DepartmentMemberAPIS
        [HttpPost]
        [Authorize]
        [Route("addTeammateToDepartment")]
        public async Task<IActionResult> AddTeamMateToDepartment([FromBody] AddTeammateToDepartmentModel model)
        {
            var response = new ResponseModel();
            var userId = LoggedInUserId.Value;

            var selectedDepartment = _vwsDbContext.Departments.Include(department => department.Team).FirstOrDefault(department => department.Id == model.DepartmentId);
            if (selectedDepartment == null || selectedDepartment.IsDeleted || selectedDepartment.Team.IsDeleted)
            {
                response.AddError(_localizer["There is no department with such id."]);
                response.Message = "Department not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedTeamMember = await _vwsDbContext.GetTeamMemberAsync(selectedDepartment.TeamId, model.UserId);
            if(selectedTeamMember == null)
            {
                response.AddError(_localizer["User you want to to add, is not a member of selected team."]);
                response.Message = "Not member of team";
                return StatusCode(StatusCodes.Status406NotAcceptable, response);
            }

            if(!_vwsDbContext.DepartmentMembers.Any(departmentMember => departmentMember.UserProfileId == userId &&
                                                                       departmentMember.DepartmentId == model.DepartmentId &&
                                                                       !departmentMember.IsDeleted))
            {
                response.AddError(_localizer["You are not member of given department."]);
                response.Message = "Department access denied";
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (_vwsDbContext.DepartmentMembers.Any(departmentMember => departmentMember.UserProfileId == model.UserId &&
                                                                       departmentMember.DepartmentId == model.DepartmentId &&
                                                                       !departmentMember.IsDeleted))
            {
                response.AddError(_localizer["User you want to add, is already a member of selected department."]);
                response.Message = "User added before";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            await _departmentManager.AddUserToDepartment(model.UserId, model.DepartmentId);

            #region History
            var newHistory = new DepartmentHistory()
            {
                DepartmentId = selectedDepartment.Id,
                EventTime = selectedDepartment.ModifiedOn,
                EventBody = "{0} added {1} to department."
            };
            _vwsDbContext.AddDepartmentHistory(newHistory);
            _vwsDbContext.Save();

            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(_userService.GetUser(LoggedInUserId.Value)),
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            _vwsDbContext.AddDepartmentHistoryParameter(new DepartmentHistoryParameter()
            {
                ActivityParameterTypeId = (byte)SeedDataEnum.ActivityParameterTypes.User,
                Body = JsonConvert.SerializeObject(_userService.GetUser(model.UserId)),
                DepartmentHistoryId = newHistory.Id
            });
            _vwsDbContext.Save();
            #endregion

            var users = (await _departmentManager.GetDepartmentMembers(selectedDepartment.Id)).Select(user => user.UserId).ToList();
            users = users.Distinct().ToList();
            users.Remove(LoggedInUserId.Value);
            string emailMessage = "<b>«{0}»</b> added <b>«{1}»</b> to your department with name <b>«{2}»</b>.";
            string[] arguments = { LoggedInNickName, _userService.GetUser(model.UserId).NickName, selectedDepartment.Name };
            await _notificationService.SendMultipleEmails((int)EmailTemplateEnum.NotificationEmail, users, emailMessage, "Department Update", arguments);

            response.Message = "User added to department successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getCoDepartments")]
        public IActionResult GetCoDepartments(int id)
        {
            var response = new ResponseModel<List<UserModel>>();
            var coDepartmentsList = new List<UserModel>();

            var selectedDepartment = _vwsDbContext.Departments.Include(department => department.Team).FirstOrDefault(department => department.Id == id);
            var userId = LoggedInUserId.Value;

            if (selectedDepartment == null || selectedDepartment.IsDeleted || selectedDepartment.Team.IsDeleted)
            {
                response.Message = "Department not found";
                response.AddError(_localizer["There is no department with such id."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var selectedDepartmentMember = _vwsDbContext.DepartmentMembers.FirstOrDefault(departmentMember => departmentMember.UserProfileId == userId &&
                                                                                                             departmentMember.IsDeleted == false &&
                                                                                                             departmentMember.DepartmentId == id);

            if (selectedDepartmentMember == null)
            {
                response.Message = "Not member of department";
                response.AddError(_localizer["You are not member of given department."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            List<UserProfile> userCoDepartments = _vwsDbContext.DepartmentMembers
                .Include(departmentMember => departmentMember.UserProfile)
                .Where(departmentMember => departmentMember.DepartmentId == id && departmentMember.IsDeleted == false)
                .Select(departmentMember => departmentMember.UserProfile).Distinct().ToList();

            foreach (var userCoDepartment in userCoDepartments)
            {
                coDepartmentsList.Add(new UserModel()
                {
                    NickName = userCoDepartment.NickName,
                    UserId = userCoDepartment.UserId,
                    ProfileImageGuid = userCoDepartment.ProfileImageGuid
                });
            }

            response.Message = "Co-departments are given successfully!";
            response.Value = coDepartmentsList;
            return Ok(response);
        }
        #endregion
    }
}
