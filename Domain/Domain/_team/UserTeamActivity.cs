using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._base;

namespace vws.web.Domain._team
{
    [Table("Team_UserTeamActivity")]
    public class UserTeamActivity
    {
        public long Id { get; set; }

        public int TeamId { get; set; }

        public Guid UserProfileId { get; set; }

        public DateTime Time { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        public virtual Team Team { get; set; }
    }
}
