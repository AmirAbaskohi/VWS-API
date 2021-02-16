using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._project
{
    public class UpdateProjectModel : ProjectModel
    {
        [Required]
        public int Id { get; set; }
    }
}
