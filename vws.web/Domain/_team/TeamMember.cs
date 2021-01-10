using System;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._base;

namespace vws.web.Domain._team
{
    [Table("Team_TeamMember")]
    public class TeamMember
    {
        public int Id { get; set; }

        public int TeamId { get; set; }

        public Guid UserProfileId { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? DeletedOn { get; set; }

        public virtual Team Team { get; set; }

        public virtual UserProfile UserProfile { get; set; }

    }
}
