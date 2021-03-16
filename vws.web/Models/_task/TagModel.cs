using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class TagModel
    {
        public int? ProjectId { get; set; }
        public int? TeamId { get; set; }
        public string Title { get; set; }
        public string Color { get; set; }
    }
}
