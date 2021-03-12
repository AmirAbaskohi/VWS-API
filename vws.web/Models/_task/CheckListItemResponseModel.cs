using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class CheckListItemResponseModel
    {
        public long Id { get; set; }
        public long TaskCheckListId { get; set; }
        public string Title { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid ModifiedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public bool IsChecked { get; set; }
    }
}
