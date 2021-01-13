using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._team;

namespace vws.web.Domain._department
{
    [Table("Department_Department")]
    public class Department
    {
        public Department()
        {
            DepartmentMembers = new HashSet<DepartmentMember>();
        }

        public int Id { get; set; }

        public int TeamId { get; set; }

        [MaxLength(500)]
        public string Name { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        [MaxLength(6)]
        public string Color { get; set; }

        public virtual Team Team { get; set; }

        public virtual ICollection<DepartmentMember> DepartmentMembers { get; set; }

    }
}
