using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;

namespace vws.web.Repositories
{
   public interface IFileManager
    {
        public Task<bool> WriteFile(IFormFile file, Guid userId, string address);
    }
}
