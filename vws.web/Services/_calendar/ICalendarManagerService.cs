using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Models;
using vws.web.Models._project;
using vws.web.Models._team;

namespace vws.web.Services._calender
{
    public interface ICalendarManagerService
    {
        public Task<List<UserModel>> GetEventUsers(int id);

        public List<ProjectSummaryResponseModel> GetEventProjects(int id);

        public TeamSummaryResponseModel GetEventTeam(int id);
    }
}
