using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._file
{
    public class FileUploadResponseModel
    {
        public FileUploadResponseModel()
        {
            UnsuccessfulFileUpload = new List<string>();
            SuccessfulFileUpload = new List<FileModel>();
        }
        public bool UploadedCompletely { get => UnsuccessfulFileUpload.Count == 0; }
        public List<string> UnsuccessfulFileUpload { get; set; }
        public List<FileModel> SuccessfulFileUpload { get; set; }
    }
}
