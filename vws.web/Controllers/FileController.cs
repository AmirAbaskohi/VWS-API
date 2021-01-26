using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        [Route("upload")]
        public async Task<IActionResult> UploadFiles(List<IFormFile> files)
        {
            foreach (var file in files)
                if (await WriteFile(file))
                    return Ok();
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        //[HttpGet]
        //[Route("get")]
        //public async Task<HttpResponseMessage> GetFile(string guid)
        //{
        //    Guid fileId = new Guid(guid);
        //    string address = (await vwsDbContext.GetFileAsync(fileId)).Address;
        //    string fileName = guid + "." + address.Split('.')[address.Split('.').Length - 1];

        //    byte[] fileBytes;

        //    using (FileStream fileStream = new FileStream(address, FileMode.Open, FileAccess.Read))
        //    {
        //        fileBytes = System.IO.File.ReadAllBytes(address);
        //        fileStream.Read(fileBytes, 0, Convert.ToInt32(fileStream.Length));
        //        fileStream.Close();
        //    }

        //    HttpResponseMessage result = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        //    var stream = new MemoryStream();

        //    result.Content = new StreamContent(stream);
        //    result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
        //    result.Content.Headers.ContentDisposition.FileName = fileName;
        //    result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        //    return result;
        //}


        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetFile(string guid)
        {
            Guid fileId = new Guid(guid);
            string address = (await vwsDbContext.GetFileAsync(fileId)).Address;
            string fileName = guid + "." + address.Split('.')[address.Split('.').Length - 1];


            var memory = new MemoryStream();
            using (var stream = new FileStream(address, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/pdf", Path.GetFileName("ali.pdf"));
        }
    }
}
