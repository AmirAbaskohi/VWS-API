using System;
using System.ComponentModel.DataAnnotations;

namespace vws.web.Domain.dbo
{
    public class UserProfile
    {
        [Key]
        public int UserId { get; set; }

        public byte CultureId { get; set; }

        [MaxLength(6)]
        public string ThemeColorCode { get; set; }

        public virtual Culture Culture { get; set; }
    }
}
