using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._base
{
    [Table("Base_RefreshToken")]
    public class RefreshToken
    {
        [Key]
        public long Id { get; set; }

        public Guid UserId { get; set; }

        public string Token { get; set; }

        public bool IsValid { get; set; }
    }
}
