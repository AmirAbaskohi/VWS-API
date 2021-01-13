using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._base
{
    [Table("Base_Culture")]
    public class Culture
    {
        [Key]
        public byte Id { get; set; }

        public string CultureAbbreviation { get; set; }
    }
}