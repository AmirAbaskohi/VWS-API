using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;

namespace vws.web.Repositories
{
    public class FileManager : IFileManager
    {
        private IVWS_DbContext vwsDbContext;

        public FileManager(IVWS_DbContext _vwsDbContext)
        {
            vwsDbContext = _vwsDbContext;
        }
        public async Task<bool> WriteFile(IFormFile file, Guid userId, string address)
        {
            bool isSaveSuccess = false;
            string fileName;
            try
            {
                var extension = file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
                Guid fileGuid = Guid.NewGuid();
                fileName = fileGuid.ToString() + "." + extension;

                var pathBuilt = Path.Combine(Directory.GetCurrentDirectory(), $"Upload{Path.DirectorySeparatorChar}" + address );

                if (!Directory.Exists(pathBuilt))
                {
                    Directory.CreateDirectory(pathBuilt);
                }

                var path = Path.Combine(Directory.GetCurrentDirectory(), $"Upload{Path.DirectorySeparatorChar}" + address,
                   fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var newFile = new Domain._file.File()
                {
                    FileId = fileGuid,
                    Address = path,
                    Extension = extension,
                    Name = file.FileName,
                    UploadedBy = userId
                };

                isSaveSuccess = true;

                await vwsDbContext.AddFileAsync(newFile);
                vwsDbContext.Save();
            }
            catch (Exception e)
            {
                //log error
            }

            return isSaveSuccess;
        }
    }
}
