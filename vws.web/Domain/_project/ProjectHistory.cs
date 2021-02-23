using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._project
{
    [Table("Project_ProjectHistory")]
    public class ProjectHistory
    {
        public long Id { get; set; }

        public int ProjectId { get; set; }

        public string Event { get; set; }

        public string CommaSepratedParameters { get; set; }

        public DateTime EventTime { get; set; }
    }
}
