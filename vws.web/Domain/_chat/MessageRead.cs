using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain._chat
{
    [Table("Chat_MessageRead")]
    public class MessageRead
    {
        public MessageRead()
        {
        }

        [Key]
        public long Id { get; set; }

        public long MessageId { get; set; }

        [MaxLength(256)]
        public string WhoReadUserName { get; set; }

        public virtual Message Message { get; set; }

    }
}
