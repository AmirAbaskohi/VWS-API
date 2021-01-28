using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._file;

namespace vws.web.Domain._base
{
    [Table("Base_UserProfile")]
    public class UserProfile
    {
        [Key]
        public Guid UserId { get; set; }

        public byte? CultureId { get; set; }

        [MaxLength(6)]
        public string ThemeColorCode { get; set; }

        public Guid? ProfileImageId { get; set; }

        public virtual Culture Culture { get; set; }
        public virtual File ProfileImage { get; set; }
    }
}
