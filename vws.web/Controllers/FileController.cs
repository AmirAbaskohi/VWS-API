using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using vws.web.Domain;

namespace vws.web.Controllers
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class FileController : BaseController
    {
        private readonly IConfiguration configuration;
        private readonly IStringLocalizer<TeamController> localizer;
        private readonly IVWS_DbContext vwsDbContext;

        public FileController(IConfiguration _configuration, IStringLocalizer<TeamController> _localizer,
            IVWS_DbContext _vwsDbContext)
        {
            configuration = _configuration;
            localizer = _localizer;
            vwsDbContext = _vwsDbContext;
        }

        private async Task<bool> WriteFile(IFormFile file)
        {
            bool isSaveSuccess = false;
            string fileName;
            try
            {
                var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
                Guid fileGuid = Guid.NewGuid();
                fileName = fileGuid.ToString() + extension;

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
                    Address = path
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
        public async Task<IActionResult> UploadFiles(List<IFormFile> files)
        {
            foreach (var file in files)
                if (await WriteFile(file))
                    return Ok();
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
