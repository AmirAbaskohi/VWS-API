using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._team
{
    [Table("Team_TeamHistoryParameter")]
    public class TeamHistoryParameter
    {
        public long Id { get; set; }

        public byte ActivityParameterTypeId { get; set; }

        public long TeamHistoryId { get; set; }

        public string Body { get; set; }

        public virtual ActivityParameterType ActivityParameterType { get; set; }

        public virtual TeamHistory TeamHistory { get; set; }
    }
}
