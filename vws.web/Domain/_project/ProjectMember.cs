using System;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._base;

namespace vws.web.Domain._project
{
    [Table("Project_ProjectMember")]
    public class ProjectMember
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public Guid UserProfileId { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime PermittedOn { get; set; }

        public DateTime? DeletedOn { get; set; }

        public bool IsDeleted { get; set; }

        public bool? IsPermittedByCreator { get; set; }

        public virtual Project Project { get; set; }

        public virtual UserProfile UserProfile { get; set; }
    }
}
