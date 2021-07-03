using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._base;

namespace vws.web.Domain._project
{
    [Table("Project_UserProjectOrder")]
    public class UserProjectOrder
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public Guid UserProfileId { get; set; }

        public int Order { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        public virtual Project Project { get; set; }
    }
}
