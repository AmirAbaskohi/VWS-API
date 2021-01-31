using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._file
{
    [Table("File_File")]
    public class File
    {
        public Guid Id { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public Guid UploadedBy { get; set; }
        public int FileContainerId { get; set; }
    }
}
