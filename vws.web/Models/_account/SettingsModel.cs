using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._account
{
    public class SettingsModel
    {
        public bool IsDarkModeOn { get; set; }
        public bool IsSeondCalendarOn { get; set; }
        public byte FirstCalendar { get; set; }
        public byte? SecondCalendar { get; set; }
        public List<byte> Weekends { get; set; }
        public byte FirstWeekDay { get; set; }
    }
}
