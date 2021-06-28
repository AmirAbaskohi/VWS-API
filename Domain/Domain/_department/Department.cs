using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._file;
using vws.web.Domain._project;
using vws.web.Domain._team;

namespace vws.web.Domain._department
{
    [Table("Department_Department")]
    public class Department
    {
        public Department()
        {
            DepartmentMembers = new HashSet<DepartmentMember>();
            ProjectDepartments = new HashSet<ProjectDepartment>();
        }

        public int Id { get; set; }

        public Guid Guid { get; set; }

        public int TeamId { get; set; }

        [MaxLength(500)]
        public string Name { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        [MaxLength(6)]
        public string Color { get; set; }

        public bool IsDeleted { get; set; }

        public Guid CreatedBy { get; set; }

        public Guid ModifiedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }

        [ForeignKey("DepartmentImage")]
        public int? DepartmentImageId { get; set; }

        public Guid? DepartmentImageGuid { get; set; }

        public virtual FileContainer DepartmentImage { get; set; }

        public virtual Team Team { get; set; }

        public virtual ICollection<DepartmentMember> DepartmentMembers { get; set; }

        public virtual ICollection<ProjectDepartment> ProjectDepartments { get; set; }
    }
}
