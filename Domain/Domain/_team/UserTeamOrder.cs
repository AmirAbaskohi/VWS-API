using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._base;

namespace vws.web.Domain._team
{
    [Table("Team_UserTeamOrder")]
    public class UserTeamOrder
    {
        public int Id { get; set; }

        public int TeamId { get; set; }

        public Guid UserProfileId { get; set; }

        public int Order { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        public virtual Team Team { get; set; }
    }
}
