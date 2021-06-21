using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Models._project;
using vws.web.Models._team;

namespace vws.web.Models._calender
{
    public class EventResponseModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsAllDay { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public UserModel CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public UserModel ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public TeamSummaryResponseModel Team { get; set; }
        public List<ProjectSummaryResponseModel> Projects { get; set; }
        public List<UserModel> Users { get; set; }
    }
}
