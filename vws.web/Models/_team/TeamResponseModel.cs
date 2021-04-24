using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Models._department;

namespace vws.web.Models._team
{
    public class TeamResponseModel : TeamExcludingUsersAndDepartmentsResponseModel
    {
        public List<UserModel> Users { get; set; }
        public List<DepartmentResponseModel> Departments { get; set; }
    }
}
