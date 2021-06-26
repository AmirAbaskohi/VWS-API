using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Domain._base;

namespace vws.web.ServiceEngine
{
    public class InactiveUsersEngine
    {
        public static void RemoveInactiveUsers(IApplicationBuilder app)
        {
            Task.Run(async () =>
            {
                var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();

                while (true)
                {
                    try
                    {
                        var vwsDbContext = serviceScope.ServiceProvider.GetRequiredService<IVWS_DbContext>();
                        var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                        while (true)
                        {
                            Thread.Sleep(1000 * 60 * 60 * 24);

                            var users = vwsDbContext.UserProfiles.ToList();

                            foreach (var user in users)
                            {
                                var selectedAspUser = await userManager.FindByIdAsync(user.UserId.ToString());
                                if (selectedAspUser != null && !selectedAspUser.EmailConfirmed && (DateTime.UtcNow - user.CreatedOn).TotalHours >= 23)
                                {
                                    await userManager.DeleteAsync(selectedAspUser);
                                    vwsDbContext.DeleteUserProfile(user);
                                    vwsDbContext.Save();
                                }
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
