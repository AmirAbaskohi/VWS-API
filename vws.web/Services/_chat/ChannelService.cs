using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._department;
using vws.web.Domain._project;
using vws.web.Domain._team;
using vws.web.Enums;
using vws.web.Models._chat;

namespace vws.web.Services._chat
{
    public class ChannelService : IChannelService
    {
        private readonly IVWS_DbContext vwsDbContext;
        private readonly UserManager<ApplicationUser> userManager;
        public ChannelService(IVWS_DbContext _vwsDbContext, UserManager<ApplicationUser> _userManager)
        {
            userManager = _userManager;
            vwsDbContext = _vwsDbContext;
        }

        public async Task<List<ChannelResponseModel>> GetUserChannels(Guid userId)
        {
            List<ChannelResponseModel> channelResponseModels = new List<ChannelResponseModel>();

            List<Team> userTeams = vwsDbContext.GetUserTeams(userId).ToList();
            List<Project> userProjects = vwsDbContext.GetUserProjects(userId).ToList();
            List<Department> userDepartments = vwsDbContext.GetUserDepartments(userId).ToList();

            List<UserProfile> userTeamMates = vwsDbContext.TeamMembers
                .Include(teamMember => teamMember.UserProfile)
                .Where(teamMember => userTeams.Select(userTeam => userTeam.Id).Contains(teamMember.TeamId) && !teamMember.IsDeleted)
                .Select(teamMember => teamMember.UserProfile).Distinct().ToList();
            userTeamMates.Remove(await vwsDbContext.GetUserProfileAsync(userId));

            foreach (var userTeamMate in userTeamMates)
            {
                channelResponseModels.Add(new ChannelResponseModel
                {
                    Guid = userTeamMate.UserId,
                    ChannelTypeId = (byte)SeedDataEnum.ChannelTypes.Private,
                    LogoUrl = "http://app.seventask.com/assets/Images/Chat/DefaultAvatars/User.jpg",
                    Title = (await userManager.FindByIdAsync(userTeamMate.UserId.ToString())).UserName,
                    IsMuted = false,
                    IsPinned = false,
                    EvenOrder = 0,
                    LastTransactionDateTime = new DateTime()
                });
            }

            channelResponseModels.AddRange(userTeams.Select(userTeam => new ChannelResponseModel
            {
                Guid = userTeam.Guid,
                ChannelTypeId = (byte)SeedDataEnum.ChannelTypes.Team,
                LogoUrl = "http://app.seventask.com/assets/Images/Chat/DefaultAvatars/Team.jpg",
                Title = userTeam.Name,
                IsMuted = false,
                IsPinned = false,
                EvenOrder = 0,
                LastTransactionDateTime = new DateTime()
            }));

            channelResponseModels.AddRange(userProjects.Select(userProject => new ChannelResponseModel
            {
                Guid = userProject.Guid,
                ChannelTypeId = (byte)SeedDataEnum.ChannelTypes.Project,
                LogoUrl = "http://app.seventask.com/assets/Images/Chat/DefaultAvatars/Project.jpg",
                Title = userProject.Name,
                IsMuted = false,
                EvenOrder = 0,
                LastTransactionDateTime = new DateTime()
            }));

            channelResponseModels.AddRange(userDepartments.Select(userDepartment => new ChannelResponseModel
            {
                Guid = userDepartment.Guid,
                ChannelTypeId = (byte)SeedDataEnum.ChannelTypes.Department,
                LogoUrl = "http://app.seventask.com/assets/Images/Chat/DefaultAvatars/Department.jpg",
                Title = userDepartment.Name,
                IsMuted = false,
                EvenOrder = 0,
                LastTransactionDateTime = new DateTime()
            }));

            return channelResponseModels;
        }

        public bool HasUserAccessToChannel(Guid userId, Guid channelId, byte channelTypeId)
        {
            List<Team> userTeams = vwsDbContext.GetUserTeams(userId).ToList();

            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                List<Guid> userTeamMates = vwsDbContext.TeamMembers
                .Where(teamMember => userTeams.Select(userTeam => userTeam.Id).Contains(teamMember.TeamId) && !teamMember.IsDeleted)
                .Select(teamMember => teamMember.UserProfileId).Distinct().ToList();
                userTeamMates.Remove(userId);

                return userTeamMates.Contains(channelId);
            }

            else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Team)
                return userTeams.Select(team => team.Guid).Contains(channelId);

            else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Project)
                return vwsDbContext.GetUserProjects(userId).Select(project => project.Guid).Contains(channelId);

            else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Department)
                return vwsDbContext.GetUserDepartments(userId).Select(project => project.Guid).Contains(channelId);

            return false;
        }

        public bool DoesChannelExist(Guid channelId, byte channelTypeId)
        {
            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
                return vwsDbContext.UserProfiles.Any(user => user.UserId == channelId);

            else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Team)
                return vwsDbContext.Teams.Any(team => team.Guid == channelId && !team.IsDeleted);

            else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Project)
                return vwsDbContext.Projects.Any(project => project.Guid == channelId && !project.IsDeleted);

            else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Department)
                return vwsDbContext.Departments.Any(department => department.Guid == channelId && !department.IsDeleted);

            return false;
        }
    }
}
