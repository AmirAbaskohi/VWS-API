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

        [ForeignKey("TagetUser")]
        public Guid TargetUserId { get; set; }

        [ForeignKey("OwnerUser")]
        public Guid OwnerUserId { get; set; }

        public DateTime Time { get; set; }

        public virtual UserProfile OwnerUser { get; set; }

        public virtual UserProfile TagetUser { get; set; }
    }
}
