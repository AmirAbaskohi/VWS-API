using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._team
{
    public class AddMembersToTeamModel
    {
        [Required]
        public int TeamId { get; set; }
        [Required]
        public List<Guid> Users { get; set; }
        [Required]
        public List<string> EmailsForInvite { get; set; }
    }
}
