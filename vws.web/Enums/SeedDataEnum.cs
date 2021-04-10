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

        public enum TaskPriority : byte
        {
            Low = 1,
            Medium = 3,
            High = 5
        }

        public enum Cultures : byte
        {
            en_US = 1,
            fr_FR = 2,
            ru_RU = 3,
            es_SP = 4,
            pt_PG = 5,
            fa_IR = 6,
            ar_SB = 7,
            de_GE = 8,
            it_IT = 9,
            tr_TU = 10
        };

        public enum NotificationTypes : byte
        {
            Team = 1,
            Project = 2,
            Department = 3,
            Task = 4
        }

        public enum ActivityParameterTypes : byte
        {
            Text = 1,
            User = 2,
            File = 3,
            Color = 4
        }
    }
}
