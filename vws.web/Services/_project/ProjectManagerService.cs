using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._project;
using vws.web.Models;
using vws.web.Services._team;

namespace vws.web.Services._project
{
    public class ProjectManagerService : IProjectManagerService
    {
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly ITeamManagerService _teamManager;

        public ProjectManagerService(IVWS_DbContext vwsDbContext, ITeamManagerService teamManager)
        {
            _vwsDbContext = vwsDbContext;
            _teamManager = teamManager;
        }

        public List<UserModel> GetProjectUsers(int id)
        {
            var selectedProject = _vwsDbContext.Projects.Include(project => project.ProjectDepartments)
                                                       .FirstOrDefault(project => project.Id == id);

            List<UserProfile> projectUsers = new List<UserProfile>();

            if (selectedProject.TeamId != null)
            {
                if (selectedProject.ProjectDepartments.Count == 0)
                {
                    projectUsers = _vwsDbContext.TeamMembers.Include(teamMember => teamMember.UserProfile)
                                                           .Where(teamMember => teamMember.TeamId == (int)selectedProject.TeamId &&
                                                                                                     !teamMember.IsDeleted)
                                                           .Select(teamMember => teamMember.UserProfile)
                                                           .ToList();
                }

                else
                {
                    foreach (var departmentId in selectedProject.ProjectDepartments.Select(pd => pd.DepartmentId))
                    {
                        projectUsers.AddRange(_vwsDbContext.DepartmentMembers.Include(departmentMember => departmentMember.UserProfile)
                                                                            .Where(departmentMember => departmentMember.DepartmentId == departmentId &&
                                                                                                       !departmentMember.IsDeleted)
                                                                            .Select(departmentMember => departmentMember.UserProfile)
                                                                            .ToList());
                    }
                }
            }

            else
            {
                projectUsers = _vwsDbContext.ProjectMembers.Include(projectMember => projectMember.UserProfile)
                                                          .Where(projectMember => !projectMember.IsDeleted &&
                                                                                  projectMember.IsPermittedByCreator == true &&
                                                                                  projectMember.ProjectId == id)
                                                          .Select(projectMember => projectMember.UserProfile)
                                                          .ToList();
            }

            List<UserModel> users = new List<UserModel>();

            foreach (var user in projectUsers)
            {
                users.Add(new UserModel()
                {
                    UserId = user.UserId,
                    ProfileImageGuid = user.ProfileImageGuid,
                    NickName = user.NickName
                });
            }

            return users.Distinct().ToList();
        }

        public List<Project> GetAllUserProjects(Guid userId)
        {
            List<Project> userProjects = new List<Project>();

            List<int> userTeams = _teamManager.GetAllUserTeams(userId).Select(team => team.Id).ToList();
            List<int> userDepartments = _vwsDbContext.DepartmentMembers.Include(departmentMember => departmentMember.Department)
                                                                      .Where(departmentMember => !departmentMember.IsDeleted &&
                                                                                                 departmentMember.UserProfileId == userId &&
                                                                                                 !departmentMember.Department.IsDeleted)
                                                                      .Select(departmentMember => departmentMember.DepartmentId)
                                                                      .ToList();

            userProjects.AddRange(_vwsDbContext.Projects.Include(project => project.ProjectDepartments)
                                                       .Where(project => project.IsDeleted == false &&
                                                                         project.TeamId != null &&
                                                                         project.ProjectDepartments.Count == 0 &&
                                                                         userTeams.Contains((int)project.TeamId))
                                                       .ToList());

            foreach (var project in _vwsDbContext.Projects.Include(project => project.ProjectDepartments))
            {
                if (project.IsDeleted == false && project.TeamId != null &&
                   project.ProjectDepartments.Count != 0 &&
                   userTeams.Contains((int)project.TeamId) &&
                   project.ProjectDepartments.Select(pd => pd.DepartmentId).Intersect(userDepartments).Any())
                {
                    userProjects.Add(project);
                }
            }

            userProjects.AddRange(_vwsDbContext.ProjectMembers.Include(projectMember => projectMember.Project)
                                                             .Where(projectMember => projectMember.UserProfileId == userId &&
                                                                                     !projectMember.IsDeleted &&
                                                                                     projectMember.IsPermittedByCreator == true &&
                                                                                     !projectMember.Project.IsDeleted)
                                                             .Select(projectMember => projectMember.Project));

            return userProjects;
        }

        public long GetNumberOfProjectTasks(int id)
        {
            return _vwsDbContext.GeneralTasks.Where(task => task.ProjectId == id && !task.IsDeleted).Count();
        }

        public double GetProjectSpentTime(int projectId)
        {
            double result = 0;

            var times = _vwsDbContext.TimeTracks.Include(timeTrack => timeTrack.GeneralTask)
                                                .Where(timeTrack => timeTrack.GeneralTask.ProjectId == projectId);

            foreach (var time in times)
            {
                if (time.TotalTimeInMinutes != null)
                    result += (double)time.TotalTimeInMinutes;
                else
                    result += (DateTime.UtcNow - time.StartDate).TotalMinutes;
            }

            return result;
        }

        public DateTime GetUserJoinDateTime(Guid userId, int projectId)
        {
            var selectedProject = _vwsDbContext.Projects.Include(project => project.ProjectDepartments)
                                                        .FirstOrDefault(project => project.Id == projectId);

            if (selectedProject == null || selectedProject.IsDeleted)
                return new DateTime();

            if (selectedProject.TeamId != null)
                return selectedProject.CreatedOn;

            var selectedProjectMember = _vwsDbContext.ProjectMembers.FirstOrDefault(projectMember => !projectMember.IsDeleted &&
                                                                                                    projectMember.IsPermittedByCreator == true &&
                                                                                                    projectMember.ProjectId == projectId &&
                                                                                                    projectMember.UserProfileId == userId);

            if (selectedProjectMember == null || selectedProjectMember.IsDeleted)
                return new DateTime();

            return selectedProjectMember.CreatedOn;
        }
    }
}
