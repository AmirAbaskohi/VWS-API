using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._team
{
    public class TeamExcludingUsersAndDepartmentsResponseModel
    {
        public int Id { get; set; }
        public byte TeamTypeId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public Guid Guid { get; set; }
        public Guid? TeamImageGuid { get; set; }
        public long NumberOfTasks { get; set; }
        public int NumberOfDepartments { get; set; }
        public int NumberOfProjects { get; set; }
        public int NumberOfMembers { get; set; }
    }
}
