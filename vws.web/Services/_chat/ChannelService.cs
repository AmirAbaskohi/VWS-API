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
using vws.web.Services._department;
using vws.web.Services._project;
using vws.web.Services._team;

namespace vws.web.Services._chat
{
    public class ChannelService : IChannelService
    {
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly IProjectManagerService _projectManager;
        private readonly ITeamManagerService _teamManager;
        private readonly IDepartmentManagerService _departmentManager;

        public ChannelService(IVWS_DbContext vwsDbContext, IProjectManagerService projectManager,
            ITeamManagerService teamManager, IDepartmentManagerService departmentManager)
        {
            _vwsDbContext = vwsDbContext;
            _projectManager = projectManager;
            _teamManager = teamManager;
            _departmentManager = departmentManager;
        }

        public async Task<List<ChannelResponseModel>> GetUserChannels(Guid userId)
        {
            List<ChannelResponseModel> channelResponseModels = new List<ChannelResponseModel>();

            List<Team> userTeams = _teamManager.GetAllUserTeams(userId);
            List<Project> userProjects = _projectManager.GetAllUserProjects(userId);
            List<Department> userDepartments = _departmentManager.GetAllUserDepartments(userId);

            List<UserProfile> userTeamMates = _vwsDbContext.TeamMembers
                .Include(teamMember => teamMember.UserProfile)
                .Where(teamMember => userTeams.Select(userTeam => userTeam.Id).Contains(teamMember.TeamId) && !teamMember.IsDeleted)
                .Select(teamMember => teamMember.UserProfile).Distinct().ToList();
            userTeamMates.Remove(await _vwsDbContext.GetUserProfileAsync(userId));

            foreach (var userTeamMate in userTeamMates)
            {
                var user = await _vwsDbContext.GetUserProfileAsync(userTeamMate.UserId);
                channelResponseModels.Add(new ChannelResponseModel
                {
                    Guid = userTeamMate.UserId,
                    ChannelTypeId = (byte)SeedDataEnum.ChannelTypes.Private,
                    Title = user.NickName,
                    IsMuted = false,
                    IsPinned = false,
                    EvenOrder = 0,
                    LastTransactionDateTime = new DateTime(),
                    ProfileImageGuid = user.ProfileImageGuid
                });
            }

            channelResponseModels.AddRange(userTeams.Select(userTeam => new ChannelResponseModel
            {
                Guid = userTeam.Guid,
                ChannelTypeId = (byte)SeedDataEnum.ChannelTypes.Team,
                Title = userTeam.Name,
                IsMuted = false,
                IsPinned = false,
                EvenOrder = 0,
                LastTransactionDateTime = new DateTime(),
                ProfileImageGuid = userTeam.TeamImageGuid
            }));

            channelResponseModels.AddRange(userProjects.Select(userProject => new ChannelResponseModel
            {
                Guid = userProject.Guid,
                ChannelTypeId = (byte)SeedDataEnum.ChannelTypes.Project,
                Title = userProject.Name,
                IsMuted = false,
                EvenOrder = 0,
                LastTransactionDateTime = new DateTime(),
                ProfileImageGuid = userProject.ProjectImageGuid
            }));

            channelResponseModels.AddRange(userDepartments.Select(userDepartment => new ChannelResponseModel
            {
                Guid = userDepartment.Guid,
                ChannelTypeId = (byte)SeedDataEnum.ChannelTypes.Department,
                Title = userDepartment.Name,
                IsMuted = false,
                EvenOrder = 0,
                LastTransactionDateTime = new DateTime(),
                ProfileImageGuid = userDepartment.DepartmentImageGuid
            }));

            return channelResponseModels;
        }

        public bool HasUserAccessToChannel(Guid userId, Guid channelId, byte channelTypeId)
        {
            List<Team> userTeams = _teamManager.GetAllUserTeams(userId);

            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
            {
                List<Guid> userTeamMates = _vwsDbContext.TeamMembers
                .Where(teamMember => userTeams.Select(userTeam => userTeam.Id).Contains(teamMember.TeamId) && !teamMember.IsDeleted)
                .Select(teamMember => teamMember.UserProfileId).Distinct().ToList();
                userTeamMates.Remove(userId);

                return userTeamMates.Contains(channelId);
            }

            else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Team)
                return userTeams.Select(team => team.Guid).Contains(channelId);

            else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Project)
            {
                List<Project> userProjects = _projectManager.GetAllUserProjects(userId);
                List<Department> userDepartments = _departmentManager.GetAllUserDepartments(userId);
                var userProjectsUnderTeams = _vwsDbContext.Projects.Include(project => project.ProjectDepartments)
                                                                  .Where(project => project.TeamId != null && userTeams.Select(userTeam => userTeam.Id).Contains((int)project.TeamId));
                foreach (var userProjectUnderTeams in userProjectsUnderTeams)
                {
                    if (userProjectUnderTeams.ProjectDepartments.Count == 0)
                        userProjects.Add(userProjectUnderTeams);
                    else if (userProjectUnderTeams.ProjectDepartments.Select(pd => pd.DepartmentId).Intersect(userDepartments.Select(department => department.Id)).Count() != 0)
                        userProjects.Add(userProjectUnderTeams);
                }
                return userProjects.Select(project => project.Guid).Contains(channelId);
            }

            else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Department)
                return _departmentManager.GetAllUserDepartments(userId).Select(department => department.Guid).Contains(channelId);

            return false;
        }

        public bool DoesChannelExist(Guid channelId, byte channelTypeId)
        {
            if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Private)
                return _vwsDbContext.UserProfiles.Any(user => user.UserId == channelId);

            else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Team)
                return _vwsDbContext.Teams.Any(team => team.Guid == channelId && !team.IsDeleted);

            else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Project)
                return _vwsDbContext.Projects.Any(project => project.Guid == channelId && !project.IsDeleted);

            else if (channelTypeId == (byte)SeedDataEnum.ChannelTypes.Department)
                return _vwsDbContext.Departments.Any(department => department.Guid == channelId && !department.IsDeleted);

            return false;
        }
    }
}
