using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._team
{
    public class TeamSummaryResponseModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public Guid? TeamImageId { get; set; }
    }
}
