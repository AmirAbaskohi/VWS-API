using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._project
{
    public class AddUserToProjectModel
    {
        [Required]
        public List<Guid> Users { get; set; }
        [Required]
        public int ProjectId { get; set; }
    }
}
