using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models
{
    public class TeamInviteLinkResponseModel
    {
        public int Id { get; set; }
        public string TeamName { get; set; }
        public bool IsInvoked { get; set; }
        public string LinkGuid { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }
}
