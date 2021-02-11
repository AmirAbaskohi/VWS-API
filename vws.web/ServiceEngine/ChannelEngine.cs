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
                        while (true)
                        {
                            Thread.Sleep(1000 * 15 * 1);
                            var context = serviceScope.ServiceProvider.GetRequiredService<IVWS_DbContext>();
                            var channelService = serviceScope.ServiceProvider.GetRequiredService<IChannelService>();
                            var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<ChatHub>>();
                            var hub = serviceScope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                            //var a = new ChatHub(context, channelService, logger);
                            hub.Clients.All.SendAsync("UnmuteChannel", "a2");
                        }
                    }
                    catch (Exception e)
                    {
                        //serviceScope.ServiceProvider.GetRequiredService<ILogger>();
                    }

                }

            });

        }
    }
}
