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
        public string Description { get; set; }
        public Guid Guid { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public Guid? DepartmentImageGuid { get; set; }
        public List<UserModel> DepartmentMembers { get; set; }
    }
}
