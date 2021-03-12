using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._department
{
    public class DepartmentBaseModel
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        [Required]
        public List<Guid> Users { get; set; }
    }
}
