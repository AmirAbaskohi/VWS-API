using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._department;

namespace vws.web.Domain._project
{
    [Table("Project_ProjectDepartment")]
    public class ProjectDepartment
    {
        public int ProjectId { get; set; }

        public Project Project { get; set; }

        public int DepartmentId { get; set; }

        public Department Department { get; set; }
    }
}
