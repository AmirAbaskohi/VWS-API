using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._base
{
    [Table("Base_UserProfile")]
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
