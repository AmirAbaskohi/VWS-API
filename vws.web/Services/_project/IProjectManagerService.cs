using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._project;
using vws.web.Models;

namespace vws.web.Services._project
{
    public interface IProjectManagerService
    {
        public List<UserModel> GetProjectUsers(int projectId);

        public List<Project> GetAllUserProjects(Guid userId);

        public long GetNumberOfProjectTasks(int id);
    }
}
