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
        private readonly IStringLocalizer<FileController> localizer;
        private readonly IVWS_DbContext vwsDbContext;
        private readonly IFileManager fileManager;

        public FileController(IStringLocalizer<FileController> _localizer,
            IVWS_DbContext _vwsDbContext, IFileManager _fileManager)
        {
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
            fileManager = _fileManager;
        }

        [HttpPost]
        [Authorize]
        [Route("upload")]
        public async Task<IActionResult> UploadFiles(List<IFormFile> formFiles, Guid? channelId, byte? channelTypeId)
        {
            var files = Request.Form.Files.Union(formFiles);
            var response = new ResponseModel<FileUploadResponseModel>();

            if ((channelId == null && channelTypeId != null) && (channelId != null && channelTypeId == null))
            {
                response.AddError(localizer["Invalid parameters."]);
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
                await vwsDbContext.AddFileContainerAsync(newFileContainer);
                vwsDbContext.Save();
                var result = await fileManager.WriteFile(file, LoggedInUserId.Value, location, newFileContainer.Id);
                if (result.HasError)
                {
                    foreach (var error in result.Errors)
                        response.AddError(localizer[error]);
                    unsuccessfullyFileContainers.Add(newFileContainer);
                    unsuccessfullyUploadedFiles.Add(file.FileName);
                }
                else
                {
                    successfullyUploadedFiles.Add(result.Value);
                    newFileContainer.RecentFileId = result.Value.Id;
                    vwsDbContext.Save();
                }
            }

            var successfulFiles = successfullyUploadedFiles.Select(file => new FileModel { Name = file.Name, Extension = file.Extension, Size = file.Size, FileContainerGuid = file.FileContainerGuid }).ToList();

            if (unsuccessfullyFileContainers.Count != 0)
            {
                foreach(var unsuccessfullFileContainer in unsuccessfullyFileContainers)
                    vwsDbContext.DeleteFileContainer(unsuccessfullFileContainer);

                vwsDbContext.Save();          
            }
            response.Value = new FileUploadResponseModel() { SuccessfulFileUpload = successfulFiles, UnsuccessfulFileUpload = unsuccessfullyUploadedFiles };
            response.Message = "Successful writing";
            return Ok(response);
        }


        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetFile(Guid id)
        {
            var response = new ResponseModel();
            var fileContainer = await vwsDbContext.GetFileContainerAsync(id);
            if (fileContainer == null)
            {
                response.AddError(localizer["There is no such file."]);
                response.Message = "File not found";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            var selectedFile = (await vwsDbContext.GetFileAsync(fileContainer.RecentFileId));
            string address = selectedFile.Address;
            string fileName = selectedFile.Name;


            var memory = new MemoryStream();
            using (var stream = new FileStream(address, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/" + selectedFile.Extension, Path.GetFileName(fileName));
        }
    }
}
