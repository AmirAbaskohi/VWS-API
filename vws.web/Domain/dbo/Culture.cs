using System.ComponentModel.DataAnnotations;

namespace vws.web.Domain.dbo
{
    public class Culture
    {
        [Key]
        public byte Id { get; set; }

        public string CultureAbbreviation { get; set; }
    }
}