using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._department;
using vws.web.Domain._file;
using vws.web.Domain._team;

namespace vws.web.Domain._project
{
    [Table("Project_Project")]
    public class Project
    {
        public Project()
        {
            ProjectMembers = new HashSet<ProjectMember>();
        }

        public int Id { get; set; }

        public Guid Guid { get; set; }

        [MaxLength(500)]
        public string Name { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        public byte StatusId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(6)]
        public string Color { get; set; }
        public bool IsDeleted { get; set;}

        public Guid CreateBy { get; set; }
        public Guid ModifiedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        [ForeignKey("Department")]
        public int? DepartmentId { get; set; }
        [ForeignKey("Team")]
        public int? TeamId { get; set; }
        [ForeignKey("ProjectImage")]
        public int? ProjectImageId { get; set; }
        public virtual FileContainer ProjectImage { get; set; }

        public virtual Department Department { get; set; }

        public virtual Team Team { get; set; }

        public virtual ProjectStatus Status { get; set; }

        public virtual ICollection<ProjectMember> ProjectMembers { get; set; }

    }
}
