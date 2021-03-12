using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;

namespace vws.web.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IVWS_DbContext vwsDbContext;

        public PermissionService(IVWS_DbContext _vwsDbContext)
        {
            vwsDbContext = _vwsDbContext;
        }

        public bool HasAccessToProject(Guid userId, int projectId)
        {
            var selectedProject = vwsDbContext.Projects.Include(project => project.ProjectDepartments)
                                                       .FirstOrDefault(project => project.Id == projectId);

            if (selectedProject.TeamId != null)
            {
                List<Guid> projectUsers = new List<Guid>();

                if (selectedProject.ProjectDepartments.Count == 0)
                {
                    projectUsers = vwsDbContext.TeamMembers.Where(teamMember => teamMember.TeamId == (int)selectedProject.TeamId &&
                                                                                                     !teamMember.IsDeleted)
                                                           .Select(teamMember => teamMember.UserProfileId)
                                                           .ToList();
                }

                else
                {
                    foreach (var departmentId in selectedProject.ProjectDepartments.Select(pd => pd.DepartmentId))
                    {
                        projectUsers.AddRange(vwsDbContext.DepartmentMembers.Where(departmentMember => departmentMember.DepartmentId == departmentId &&
                                                                                                       !departmentMember.IsDeleted)
                                                                            .Select(departmentMember => departmentMember.UserProfileId)
                                                                            .ToList());
                    }
                }

                return projectUsers.Contains(userId);
            }

            var selectedProjectMember = vwsDbContext.ProjectMembers.FirstOrDefault(projectMember => !projectMember.IsDeleted &&
                                                                                                    projectMember.IsPermittedByCreator == true &&
                                                                                                    projectMember.ProjectId == projectId &&
                                                                                                    projectMember.UserProfileId == userId);

            return selectedProjectMember != null;
        }

        public bool HasAccessToDepartment(Guid userId, int departmentId)
        {
            return vwsDbContext.DepartmentMembers.Any(departmentMember => departmentMember.UserProfileId == userId &&
                                                                          departmentMember.DepartmentId == departmentId &&
                                                                          !departmentMember.IsDeleted);
        }
        public bool HasAccessToTeam(Guid userId, int teamId)
        {
            return vwsDbContext.TeamMembers.Any(teamMember => teamMember.UserProfileId == userId &&
                                                              teamMember.TeamId == teamId &&
                                                              !teamMember.IsDeleted);
        }

        public bool HasAccessToTask(Guid userId, long taskId)
        {
            bool hasAccess = vwsDbContext.TaskAssigns.Any(assign => assign.UserProfileId == userId && assign.GeneralTaskId == taskId && !assign.IsDeleted);

            if (hasAccess == true)
                return true;

            var selectedTask = vwsDbContext.GeneralTasks.FirstOrDefault(task => task.Id == taskId);
            return selectedTask.CreatedBy == userId;
        }

        public List<Guid> GetUsersHaveAccessToProject(int projectId)
        {
            var selectedProject = vwsDbContext.Projects.Include(project => project.ProjectDepartments)
                                                       .FirstOrDefault(project => project.Id == projectId);

            if (selectedProject.TeamId != null)
            {
                List<Guid> projectUsers = new List<Guid>();

                if (selectedProject.ProjectDepartments.Count == 0)
                {
                    projectUsers = vwsDbContext.TeamMembers.Where(teamMember => teamMember.TeamId == (int)selectedProject.TeamId &&
                                                                                                     !teamMember.IsDeleted)
                                                           .Select(teamMember => teamMember.UserProfileId)
                                                           .ToList();
                }

                else
                {
                    foreach (var departmentId in selectedProject.ProjectDepartments.Select(pd => pd.DepartmentId))
                    {
                        projectUsers.AddRange(vwsDbContext.DepartmentMembers.Where(departmentMember => departmentMember.DepartmentId == departmentId &&
                                                                                                       !departmentMember.IsDeleted)
                                                                            .Select(departmentMember => departmentMember.UserProfileId)
                                                                            .ToList());
                    }
                }

                return projectUsers;
            }

            return vwsDbContext.ProjectMembers.Where(projectMember => !projectMember.IsDeleted &&
                                                                      projectMember.IsPermittedByCreator == true &&
                                                                      projectMember.ProjectId == projectId)
                                              .Select(projectMember => projectMember.UserProfileId)
                                              .ToList();
        }
        public List<Guid> GetUsersHaveAccessToDepartment(int departmentId)
        {
            return vwsDbContext.DepartmentMembers.Where(departmentMember => departmentMember.DepartmentId == departmentId &&
                                                                      !departmentMember.IsDeleted)
                                                 .Select(teamMember => teamMember.UserProfileId)
                                                 .ToList();
        }
        public List<Guid> GetUsersHaveAccessToTeam(int teamId)
        {
            return vwsDbContext.TeamMembers.Where(teamMember => teamMember.TeamId == teamId &&
                                                                !teamMember.IsDeleted)
                                           .Select(teamMember => teamMember.UserProfileId)
                                           .ToList();
        }
    }
}
