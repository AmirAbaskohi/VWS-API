using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Enums
{
    public class SeedDataEnum
    {
        public enum MessageTypes : byte
        {
            Text = 1,
            Picture = 2,
            Video = 3,
            Voice = 4,
            Others = 5
        };
        public enum TeamTypes : byte
        {
            Team = 1,
            Company = 2,
            Organization = 3
        };

        public enum ChannelTypes : byte
        {
            Private = 1,
            Team = 2,
            Project = 3,
            Department = 4 
        };

        public enum ProjectStatuses : byte
        {
            Active = 1,
            Hold = 2,
            DoneOrArchived = 3
        };
    }
}
