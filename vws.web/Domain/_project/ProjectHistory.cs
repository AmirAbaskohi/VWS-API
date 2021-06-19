using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._project
{
    [Table("Project_ProjectHistory")]
    public class ProjectHistory
    {
        public ProjectHistory()
        {
            ProjectHistoryParameters = new HashSet<ProjectHistoryParameter>();
        }

        public long Id { get; set; }

        public int ProjectId { get; set; }

        public string EventBody { get; set; }

        public DateTime EventTime { get; set; }

        public virtual Project Project { get; set; }

        public virtual ICollection<ProjectHistoryParameter> ProjectHistoryParameters { get; set; }
    }
}
