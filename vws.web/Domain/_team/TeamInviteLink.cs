using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._team
{
    [Table("Team_TeamInviteLink")]
    public class TeamInviteLink
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public Guid LinkGuid { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid ModifiedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public bool IsRevoked { get; set; } 
    }
}
