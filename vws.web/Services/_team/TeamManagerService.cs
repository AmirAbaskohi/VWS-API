using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._team;
using vws.web.Enums;
using vws.web.Models;
using vws.web.Models._team;

namespace vws.web.Services._team
{
    public class TeamManagerService : ITeamManagerService
    {
        private readonly IVWS_DbContext _vwsDbContext;
        public TeamManagerService(IVWS_DbContext vwsDbContext)
        {
            _vwsDbContext = vwsDbContext;
        }

        public async Task<Team> CreateTeam(TeamModel model, Guid userId)
        {
            var creationTime = DateTime.UtcNow;

            var newTeam = new Team()
            {
                Name = model.Name,
                TeamTypeId = (byte)SeedDataEnum.TeamTypes.Team,
                Description = model.Description,
                Color = model.Color,
                CreatedOn = creationTime,
                CreatedBy = userId,
                ModifiedOn = creationTime,
                ModifiedBy = userId,
                Guid = Guid.NewGuid()
            };
            await _vwsDbContext.AddTeamAsync(newTeam);
            _vwsDbContext.Save();

            model.Users.Add(userId);
            model.Users = model.Users.Distinct().ToList();

            foreach (var user in model.Users)
            {
                await _vwsDbContext.AddTeamMemberAsync(new TeamMember()
                {
                    CreatedOn = creationTime,
                    IsDeleted = false,
                    TeamId = newTeam.Id,
                    UserProfileId = user
                });
                if (userId != user)
                    _vwsDbContext.AddUsersActivity(new UsersActivity() { Time = creationTime, TargetUserId = user, OwnerUserId = userId });
            }
            _vwsDbContext.Save();

            return newTeam;
        }

        public async Task<List<UserModel>> GetTeamMembers(int teamId)
        {
            var result = new List<UserModel>();

            var members = _vwsDbContext.TeamMembers.Where(member => member.TeamId == teamId && !member.IsDeleted)
                                                  .Select(member => member.UserProfileId);

            foreach (var member in members)
            {
                UserProfile userProfile = await _vwsDbContext.GetUserProfileAsync(member);
                result.Add(new UserModel()
                {
                    UserId = member,
                    NickName = userProfile.NickName,
                    ProfileImageGuid = userProfile.ProfileImageGuid
                });
            }

            return result;
        }

        public List<Team> GetAllUserTeams(Guid userId)
        {
            return _vwsDbContext.TeamMembers.Include(teamMemeber => teamMemeber.Team)
                                            .Where(teamMemeber => teamMemeber.UserProfileId == userId && !teamMemeber.IsDeleted && !teamMemeber.Team.IsDeleted)
                                            .Select(teamMemeber => teamMemeber.Team).ToList();
        }

        public List<Guid> GetUserTeammates(Guid userId)
        {
            List<Team> userTeams = _vwsDbContext.GetUserTeams(userId).ToList();
            return _vwsDbContext.TeamMembers
                               .Where(teamMember => userTeams.Select(userTeam => userTeam.Id).Contains(teamMember.TeamId) && !teamMember.IsDeleted)
                               .Select(teamMember => teamMember.UserProfileId).Distinct().Where(id => id != userId).ToList();
        }
    }
}
