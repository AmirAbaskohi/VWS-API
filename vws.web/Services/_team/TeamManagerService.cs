using Microsoft.AspNetCore.Identity;
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
        private readonly IVWS_DbContext vwsDbContext;
        private readonly UserManager<ApplicationUser> userManager;

        public TeamManagerService(IVWS_DbContext _vwsDbContext)
        {
            vwsDbContext = _vwsDbContext;
        }

        public async Task<Team> CreateTeam(TeamModel model, Guid userId)
        {
            var creationTime = DateTime.Now;

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
            await vwsDbContext.AddTeamAsync(newTeam);
            vwsDbContext.Save();

            var newTeamMember = new TeamMember()
            {
                TeamId = newTeam.Id,
                UserProfileId = userId,
                CreatedOn = DateTime.Now
            };

            await vwsDbContext.AddTeamMemberAsync(newTeamMember);
            vwsDbContext.Save();

            foreach (var user in model.Users)
            {
                await vwsDbContext.AddTeamMemberAsync(new TeamMember()
                {
                    CreatedOn = creationTime,
                    IsDeleted = false,
                    TeamId = newTeam.Id,
                    UserProfileId = user
                });
            }
            vwsDbContext.Save();

            return newTeam;
        }

        public async Task<List<UserModel>> GetTeamMembers(int teamId)
        {
            var result = new List<UserModel>();

            var members = vwsDbContext.TeamMembers.Where(member => member.TeamId == teamId && !member.IsDeleted)
                                                  .Select(member => member.UserProfileId);

            foreach (var member in members)
            {
                UserProfile userProfile = await vwsDbContext.GetUserProfileAsync(member);
                result.Add(new UserModel()
                {
                    UserId = member,
                    NickName = userProfile.NickName,
                    ProfileImageGuid = userProfile.ProfileImageGuid
                });
            }

            return result;
        }
    }
}
