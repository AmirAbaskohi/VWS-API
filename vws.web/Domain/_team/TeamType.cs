using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._team
{
    [Table("Team_TeamType")]
    public class TeamType
    {
        public TeamType()
        {
            Teams = new HashSet<Team>();
        }

        public byte Id { get; set; }

        [MaxLength(1000)]
        public string Name { get; set; }

        public virtual ICollection<Team> Teams { get; set; }
    }
}