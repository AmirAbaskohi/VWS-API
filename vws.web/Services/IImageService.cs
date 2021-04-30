using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._file;

namespace vws.web.Services
{
    public interface IImageService
    {
        public bool IsImage(IFormFile postedFile);
        public void SaveInOtherQualities(File fileResponse);
        public bool IsImageSquare(IFormFile image);
    }
}
