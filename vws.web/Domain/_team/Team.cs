using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._department;

namespace vws.web.Domain._team
{
    [Table("Team_Team")]
    public class Team
    {

        public Team()
        {
            Departments = new HashSet<Department>();
        }

        public int Id { get; set; }

        public byte TeamTypeId { get; set; }

        [MaxLength(500)]
        [Required]
        public string Name { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        [MaxLength(6)]
        public string Color { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }

        public Guid CreatedBy { get; set; }

        public Guid ModifiedBy { get; set; }

        public virtual TeamType TeamType { get; set; }

        public virtual ICollection<Department> Departments { get; set; }


    }
}
