using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._version
{
    [Table("Version_VersionLog")]
    public class VersionLog
    {
        public int Id { get; set; }

        public int VersionId { get; set; }

        public string Log { get; set; }

        public string ImageName { get; set; }

        public int Order { get; set; }

        public virtual Version Version { get; set; }
    }
}
