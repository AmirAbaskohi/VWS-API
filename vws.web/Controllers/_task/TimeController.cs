using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;

namespace vws.web.Controllers._task
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class TimeController : BaseController
    {
        #region Feilds
        private readonly IVWS_DbContext _vwsDbContext;
        #endregion

        #region Ctor
        public TimeController(IVWS_DbContext vwsDbContext)
        {
            _vwsDbContext = vwsDbContext;
        }
        #endregion
    }
}
