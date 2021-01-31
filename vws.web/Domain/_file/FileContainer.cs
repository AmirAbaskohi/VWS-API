using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._file
{
    [Table("File_FileContainer")]
    public class FileContainer
    {
        public FileContainer()
        {
            Files = new HashSet<File>();
        }
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid ModifiedBy { get; set; }
        public Guid RecentFileId { get; set; }
        public virtual ICollection<File> Files { get; set; }
    }
}
