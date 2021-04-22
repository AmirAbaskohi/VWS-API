using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._project
{
    public class ProjectSummaryResponseModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public Guid? ProjectImageId { get; set; }
    }
}
