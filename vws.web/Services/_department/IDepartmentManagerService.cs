using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._department;
using vws.web.Models;
using vws.web.Models._department;

namespace vws.web.Services._department
{
    public interface IDepartmentManagerService
    {
        public Task<Department> CreateDepartment(DepartmentModel department, Guid userId);
        public List<string> CheckDepartmentModel(DepartmentModel model);
        public Task AddUserToDepartment(Guid user, int departmentId);
        public Task<List<UserModel>> GetDepartmentMembers(int departmentId);
        public List<Department> GetAllUserDepartments(Guid userId);
    }
}
