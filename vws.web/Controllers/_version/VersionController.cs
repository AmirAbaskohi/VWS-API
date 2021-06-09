using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Models;
using vws.web.Models._versions;

namespace vws.web.Controllers._version
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class VersionController : BaseController
    {
        #region Feilds
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly IStringLocalizer<VersionController> _localizer;
        #endregion

        #region Ctor
        public VersionController(IVWS_DbContext vwsDbContext, IStringLocalizer<VersionController> localizer)
        {
            _localizer = localizer;
            _vwsDbContext = vwsDbContext;
        }
        #endregion

        #region VersionAPIS
        [HttpGet]
        [Route("getLastVersion")]
        public IActionResult GetLastVersion()
        {
            var versions = _vwsDbContext.Versions;
            var response = new ResponseModel<Object>();

            if (versions.Count() == 0)
                return StatusCode(StatusCodes.Status204NoContent, response);

            var lastVersion = versions.FirstOrDefault(version => version.ReleaseDate == versions.Max(v => v.ReleaseDate));

            response.Value = new { Id = lastVersion.Id, Name = lastVersion.Name, ReleaseDate = lastVersion.ReleaseDate };
            response.Message = "Last version returned successfully";
            return Ok(response);
        }

        [HttpGet]
        [Route("getVersionLogs")]
        public IActionResult GetVersionLogs(int id)
        {
            var response = new ResponseModel<Object>();

            var selectedVersion = _vwsDbContext.Versions.Include(version => version.VersionLogs).FirstOrDefault(version => version.Id == id);
            if (selectedVersion == null)
            {
                response.Message = "Version not found!";
                response.AddError(_localizer["Version not found."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var logs = selectedVersion.VersionLogs.OrderBy(log => log.Order);

            response.Value = new
            {
                Id = selectedVersion.Id,
                Name = selectedVersion.Name,
                ReleaseDate = selectedVersion.ReleaseDate,
                Logs = logs.Select(log => new VersionLogModel()
                {
                    Log = _localizer[log.Key] == log.Key ? log.Log : _localizer[log.Key],
                    ImageUrl = String.IsNullOrEmpty(log.ImageName) ? null : $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}:/whatsnew/{log.ImageName}"
                })
            };
            response.Message = "Logs returned successfully!";
            return Ok(response);
        }
        #endregion
    }
}
