using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using vws.web.Domain;
using vws.web.Enums;
using vws.web.Hubs;
using vws.web.Models;
using vws.web.Models._file;
using vws.web.Repositories;

namespace vws.web.Controllers._file
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class FileController : BaseController
    {
        #region Feilds
        private readonly IStringLocalizer<FileController> _localizer;
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly IFileManager _fileManager;
        #endregion
        
        #region Ctor
        public FileController(IStringLocalizer<FileController> localizer,
            IVWS_DbContext vwsDbContext, IFileManager fileManager)
        {
            _localizer = localizer;
            _vwsDbContext = vwsDbContext;
            _fileManager = fileManager;
        }
        #endregion

        #region FileAPIS
        [HttpPost]
        [Authorize]
        [Route("upload")]
        public async Task<IActionResult> UploadFiles(List<IFormFile> formFiles, Guid? channelId, byte? channelTypeId)
        {
            var files = Request.Form.Files.Union(formFiles);
            var response = new ResponseModel<FileUploadResponseModel>();

            if ((channelId == null && channelTypeId != null) && (channelId != null && channelTypeId == null))
            {
                response.AddError(_localizer["Invalid parameters."]);
                response.Message = "Invalid parameters.";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            string channelFolderName = "";
            if (channelId != null)
                channelFolderName = (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private) ?
                                    ChatHub.CombineTwoGuidsInOrder(LoggedInUserId.Value, channelId.Value) :
                                    channelId.Value.ToString();

            string location = (channelId == null) ? "files" : $"chat{Path.DirectorySeparatorChar}" + channelFolderName;

            List<Domain._file.File> successfullyUploadedFiles = new List<Domain._file.File>();
            List<string> unsuccessfullyUploadedFiles = new List<string>();

            List<Domain._file.FileContainer> unsuccessfullyFileContainers = new List<Domain._file.FileContainer>();

            var time = DateTime.Now;

            foreach (var file in files)
            {
                var newFileContainer = new Domain._file.FileContainer()
                {
                    ModifiedBy = LoggedInUserId.Value,
                    CreatedBy = LoggedInUserId.Value,
                    CreatedOn = time,
                    ModifiedOn = time,
                    Guid = Guid.NewGuid()
                };
                await _vwsDbContext.AddFileContainerAsync(newFileContainer);
                _vwsDbContext.Save();
                var result = await _fileManager.WriteFile(file, LoggedInUserId.Value, location, newFileContainer.Id);
                if (result.HasError)
                {
                    foreach (var error in result.Errors)
                        response.AddError(_localizer[error]);
                    unsuccessfullyFileContainers.Add(newFileContainer);
                    unsuccessfullyUploadedFiles.Add(file.FileName);
                }
                else
                {
                    successfullyUploadedFiles.Add(result.Value);
                    newFileContainer.RecentFileId = result.Value.Id;
                    _vwsDbContext.Save();
                }
            }

            var successfulFiles = successfullyUploadedFiles.Select(file => new FileModel { Name = file.Name, Extension = file.Extension, Size = file.Size, FileContainerGuid = file.FileContainerGuid }).ToList();

            if (unsuccessfullyFileContainers.Count != 0)
            {
                foreach(var unsuccessfullFileContainer in unsuccessfullyFileContainers)
                    _vwsDbContext.DeleteFileContainer(unsuccessfullFileContainer);

                _vwsDbContext.Save();          
            }
            response.Value = new FileUploadResponseModel() { SuccessfulFileUpload = successfulFiles, UnsuccessfulFileUpload = unsuccessfullyUploadedFiles };
            response.Message = "Successful writing";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("update")]
        public async Task<IActionResult> UpdateFile(Guid id, IFormFile file)
        {
            var response = new ResponseModel<FileModel>();

            var files = Request.Form.Files.ToList();

            List<string> types = new List<string>();

            if (files.Count > 1)
            {
                response.AddError(_localizer["There is more than one file."]);
                response.Message = "Too many files passed";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            if (files.Count == 0 && file == null)
            {
                response.AddError(_localizer["You did not upload any file."]);
                response.Message = "There is no file";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            var uploadedFile = files.Count == 0 ? file : files[0];

            var selectedContainer = _vwsDbContext.FileContainers.FirstOrDefault(container => container.Guid == id);
            if (selectedContainer == null)
            {
                response.AddError(_localizer["There is no such file."]);
                response.Message = "File not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var recentFile = _vwsDbContext.Files.FirstOrDefault(file => file.Id == selectedContainer.RecentFileId);
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), $"Upload{Path.DirectorySeparatorChar}");
            string filePath = recentFile.Address;
            filePath = filePath.Replace(uploadPath, "");
            string[] subs = filePath.Split(Path.DirectorySeparatorChar);
            if (subs[0] == "profileImages")
            {
                string[] imageTypes = { "png", "jpg", "jpeg" };
                types.AddRange(imageTypes);
            }
            filePath = "";
            for (int i = 0; i < subs.Length - 1; i++)
            {
                filePath += subs[i];
                if (i != subs.Length - 2)
                    filePath += Path.DirectorySeparatorChar;
            }

            var fileResponse = await _fileManager.WriteFile(uploadedFile, LoggedInUserId.Value, filePath, selectedContainer.Id, types.Count == 0 ? null : types.ToList());
            if (fileResponse.HasError)
            {
                foreach (var error in fileResponse.Errors)
                    response.AddError(_localizer[error]);
                response.Message = "Error in writing file";
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
            selectedContainer.RecentFileId = fileResponse.Value.Id;
            selectedContainer.ModifiedOn = DateTime.Now;
            selectedContainer.ModifiedBy = LoggedInUserId.Value;
            _vwsDbContext.Save();

            response.Message = "File updated successfully!";
            return Ok(response);
        }

        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetFile(Guid id, string quality)
        {
            var response = new ResponseModel();
            var fileContainer = await _vwsDbContext.GetFileContainerAsync(id);
            if (fileContainer == null)
            {
                response.AddError(_localizer["There is no such file."]);
                response.Message = "File not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            var selectedFile = (await _vwsDbContext.GetFileAsync(fileContainer.RecentFileId));
            string address = selectedFile.Address;
            string fileName = selectedFile.Name;

            if (!System.IO.File.Exists(address))
            {
                response.AddError(_localizer["File not found."]);
                response.Message = "File not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!String.IsNullOrEmpty(quality))
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), $"Upload{Path.DirectorySeparatorChar}");
                string filePath = address;
                filePath = filePath.Replace(uploadPath, "");
                string[] subs = filePath.Split(Path.DirectorySeparatorChar);
                filePath = "";
                for (int i = 0; i < subs.Length - 1; i++)
                {
                    filePath += subs[i];
                    if (i != subs.Length - 2)
                        filePath += Path.DirectorySeparatorChar;
                }
                var path = uploadPath + Path.DirectorySeparatorChar + filePath + Path.DirectorySeparatorChar + selectedFile.Id.ToString() + "-" + quality + "." + selectedFile.Extension;
                address = System.IO.File.Exists(path) ? path : address;
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(address, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/" + selectedFile.Extension, Path.GetFileName(fileName));
        }

        #endregion
    }
}
