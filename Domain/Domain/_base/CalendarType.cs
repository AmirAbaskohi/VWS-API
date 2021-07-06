using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Domain._base
{
    [Table("Base_CalendarType")]
    public class CalendarType
    {
        public byte Id { get; set; }

        public string Name { get; set; }
    }
}
