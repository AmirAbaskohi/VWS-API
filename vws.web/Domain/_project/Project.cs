using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        public virtual ProjectStatus Status { get; set; }

        public virtual ICollection<ProjectMember> ProjectMembers { get; set; }

    }
}
