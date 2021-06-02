using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Services._project;
using vws.web.Services._team;

namespace vws.web.ServiceEngine
{
    public class IdDate
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
    }

    public class ActivityEngine
    {
        public static void UpdateTeamAndProjectOrder(IApplicationBuilder app)
        {
            Task.Run(() =>
            {
                var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();

                while (true)
                {
                    try
                    {
                        var vwsDbContext = serviceScope.ServiceProvider.GetRequiredService<IVWS_DbContext>();
                        var teamManagerService = serviceScope.ServiceProvider.GetRequiredService<ITeamManagerService>();
                        var projectManagerService = serviceScope.ServiceProvider.GetRequiredService<IProjectManagerService>();
                        while (true)
                        {
                            Thread.Sleep(1000 * 60 * 60);

                            var allUsersIds = vwsDbContext.UserProfiles.Select(profile => profile.UserId).ToList();
                            
                            foreach (var userId in allUsersIds)
                            {
                                var userTeamsIds = teamManagerService.GetAllUserTeams(userId).Select(team => team.Id);
                                var userProjectsIds = projectManagerService.GetAllUserProjects(userId).Select(project => project.Id);

                                #region DeleteUnusuableTeamOrders
                                var userTeamOrders = vwsDbContext.UserTeamOrders.Where(userTeamOrder => userTeamOrder.UserProfileId == userId).Select(userTeamOrder => userTeamOrder.TeamId).ToList();
                                var userTeamOrderShouldBeDeleted = userTeamOrders.Except(userTeamsIds);
                                vwsDbContext.DeleteUserTeamOrders(userTeamOrderShouldBeDeleted);
                                vwsDbContext.Save();
                                #endregion

                                #region DeleteUnusuableProjectOrders
                                var userProjectOrders = vwsDbContext.UserProjectOrders.Where(userProjectOrder => userProjectOrder.UserProfileId == userId).Select(userProjectOrder => userProjectOrder.ProjectId).ToList();
                                var userProjectOrderShouldBeDeleted = userProjectOrders.Except(userProjectsIds);
                                vwsDbContext.DeleteUserProjectOrders(userProjectOrderShouldBeDeleted);
                                vwsDbContext.Save();
                                #endregion

                                var teamOrders = new List<IdDate>();
                                var projectOrders = new List<IdDate>();

                                #region UpdateUserTeamOrders
                                foreach (var userTeamId in userTeamsIds)
                                {
                                    var userTeamActivities = vwsDbContext.UserTeamActivities.Where(utActivity => utActivity.UserProfileId == userId && utActivity.TeamId == userTeamId)
                                                                                            .OrderByDescending(utActivity => utActivity.Time);
                                    if (userTeamActivities.Count() != 0)
                                        teamOrders.Add(new IdDate() { Id = userTeamId, Time = userTeamActivities.First().Time });
                                    else
                                    {
                                        var selectedTeamMember = vwsDbContext.TeamMembers.FirstOrDefault(teamMember => teamMember.TeamId == userTeamId && teamMember.UserProfileId == userId && !teamMember.IsDeleted);
                                        teamOrders.Add(new IdDate() { Id = userTeamId, Time = selectedTeamMember == null ? new DateTime() : selectedTeamMember.CreatedOn });
                                    }
                                }
                                teamOrders = teamOrders.OrderByDescending(teamOrder => teamOrder.Time).ToList();
                                for (int i = 0; i < teamOrders.Count; i++)
                                {
                                    var teamOrder = teamOrders[i];
                                    var selectedUserTeamOrder = vwsDbContext.UserTeamOrders.FirstOrDefault(utOrder => utOrder.UserProfileId == userId && utOrder.TeamId == teamOrder.Id);
                                    if (selectedUserTeamOrder == null)
                                        vwsDbContext.AddUserTeamOrder(new Domain._team.UserTeamOrder() { UserProfileId = userId, TeamId = teamOrder.Id, Order = i + 1 });
                                    else
                                        selectedUserTeamOrder.Order = i + 1;
                                }
                                vwsDbContext.Save();
                                #endregion

                                #region UpdateUserProjectOrders
                                foreach (var userProjectId in userProjectsIds)
                                {
                                    var userProjectsActivities = vwsDbContext.UserProjectActivities.Where(upActivity => upActivity.UserProfileId == userId && upActivity.ProjectId == userProjectId)
                                                                                                   .OrderByDescending(upActivity => upActivity.Time);
                                    if (userProjectsActivities.Count() != 0)
                                        projectOrders.Add(new IdDate() { Id = userProjectId, Time = userProjectsActivities.First().Time });
                                    else
                                        projectOrders.Add(new IdDate() { Id = userProjectId, Time = projectManagerService.GetUserJoinDateTime(userId, userProjectId) });
                                }
                                projectOrders = projectOrders.OrderByDescending(teamOrder => teamOrder.Time).ToList();
                                for (int i = 0; i < teamOrders.Count; i++)
                                {
                                    var projectOrder = projectOrders[i];
                                    var selectedUserProjectOrder = vwsDbContext.UserProjectOrders.FirstOrDefault(upOrder => upOrder.UserProfileId == userId && upOrder.ProjectId == projectOrder.Id);
                                    if (selectedUserProjectOrder == null)
                                        vwsDbContext.AddUserProjectOrder(new Domain._project.UserProjectOrder() { UserProfileId = userId, ProjectId = projectOrder.Id, Order = i + 1 });
                                    else
                                        selectedUserProjectOrder.Order = i + 1;
                                }
                                vwsDbContext.Save();
                                #endregion
                            }
                        }
                    }
                    catch (Exception e)
                    {

                    }

                }

            });
        }
    }
}
