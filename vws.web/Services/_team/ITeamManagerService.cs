using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._team;
using vws.web.Models;
using vws.web.Models._team;

namespace vws.web.Services._team
{
    public interface ITeamManagerService
    {
        public Task<Team> CreateTeam(TeamModel model, Guid userId);
        public Task<List<UserModel>> GetTeamMembers(int teamId);
        public List<Team> GetAllUserTeams(Guid userId);
    }
}
