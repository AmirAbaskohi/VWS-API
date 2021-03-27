using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Models._department;

namespace vws.web.Models._team
{
    public class TeamModel
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        [Required]
        public List<Guid> Users { get; set; }
        [Required]
        public List<DepartmentBaseModel> Departments { get; set; }
        [Required]
        public List<string> EmailsForInvite { get; set; }
    }
}
