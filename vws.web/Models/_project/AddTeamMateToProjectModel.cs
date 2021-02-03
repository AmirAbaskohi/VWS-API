using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._project
{
    public class AddTeamMateToProjectModel
    {
        [Required]
        public int ProjectId { get; set; }
        public int TeamId { get; set; }
        public string UserId { get; set; }
    }
}
