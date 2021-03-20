using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class CommentResponseModel
    {
        public long Id { get; set; }
        public string Body { get; set; }
        public DateTime CommentedOn { get; set; }
        public DateTime MidifiedOn { get; set; }
        public UserModel CommentedBy { get; set; }
        public List<FileModel> Attachments { get; set; }
    }
}
