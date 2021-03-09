using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class TaskResponseModel
    {
        public TaskResponseModel()
        {
            UsersAssignedTo = new List<UserModel>();
        }
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public byte PriorityId { get; set; }
        public string PriorityTitle { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid Guid { get; set; }
        public int? TeamId { get; set; }
        public int? ProjectId { get; set; }
        public List<UserModel> UsersAssignedTo { get; set; }
    }
}
