using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Hubs;
using vws.web.Services._chat;

namespace vws.web.ServiceEngine
{
    public class ChannelEngine
    {
        public static void CheckAndSetMutedChannels(IApplicationBuilder app)
        {
            Task.Run(() =>
            {
                var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();

                while (true)
                {
                    try
                    {
                        var vwsDbContext = serviceScope.ServiceProvider.GetRequiredService<IVWS_DbContext>();
                        var channelService = serviceScope.ServiceProvider.GetRequiredService<IChannelService>();
                        var hub = serviceScope.ServiceProvider.GetRequiredService<IHubContext<ChatHub, IChatHub>>();
                        while (true)
                        {
                            Thread.Sleep(1000 * 60 * 5);
                          
                            var mutedChannels = vwsDbContext.MutedChannels.Where(mutedChannel => mutedChannel.IsMuted == true && mutedChannel.ForEver == false).ToList();
                            mutedChannels = mutedChannels.Where(mutedChannel => mutedChannel.MuteUntil <= DateTime.Now).ToList();
                            foreach (var mutedChannel in mutedChannels)
                            {
                                mutedChannel.IsMuted = false;
                                if (UserHandler.ConnectedIds.Keys.Contains(mutedChannel.UserId.ToString()))
                                    UserHandler.ConnectedIds[mutedChannel.UserId.ToString()]
                                               .ConnectionIds
                                               .ForEach(connectionId => hub.Clients.Client(connectionId)
                                                                                   .UnmuteChannel(mutedChannel.ChannelId, mutedChannel.ChannelTypeId));
                            }

                            vwsDbContext.Save();
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
