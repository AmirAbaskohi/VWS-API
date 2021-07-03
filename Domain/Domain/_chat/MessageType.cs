using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._chat
{
    [Table("Chat_MessageType")]
    public class MessageType
    {
        public MessageType()
        {
            Messages = new HashSet<Message>();
        }

        [Key]
        public byte Id { get; set; }

        [MaxLength(150)]
        public string Name { get; set; }

        public virtual ICollection<Message> Messages { get; set; }

    }
}