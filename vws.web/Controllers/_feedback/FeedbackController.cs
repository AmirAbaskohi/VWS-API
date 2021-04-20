using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Controllers._feedback
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class FeedbackController : BaseController
    {
        [HttpPost]
        [Authorize]
        [Route("sendFeedback")]
        public IActionResult SendFeedback(string title, string description, IFormFile attachment)
        {
            return Ok("hello");
        }
    }
}
