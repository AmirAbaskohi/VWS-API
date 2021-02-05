using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Models;

namespace vws.web.Repositories
{
    public class FileManager : IFileManager
    {
        private IVWS_DbContext vwsDbContext;
        private IConfiguration configuration;

        public FileManager(IVWS_DbContext _vwsDbContext, IConfiguration _configuration)
        {
            vwsDbContext = _vwsDbContext;
            configuration = _configuration;
        }
        public async Task<ResponseModel<Domain._file.File>> WriteFile(IFormFile file, Guid userId, string address, int fileContainerId, List<string> allowedExtensions = null)
        {
            var notAllowedExtensions = configuration["Files:RestrictedExtensions"].Split(',');

            var response = new ResponseModel<Domain._file.File>();

            string fileName;

            if (!file.FileName.Contains('.'))
            {
                response.AddError("Your file does not have extension.");
                response.Message = "Invalid file name";
                return response;
            }

            var extension = file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
            extension = extension.ToLower();

            if (allowedExtensions != null && (!allowedExtensions.Any(ext => ext == extension) || notAllowedExtensions.Any(ext => ext == extension)))
            {
                response.AddError("File extension is not allowed.");
                response.Message = "Invalid extension";
                return response;
            }

            if (file.Length > Int64.Parse(configuration["Files:FileMaxSizeInBytes"]))
            {
                response.AddError("File size is not allowed.");
                response.Message = "Invalid file size";
                return response;
            }

            Guid fileGuid = Guid.NewGuid();
            fileName = fileGuid.ToString() + "." + extension;

            var pathBuilt = Path.Combine(Directory.GetCurrentDirectory(), $"Upload{Path.DirectorySeparatorChar}" + address);
            var path = Path.Combine(Directory.GetCurrentDirectory(), $"Upload{Path.DirectorySeparatorChar}" + address,
                   fileName);
            try
            {
                if (!Directory.Exists(pathBuilt))
                {
                    Directory.CreateDirectory(pathBuilt);
                }

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var newFile = new Domain._file.File()
                {
                    Id = fileGuid,
                    Address = path,
                    Extension = extension,
                    Name = file.FileName,
                    UploadedBy = userId,
                    FileContainerId = fileContainerId
                };

                await vwsDbContext.AddFileAsync(newFile);
                vwsDbContext.Save();
                response.Value = newFile;
                response.Message = "File wrote successfully";
            }
            catch (Exception e)
            {
                response.AddError("Error in writing files.");
                response.Message = "Error in writing file";
                if (File.Exists(path))
                    File.Delete(path);
            }

            return response;
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }
    }
}
