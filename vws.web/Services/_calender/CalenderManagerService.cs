using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Models;
using vws.web.Models._project;
using vws.web.Models._team;

namespace vws.web.Services._calender
{
    public class CalenderManagerService : ICalenderManagerService
    {
        private readonly IVWS_DbContext _vwsDbContext;

        public CalenderManagerService(IVWS_DbContext vwsDbContext)
        {
            _vwsDbContext = vwsDbContext;
        }

        public async Task<List<UserModel>> GetEventUsers(int id)
        {
            var result = new List<UserModel>();

            var users = _vwsDbContext.EventUsers.Where(eventUser => eventUser.EventId == id).Select(eventUser => eventUser.UserProfileId);

            foreach (var user in users)
            {
                var selectedUser = await _vwsDbContext.GetUserProfileAsync(user);
                result.Add(new UserModel()
                {
                    NickName = selectedUser.NickName,
                    ProfileImageGuid = selectedUser.ProfileImageGuid,
                    UserId = selectedUser.UserId
                });
            }

            return result;
        }

        public List<ProjectSummaryResponseModel> GetEventProjects(int id)
        {
            var result = new List<ProjectSummaryResponseModel>();

            var projectIds = _vwsDbContext.EventProjects.Where(eventProject => eventProject.EventId == id).Select(eventProject => eventProject.ProjectId);

            foreach (var projectId in projectIds)
            {
                var selectedProject = _vwsDbContext.Projects.FirstOrDefault(project => project.Id == projectId);
                result.Add(new ProjectSummaryResponseModel()
                {
                    Id = selectedProject.Id,
                    Color = selectedProject.Color,
                    Name = selectedProject.Name,
                    ProjectImageId = selectedProject.ProjectImageGuid
                });
            }

            return result;
        }

        public TeamSummaryResponseModel GetEventTeam(int id)
        {
            var selectedEvent = _vwsDbContext.Events.Include(_event => _event.Team).FirstOrDefault(_event => _event.Id == id);
            if (selectedEvent.TeamId == null)
                return null;

            return new TeamSummaryResponseModel()
            {
                Id = selectedEvent.Team.Id,
                Color = selectedEvent.Team.Color,
                Name = selectedEvent.Team.Name,
                TeamImageId = selectedEvent.Team.TeamImageGuid
            };
        }
    }
}
