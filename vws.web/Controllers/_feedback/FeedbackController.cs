using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Domain._feedback;
using vws.web.Domain._file;
using vws.web.Models;
using vws.web.Models._account;
using vws.web.Repositories;

namespace vws.web.Controllers._feedback
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class FeedbackController : BaseController
    {
        #region Feilds
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly IStringLocalizer<FeedbackController> _localizer;
        private readonly IFileManager _fileManager; 
        #endregion

        #region Ctor
        public FeedbackController(IVWS_DbContext vwsDbContext, IStringLocalizer<FeedbackController> localizer,
            IFileManager fileManager)
        {
            _vwsDbContext = vwsDbContext;
            _localizer = localizer;
            _fileManager = fileManager;
        }
        #endregion

        [HttpPost]
        [Authorize]
        [Route("sendFeedback")]
        public async Task<IActionResult> SendFeedback(IFormFile attachment)
        {
            var response = new ResponseModel();

            string title = "";
            string description = "";

            try
            {
                title = Request.Form.First(e => e.Key == "title").Value.ToString();
                description = Request.Form.First(e => e.Key == "description").Value.ToString();
            }
            catch(Exception)
            {
                response.AddError(_localizer["Invalid form."]);
                response.Message = "Invalid form.";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (String.IsNullOrEmpty(title) || title.Length > 250)
            {
                response.Message = "Invalid model";
                response.AddError(_localizer["Title can not be empty or tp have more than 250 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            if (!String.IsNullOrEmpty(description) && description.Length > 1000)
            {
                response.Message = "Invalid model";
                response.AddError(_localizer["Description can not be empty or tp have more than 1000 characters."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            string[] types = { "png", "jpg", "jpeg", "mov", "mp4" };

            var files = Request.Form.Files.ToList();

            Guid userId = LoggedInUserId.Value;

            if (files.Count > 1)
            {
                response.AddError(_localizer["There is more than one file."]);
                response.Message = "Too many files passed";
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }
            var uploadedAttachment = files.Count == 0 ? attachment : files[0];

            FileContainer attachmentContainer = null;

            if (uploadedAttachment != null)
            {
                var time = DateTime.Now;
                attachmentContainer = new FileContainer
                {
                    ModifiedOn = time,
                    CreatedOn = time,
                    CreatedBy = userId,
                    ModifiedBy = userId,
                    Guid = Guid.NewGuid()
                };
                await _vwsDbContext.AddFileContainerAsync(attachmentContainer);
                _vwsDbContext.Save();
                var fileResponse = await _fileManager.WriteFile(uploadedAttachment, userId, "feedbacks", attachmentContainer.Id, types.ToList());
                if (fileResponse.HasError)
                {
                    foreach (var error in fileResponse.Errors)
                        response.AddError(_localizer[error]);
                    response.Message = "Error in writing file";
                    _vwsDbContext.DeleteFileContainer(attachmentContainer);
                    _vwsDbContext.Save();
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
                attachmentContainer.RecentFileId = fileResponse.Value.Id;
                _vwsDbContext.Save();
            }

            _vwsDbContext.AddFeedBack(new FeedBack()
            {
                AttachmentGuid = uploadedAttachment == null ? null : (Guid?)attachmentContainer.Guid,
                AttachmentId = uploadedAttachment == null ? null : (int?)attachmentContainer.Id,
                Description = description,
                Title = title,
                UserProfileId = userId
            });
            _vwsDbContext.Save();

            response.Message = "Feedback sent successfully!";
            return Ok(response);
        }
    }
}
