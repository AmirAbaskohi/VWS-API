using System;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._base;

namespace vws.web.Domain._department
{
    [Table("Department_DepartmentMember")]
    public class DepartmentMember
    {
        public int Id { get; set; }

        public int DepartmentId { get; set; }

        public Guid UserProfileId { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? DeletedOn { get; set; }
        public bool IsDeleted { get; set; }

        public virtual Department Department { get; set; }

        public virtual UserProfile UserProfile { get; set; }

    }
}