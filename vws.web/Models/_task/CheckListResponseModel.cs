using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class CheckListResponseModel
    {
        public long Id { get; set; }
        public long GeneralTaskId { get; set; }
        public string Title { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid ModifiedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public List<CheckListItemResponseModel> Items { get; set; }
    }
}
