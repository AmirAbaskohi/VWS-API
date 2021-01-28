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
using vws.web.Repositories;

namespace vws.web.Controllers._file
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class FileController : BaseController
    {
        private readonly IConfiguration configuration;
        private readonly IStringLocalizer<FileController> localizer;
        private readonly IVWS_DbContext vwsDbContext;
        private readonly IFileManager fileManager;

        public FileController(IConfiguration _configuration, IStringLocalizer<FileController> _localizer,
            IVWS_DbContext _vwsDbContext, IFileManager _fileManager)
        {
            configuration = _configuration;
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
            fileManager = _fileManager;
        }

        [HttpPost]
        [Authorize]
        [Route("upload")]
        public async Task<IActionResult> UploadFiles(List<IFormFile> formFiles)
        {
            var files = Request.Form.Files.Union(formFiles);
            foreach (var file in files)
                if (await fileManager.WriteFile(file, LoggedInUserId.Value, "files"))
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
