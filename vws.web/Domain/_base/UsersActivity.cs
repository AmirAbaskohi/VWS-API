using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._base
{
    [Table("Base_UsersActivity")]
    public class UsersActivity
    {
        public int Id { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public Guid UserProfileId { get; set; }

        public DateTime Time { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        public virtual UserProfile User { get; set; }
    }
}
