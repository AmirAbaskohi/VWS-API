using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._base;

namespace vws.web.Domain._chat
{
    [Table("Chat_MessageEdit")]
    public class MessageEdit
    {
        [Key]
        public long Id { get; set; }

        [MaxLength(4000)]
        public string OldBody { get; set; }

        [MaxLength(4000)]
        public string NewBody { get; set; }

        public Guid ChannelId { get; set; }

        public byte ChannelTypeId { get; set; }

        public long MessageId { get; set; }

        public Guid UserProfileId { get; set; }

        public virtual Message Message { get; set; }

        public virtual UserProfile UserProfile { get; set; }
    }
}
