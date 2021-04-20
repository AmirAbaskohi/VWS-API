using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._base;
using vws.web.Domain._file;

namespace vws.web.Domain._feedback
{
    [Table("FeedBack_FeedBack")]
    public class FeedBack
    {
        public int Id { get; set; }

        [MaxLength(250)]
        public string Title { get; set; }
        
        [MaxLength(1000)]
        public string Description { get; set; }

        public Guid UserProfileId { get; set; }

        [ForeignKey("FileContainer")]
        public int? AttachmentId { get; set; }

        public Guid? AttachmentGuid { get; set; }

        public virtual FileContainer FileContainer { get; set; }

        public virtual UserProfile UserProfile { get; set; }
    }
}
