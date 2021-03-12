using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Services
{
    public interface IPermissionService
    {
        public bool HasAccessToProject(Guid userId, int projectId);
        public bool HasAccessToDepartment(Guid userId, int departmentId);
        public bool HasAccessToTeam(Guid userId, int teamId);
        public bool HasAccessToTask(Guid userId, long taskId);
        public List<Guid> GetUsersHaveAccessToProject(int projectId);
        public List<Guid> GetUsersHaveAccessToDepartment(int departmentId);
        public List<Guid> GetUsersHaveAccessToTeam(int teamId);
    }
}
