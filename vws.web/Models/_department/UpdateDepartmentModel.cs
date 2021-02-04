using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._department
{
    public class UpdateDepartmentModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public int TeamId { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
    }
}
