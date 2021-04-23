using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._calender
{
    public class UpdateTeamAndProjectModel
    {
        public int? TeamId { get; set; }
        [Required]
        public List<int> ProjectIds { get; set; }
    }
}
