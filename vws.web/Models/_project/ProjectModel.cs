using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._project
{
    public class ProjectModel
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [MaxLength(6)]
        public string Color { get; set; }
        public int? TeamId { get; set; }
        [Required]
        public List<Guid> Users { get; set; }
        [Required]
        public List<int> DepartmentIds { get; set; }
    }
}
