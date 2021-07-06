using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Domain._base
{
    [Table("Base_DayOfWeek")]
    public class WeekDay
    {
        public WeekDay()
        {
            UserWeekends = new HashSet<UserWeekend>();
        }

        public byte Id { get; set; }

        public string Name { get; set; }

        public virtual ICollection<UserWeekend> UserWeekends { get; set; }
    }
}
