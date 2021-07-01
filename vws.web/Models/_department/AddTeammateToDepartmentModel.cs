using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._department
{
    public class AddTeammateToDepartmentModel
    {
        [Required]
        public int DepartmentId { get; set; }
        [Required]
        public List<Guid> Users { get; set; }
    }
}
