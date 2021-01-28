using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using vws.web.Domain;

namespace vws.web.Controllers._file
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class FileController : BaseController
    {
        private readonly IConfiguration configuration;
        private readonly IStringLocalizer<FileController> localizer;
        private readonly IVWS_DbContext vwsDbContext;

        public FileController(IConfiguration _configuration, IStringLocalizer<FileController> _localizer,
            IVWS_DbContext _vwsDbContext)
        {
            configuration = _configuration;
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
        }

        private async Task<bool> WriteFile(IFormFile file, Guid userId)
        {
            bool isSaveSuccess = false;
            string fileName;
            try
            {
                var extension = file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
                Guid fileGuid = Guid.NewGuid();
                fileName = fileGuid.ToString() + "." + extension;

                var pathBuilt = Path.Combine(Directory.GetCurrentDirectory(), $"Upload{Path.DirectorySeparatorChar}files");

                if (!Directory.Exists(pathBuilt))
                {
                    Directory.CreateDirectory(pathBuilt);
                }

                var path = Path.Combine(Directory.GetCurrentDirectory(), $"Upload{Path.DirectorySeparatorChar}files",
                   fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var newFile = new Domain._file.File()
                {
                    FileId = fileGuid,
                    Address = path,
                    Extension = extension,
                    Name = file.FileName,
                    UploadedBy = userId
                };

                await vwsDbContext.AddFileAsync(newFile);
                vwsDbContext.Save();


                isSaveSuccess = true;
            }
            catch (Exception e)
            {
                //log error
            }

            return isSaveSuccess;
        }

        [HttpPost]
        [Authorize]
        [Route("upload")]
        public async Task<IActionResult> UploadFiles(List<IFormFile> files)
        {
            foreach (var file in files)
                if (await WriteFile(file, LoggedInUserId.Value))
                    return Ok();
            return StatusCode(StatusCodes.Status500InternalServerError);
        }


        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetFile(string guid)
        {
            Guid fileId = new Guid(guid);
            var selectedFile = (await vwsDbContext.GetFileAsync(fileId));
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
