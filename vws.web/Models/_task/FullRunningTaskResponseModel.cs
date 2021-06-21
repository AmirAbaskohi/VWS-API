using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class FullRunningTaskResponseModel
    {
        public FullRunningTaskResponseModel()
        {
            UsersAssignedTo = new List<UserModel>();
            CheckLists = new List<CheckListResponseModel>();
            Tags = new List<TagResponseModel>();
            Comments = new List<CommentResponseModel>();
            Attachments = new List<FileModel>();
        }
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public byte PriorityId { get; set; }
        public string PriorityTitle { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public UserModel CreatedBy { get; set; }
        public UserModel ModifiedBy { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid Guid { get; set; }
        public int? TeamId { get; set; }
        public int? ProjectId { get; set; }
        public string TeamName { get; set; }
        public string ProjectName { get; set; }
        public int StatusId { get; set; }
        public string StatusTitle { get; set; }
        public bool IsUrgent { get; set; }
        public List<UserModel> UsersAssignedTo { get; set; }
        public List<CheckListResponseModel> CheckLists { get; set; }
        public List<TagResponseModel> Tags { get; set; }
        public List<CommentResponseModel> Comments { get; set; }
        public List<FileModel> Attachments { get; set; }
        public bool IsPaused { get; set; }
        public DateTime? TimeTrackStartDate { get; set; }
    }

    class FullRunningTaskResponseModelComparer : IEqualityComparer<FullRunningTaskResponseModel>
    {
        public bool Equals(FullRunningTaskResponseModel first, FullRunningTaskResponseModel second)
        {

            if (Object.ReferenceEquals(first, second)) return true;

            if (Object.ReferenceEquals(first, null) || Object.ReferenceEquals(second, null))
                return false;

            return first.Id == second.Id;
        }

        public int GetHashCode(FullRunningTaskResponseModel model)
        {
            if (Object.ReferenceEquals(model, null)) return 0;

            return model.Id.GetHashCode();
        }
    }
}
