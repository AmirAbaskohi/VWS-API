using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using vws.web.Domain._base;

namespace Domain.Domain._base
{
    [Table("Base_UserWeekends")]
    public class UserWeekend
    {
        public Guid UserProfileId { get; set; }
        
        public byte WeekDayId { get; set; }

        public virtual WeekDay WeekDay { get; set; }

        public virtual UserProfile UserProfile { get; set; }
    }
}
