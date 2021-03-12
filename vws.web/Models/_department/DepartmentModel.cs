using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._department
{
    public class DepartmentModel : DepartmentBaseModel
    {
        [Required]
        public int TeamId { get; set; }
    }
}
