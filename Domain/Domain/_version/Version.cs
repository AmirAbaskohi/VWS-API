using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._version
{
    [Table("Version_Version")]
    public class Version
    {
        public Version()
        {
            VersionLogs = new HashSet<VersionLog>();
        }

        public int Id { get; set; }

        public int Name { get; set; }

        public DateTime ReleaseDate { get; set; }

        public virtual ICollection<VersionLog> VersionLogs { get; set; }
    }
}
