using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Models;
using vws.web.Models._task;

namespace vws.web.Services._task
{
    public interface ITaskManagerService
    {
        public List<UserModel> GetAssignedTo(long taskId);

        public List<CheckListResponseModel> GetCheckLists(long taskId);

        public List<FileModel> GetTaskAttachments(long taskId);

        public Task<List<CommentResponseModel>> GetTaskComments(long taskId);

        public List<FileModel> GetCommentAttachments(long commentId);

        public List<TagResponseModel> GetTaskTags(long taskId);
    }
}
