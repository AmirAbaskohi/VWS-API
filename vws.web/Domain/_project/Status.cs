using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._project
{
    [Table("Project_Status")]
    public class Status
    {
        public Status()
        {
            Projects = new HashSet<Project>();
        }

        public byte Id { get; set; }

        [MaxLength(1000)]
        public string NameMultiLang { get; set; }

        public virtual ICollection<Project> Projects { get; set; }
    }
}