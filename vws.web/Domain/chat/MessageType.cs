using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain.chat
{
    [Table("MessageType", Schema = "chat")]
    public class MessageType
    {
        [Key]
        public byte Id { get; set; }
    }
}