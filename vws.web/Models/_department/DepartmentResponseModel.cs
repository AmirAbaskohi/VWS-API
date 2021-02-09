using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._department
{
    public class DepartmentResponseModel
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public Guid Guid { get; set; }
    }
}
