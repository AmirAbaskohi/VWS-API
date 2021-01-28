using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Models;

namespace vws.web.Repositories
{
   public interface IFileManager
    {
        public Task<ResponseModel<Domain._file.File>> WriteFile(IFormFile file, Guid userId, string address, List<string> allowedExtensions = null);
        public void DeleteFile(string path);
    }
}
