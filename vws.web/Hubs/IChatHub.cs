using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Hubs
{
    public interface IChatHub
    {
        Task ReciveMessage(string message);
        Task InformUserIsOnline(Guid userId);
    }
}
