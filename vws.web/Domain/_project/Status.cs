using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._project
{
    [Table("Project_Status")]
    public class ProjectStatus
    {
        public ProjectStatus()
        {
            Projects = new HashSet<Project>();
        }

        public byte Id { get; set; }

        [MaxLength(1000)]
        public string Name { get; set; }

        public virtual ICollection<Project> Projects { get; set; }
    }
}