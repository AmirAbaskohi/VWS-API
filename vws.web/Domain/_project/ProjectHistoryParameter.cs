using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._project
{
    [Table("Project_ProjectHistoryParameter")]
    public class ProjectHistoryParameter
    {
        public long Id { get; set; }

        public byte ActivityParameterTypeId { get; set; }

        public long ProjectHistoryId { get; set; }

        public string Body { get; set; }

        public virtual ActivityParameterType ActivityParameterType { get; set; }

        public virtual ProjectHistory ProjectHistory { get; set; }
    }
}
