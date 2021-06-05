using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Hubs;
using vws.web.Models;
using vws.web.Models._task;

namespace vws.web.Services._task
{
    public class TaskManagerService : ITaskManagerService
    {
        private readonly IVWS_DbContext _vwsDbContext;
        public readonly IHubContext<ChatHub, IChatHub> _hub;

        public TaskManagerService(IVWS_DbContext vwsDbContext, IHubContext<ChatHub, IChatHub> hub)
        {
            _vwsDbContext = vwsDbContext;
            _hub = hub;
        }

        public List<UserModel> GetAssignedTo(long taskId)
        {
            var result = new List<UserModel>();
            var assignedUsers = _vwsDbContext.TaskAssigns.Include(taskAssign => taskAssign.UserProfile)
                                                        .Where(taskAssign => taskAssign.GeneralTaskId == taskId && !taskAssign.IsDeleted)
                                                        .Select(taskAssign => taskAssign.UserProfile);

            foreach (var user in assignedUsers)
            {
                result.Add(new UserModel()
                {
                    ProfileImageGuid = user.ProfileImageGuid,
                    UserId = user.UserId,
                    NickName = user.NickName
                });
            }

            return result;
        }

        public List<CheckListResponseModel> GetCheckLists(long taskId)
        {
            var result = new List<CheckListResponseModel>();

            var checkLists = _vwsDbContext.TaskCheckLists.Where(checkList => checkList.GeneralTaskId == taskId && !checkList.IsDeleted);
            foreach (var checkList in checkLists)
            {
                result.Add(new CheckListResponseModel()
                {
                    Id = checkList.Id,
                    CreatedBy = checkList.CreatedBy,
                    CreatedOn = checkList.CreatedOn,
                    GeneralTaskId = checkList.GeneralTaskId,
                    ModifiedBy = checkList.ModifiedBy,
                    ModifiedOn = checkList.ModifiedOn,
                    Title = checkList.Title,
                    Items = _vwsDbContext.TaskCheckListItems.Where(item => item.TaskCheckListId == checkList.Id && !item.IsDeleted)
                                                           .Select(item => new CheckListItemResponseModel()
                                                           {
                                                               Id = item.Id,
                                                               CreatedBy = item.CreatedBy,
                                                               CreatedOn = item.CreatedOn,
                                                               IsChecked = item.IsChecked,
                                                               ModifiedBy = item.ModifiedBy,
                                                               ModifiedOn = item.ModifiedOn,
                                                               TaskCheckListId = item.TaskCheckListId,
                                                               Title = item.Title
                                                           })
                                                           .ToList()
                });
            }
            return result;
        }

        public List<FileModel> GetTaskAttachments(long taskId)
        {
            var result = new List<FileModel>();

            var attachments = _vwsDbContext.TaskAttachments.Include(attachment => attachment.FileContainer)
                                                           .Where(attachment => attachment.GeneralTaskId == taskId)
                                                           .Select(attachment => attachment.FileContainer);

            foreach (var attachment in attachments)
            {
                var recentFile = _vwsDbContext.Files.FirstOrDefault(file => file.Id == attachment.RecentFileId);
                result.Add(new FileModel()
                {
                    FileContainerGuid = attachment.Guid,
                    Name = recentFile.Name,
                    Extension = recentFile.Extension,
                    Size = recentFile.Size
                });
            }

            return result;
        }

        public async Task<List<CommentResponseModel>> GetTaskComments(long taskId)
        {
            var result = new List<CommentResponseModel>();
            var comments = _vwsDbContext.TaskComments.Where(comment => comment.GeneralTaskId == taskId);

            foreach (var comment in comments)
            {
                var userProfile = await _vwsDbContext.GetUserProfileAsync(comment.CommentedBy);
                result.Add(new CommentResponseModel()
                {
                    Id = comment.Id,
                    Body = comment.Body,
                    CommentedBy = new UserModel()
                    {
                        UserId = comment.CommentedBy,
                        NickName = userProfile.NickName,
                        ProfileImageGuid = userProfile.ProfileImageGuid
                    },
                    CommentedOn = comment.CommentedOn,
                    MidifiedOn = comment.ModifiedOn,
                    Attachments = GetCommentAttachments(comment.Id)
                });
            }

            return result;
        }

        public List<FileModel> GetCommentAttachments(long commentId)
        {
            var result = new List<FileModel>();

            var attachments = _vwsDbContext.TaskCommentAttachments.Include(attachment => attachment.FileContainer)
                                                                 .Where(attachment => attachment.TaskCommentId == commentId)
                                                                 .Select(attachment => attachment.FileContainer);

            foreach (var attachment in attachments)
            {
                var recentFile = _vwsDbContext.Files.FirstOrDefault(file => file.Id == attachment.RecentFileId);
                result.Add(new FileModel()
                {
                    FileContainerGuid = attachment.Guid,
                    Name = recentFile.Name,
                    Extension = recentFile.Extension,
                    Size = recentFile.Size
                });
            }

            return result;
        }

        public List<TagResponseModel> GetTaskTags(long taskId)
        {
            return _vwsDbContext.TaskTags.Include(taskTag => taskTag.Tag)
                                        .Where(taskTag => taskTag.GeneralTaskId == taskId)
                                        .Select(taskTag => new TagResponseModel() { Id = taskTag.Tag.Id, Title = taskTag.Tag.Title, Color = taskTag.Tag.Color })
                                        .ToList();
        }

        public void StopRunningTimes(long taskId, DateTime endTime)
        {
            var unfinishedTasks = _vwsDbContext.TimeTracks.Where(timeTrack => timeTrack.GeneralTaskId == taskId && timeTrack.EndDate == null).ToList();
            unfinishedTasks.ForEach(timeTrack =>
            {
                timeTrack.EndDate = endTime;
                timeTrack.TotalTimeInMinutes = (endTime - timeTrack.StartDate).TotalMinutes;
                if (UserHandler.ConnectedIds.Keys.Contains(timeTrack.UserProfileId.ToString()))
                    UserHandler.ConnectedIds[timeTrack.UserProfileId.ToString()]
                               .ConnectionIds
                               .ForEach(connectionId => _hub.Clients.Client(connectionId)
                                                                    .ReceiveStopTime(timeTrack.GeneralTaskId, timeTrack.StartDate, endTime, (endTime - timeTrack.StartDate).TotalMinutes));
            });
            _vwsDbContext.Save();

            var pausedTimeTracks = _vwsDbContext.TimeTrackPauses.Include(timeTrackPause => timeTrackPause.TimeTrack).Where(timeTrackPause => timeTrackPause.GeneralTaskId == taskId);
            foreach (var pausedTimeTrack in pausedTimeTracks)
            {
                if (UserHandler.ConnectedIds.Keys.Contains(pausedTimeTrack.UserProfileId.ToString()))
                    UserHandler.ConnectedIds[pausedTimeTrack.UserProfileId.ToString()]
                               .ConnectionIds
                               .ForEach(connectionId => _hub.Clients.Client(connectionId)
                                                                    .ReceiveStopTime(pausedTimeTrack.TimeTrack.GeneralTaskId, pausedTimeTrack.TimeTrack.StartDate, pausedTimeTrack.TimeTrack.EndDate.Value, pausedTimeTrack.TimeTrack.TotalTimeInMinutes.Value));
            }
            _vwsDbContext.DeleteTimeTrackPauses(pausedTimeTracks);
            _vwsDbContext.Save();
        }
    }
}
