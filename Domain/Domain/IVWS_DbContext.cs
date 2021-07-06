using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Domain._base;
using Microsoft.EntityFrameworkCore.Infrastructure;
using vws.web.Domain._base;
using vws.web.Domain._calendar;
using vws.web.Domain._chat;
using vws.web.Domain._department;
using vws.web.Domain._feedback;
using vws.web.Domain._file;
using vws.web.Domain._notification;
using vws.web.Domain._project;
using vws.web.Domain._task;
using vws.web.Domain._team;

namespace vws.web.Domain
{
    public interface IVWS_DbContext
    {
        public DatabaseFacade DatabaseFacade { get; }

        public void Save();

        #region dbo
        #region models
        public IQueryable<ActivityParameterType> ActivityParameterTypes { get; }
        #endregion
        #region methods
        public void AddActivityParameterType(ActivityParameterType activityParameterType);

        public void UpdateActivityParameterType(byte id, string newName);

        public string GetActivityParameterType(byte id);
        #endregion
        #endregion


        #region version

        #region models

        public IQueryable<_version.Version> Versions { get; }

        public IQueryable<_version.VersionLog> VersionLogs { get; }

        #endregion

        #endregion


        #region base

        #region models

        public IQueryable<UserProfile> UserProfiles { get; }

        public IQueryable<RefreshToken> RefreshTokens { get; }

        public IQueryable<Culture> Cultures { get; }

        public IQueryable<UsersOrder> UsersOrders { get; }

        public IQueryable<UsersActivity> UsersActivities { get; }

        public IQueryable<UserWeekend> UserWeekends { get; }

        public IQueryable<CalendarType> CalendarTypes { get; }

        public IQueryable<WeekDay> WeekDays { get; }


        #endregion

        #region methods

        public Task<UserProfile> AddUserProfileAsync(UserProfile userProfile);

        public Task<UserProfile> GetUserProfileAsync(Guid guid);

        public Task<RefreshToken> GetRefreshTokenAsync(Guid userId, string token);

        public Task<RefreshToken> AddRefreshTokenAsync(RefreshToken refreshToken);

        public void MakeRefreshTokenInvalid(string token);

        public void DeleteUserProfile(UserProfile userProfile);

        public void AddCulture(Culture culture);

        public string GetCulture(byte id);

        public void UpdateCulture(byte id, string name);

        public void AddUsersOrder(UsersOrder usersOrder);

        public void AddUsersActivity(UsersActivity usersActivity);

        public void DeleteUsersOrder(UsersOrder usersOrder);

        public void DeleteUsersOrdersOfSpecificUser(List<Guid> usersOrdersIds, Guid userId);

        public void AddUserWeekend(UserWeekend userWeekend);

        public void DeleteUserWeekend(byte day, Guid userId);

        public void AddCalendarType(CalendarType calendarType);

        public void AddWeekDay(WeekDay weekDay);

        public void UpdateCalendarType(byte id, string newName);

        public void UpdateWeekDay(byte id, string newName);

        public string GetCalendarType(byte id);

        public string GetWeekDay(byte id);

        #endregion

        #endregion


        #region chat

        #region models

        public IQueryable<ChannelType> ChannelTypes { get; }

        public IQueryable<Message> Messages { get; }

        public IQueryable<MessageRead> MessageReads { get; }

        public IQueryable<MessageDeliver> MessageDelivers { get; }

        public IQueryable<MessageEdit> MessageEdits { get; }

        public IQueryable<MessageType> MessageTypes { get; }

        public IQueryable<MutedChannel> MutedChannels { get; }

        public IQueryable<PinnedChannel> PinnedChannels { get; }

        public IQueryable<ChannelTransaction> ChannelTransactions { get; }

        #endregion

        #region methods

        public void AddMessage(Message message);

        public void AddMessageType(MessageType messageType);

        public string GetMessageType(byte id);

        public void UpdateMessageType(byte id, string newName);

        public void AddChannelType(ChannelType channelType);

        public string GetChannelType(byte id);

        public void UpdateChannelType(byte id, string newName);

        public void AddMessageRead(MessageRead messageRead);

        public void AddMessageEdit(MessageEdit messageEdit);

        public void AddMessageDeliver(MessageDeliver messageDeliver);

        public Task<MutedChannel> AddMutedChannelAsync(MutedChannel mutedChannel);

        public Task<MutedChannel> GetMutedChannelAsync(Guid channelId, Guid userId, byte channelTypeId);

        public PinnedChannel AddPinnedChannel(PinnedChannel pinnedChannel);

        public PinnedChannel DeletePinnedChannel(PinnedChannel pinnedChannel);

        public ChannelTransaction AddChannelTransaction(ChannelTransaction channelTransaction);

        public IQueryable<MessageRead> MarkMessagesAsRead(long messageId, Guid userId);

        #endregion

        #endregion


        #region department

        #region models

        public IQueryable<Department> Departments { get; }

        public IQueryable<DepartmentMember> DepartmentMembers { get; }

        public IQueryable<DepartmentHistory> DepartmentHistories { get; }

        public IQueryable<DepartmentHistoryParameter> DepartmentHistoryParameters { get; }

        #endregion

        #region methods

        public Task<Department> AddDepartmentAsync(Department department);

        public Task<DepartmentMember> AddDepartmentMemberAsync(DepartmentMember departmentMember);

        public void AddDepartmentHistory(DepartmentHistory departmentHistory);

        public void AddDepartmentHistoryParameter(DepartmentHistoryParameter departmentHistoryParameter);

        #endregion

        #endregion


        #region project

        #region models

        public IQueryable<Project> Projects { get; }

        public IQueryable<ProjectStatus> ProjectStatuses { get; }

        public IQueryable<ProjectMember> ProjectMembers { get; }

        public IQueryable<ProjectDepartment> ProjectDepartments { get; }

        public IQueryable<ProjectHistory> ProjectHistories { get; }

        public IQueryable<ProjectHistoryParameter> ProjectHistoryParameters { get; }

        public IQueryable<UserProjectActivity> UserProjectActivities { get; }

        public IQueryable<UserProjectOrder> UserProjectOrders { get; }

        #endregion

        #region methods

        public void AddProjectStatus(ProjectStatus projectStatus);

        public string GetProjectStatus(byte id);

        public void UpdateProjectStatus(byte id, string newName);

        public Task<Project> AddProjectAsync(Project project);

        public Task<ProjectMember> AddProjectMemberAsync(ProjectMember projectMember);

        public ProjectDepartment AddProjectDepartment(ProjectDepartment projectDepartment);

        public ProjectHistory AddProjectHistory(ProjectHistory projectHistory);

        public ProjectHistoryParameter AddProjectHistoryParameter(ProjectHistoryParameter projectHistoryParameter);

        public void DeleteProjectDepartment(ProjectDepartment projectDepartment);

        public void DeleteProjectMember(ProjectMember projectMember);

        public void AddUserProjectActivity(UserProjectActivity userProjectActivity);

        public void AddUserProjectOrder(UserProjectOrder userProjectOrder);

        public void DeleteUserProjectOrders(IEnumerable<int> userProjectOrdersIds);

        #endregion

        #endregion


        #region task

        #region models

        public IQueryable<GeneralTask> GeneralTasks { get; }

        public IQueryable<TaskCheckList> TaskCheckLists { get; }

        public IQueryable<TaskCheckListItem> TaskCheckListItems { get; }

        public IQueryable<TaskCommentTemplate> TaskCommentTemplates { get; }

        public IQueryable<TaskReminder> TaskReminders { get; }

        public IQueryable<TaskReminderLinkedUser> TaskReminderLinkedUsers { get; }

        public IQueryable<TaskScheduleType> TaskScheduleTypes { get; }

        public IQueryable<TaskAssign> TaskAssigns { get; }

        public IQueryable<TaskPriority> TaskPriorities { get; }

        public IQueryable<_task.TaskStatus> TaskStatuses { get; }

        public IQueryable<Tag> Tags { get; }

        public IQueryable<TaskTag> TaskTags { get; }

        public IQueryable<TaskComment> TaskComments { get; }

        public IQueryable<TaskCommentAttachment> TaskCommentAttachments { get; }

        public IQueryable<TaskAttachment> TaskAttachments { get; }

        public IQueryable<TimeTrack> TimeTracks { get; }

        public IQueryable<TimeTrackPause> TimeTrackPauses { get; }

        public IQueryable<TaskHistory> TaskHistories { get; }

        public IQueryable<TaskHistoryParameter> TaskHistoryParameters { get; }

        public IQueryable<TaskStatusHistory> TaskStatusHistories { get; }

        #endregion

        #region methods

        public Task<GeneralTask> AddTaskAsync(GeneralTask generalTask);

        public Task<GeneralTask> GetTaskAsync(long id);

        public Task<TaskAssign> AddTaskAssignAsync(TaskAssign taskAssign);

        public void AddTaskPriority(TaskPriority taskPriority);

        public string GetTaskPriority(byte id);

        public void UpdateTaskPriority(byte id, string newName);

        public void AddTaskStatus(_task.TaskStatus taskStatus);

        public void AddCheckList(TaskCheckList checkList);

        public void AddCheckListItem(TaskCheckListItem taskCheckListItem);

        public void AddCheckListItems(List<TaskCheckListItem> taskCheckListItems);

        public void AddTag(Tag tag);

        public void AddTaskTag(TaskTag taskTag);

        public void AddTaskComment(TaskComment taskComment);

        public void AddTaskCommentAttachment(TaskCommentAttachment taskCommentAttachment);

        public void DeleteTag(int id);

        public void DeleteTaskTag(long taskId, int tagId);

        public void DeleteTaskComment(long id);

        public void AddTaskAttachment(TaskAttachment taskAttachment);

        public void DeleteTaskAttachment(TaskAttachment taskAttachment);

        public void AddTimeTrack(TimeTrack timeTrack);

        public void AddTimeTrackPause(TimeTrackPause timeTrackPause);

        public void DeleteTimeTrackPause(TimeTrackPause timeTrackPause);

        public void AddTaskHistory(TaskHistory taskHistory);

        public void AddTaskHistoryParameter(TaskHistoryParameter taskHistoryParameter);

        public void AddTaskStatusHistory(TaskStatusHistory taskStatusHistory);

        public void DeleteTimeTrackPauses(IEnumerable<TimeTrackPause> timeTrackPauses);

        #endregion

        #endregion


        #region team

        #region models

        public IQueryable<Team> Teams { get; }

        public IQueryable<TeamMember> TeamMembers { get; }

        public IQueryable<TeamType> TeamTypes { get; }

        public IQueryable<TeamInviteLink> TeamInviteLinks { get; }

        public IQueryable<TeamHistory> TeamHistories { get; }

        public IQueryable<TeamHistoryParameter> TeamHistoryParameters { get; }

        public IQueryable<UserTeamActivity> UserTeamActivities { get; }

        public IQueryable<UserTeamOrder> UserTeamOrders { get; }

        public IQueryable<TimeTrackPausedSpentTime> TimeTrackPausedSpentTimes { get; }

        #endregion

        #region methods

        public Task<Team> AddTeamAsync(Team team);

        public Task<TeamMember> AddTeamMemberAsync(TeamMember teamMember);

        public Task<TeamInviteLink> AddTeamInviteLinkAsync(TeamInviteLink teamInviteLink);

        public Task<Team> GetTeamAsync(int id);

        public Task<TeamInviteLink> GetTeamInviteLinkByLinkGuidAsync(Guid guid);

        public Task<TeamInviteLink> GetTeamInviteLinkByIdAsync(int id);

        public Task<TeamMember> GetTeamMemberAsync(int teamId, Guid memberId);

        public void AddTeamType(TeamType teamType);

        public string GetTeamType(byte id);

        public void UpdateTeamType(byte id, string newName);

        public void AddTeamHistory(TeamHistory teamHistory);

        public void AddTeamHistoryParameter(TeamHistoryParameter teamHistoryParameter);

        public void AddUserTeamActivity(UserTeamActivity userTeamActivity);

        public void AddUserTeamOrder(UserTeamOrder userTeamActivitiy);

        public void DeleteUserTeamOrders(IEnumerable<int> userTeamOrdersIds);

        public void AddTimeTrackPausedSpentTime(TimeTrackPausedSpentTime timeTrackPauseSpentTime);

        public void DeleteTimeTrackPausedSpentTime(TimeTrackPausedSpentTime timeTrackPauseSpentTime);


        #endregion

        #endregion


        #region notification

        #region models

        public IQueryable<Notification> Notifications { get; }

        public IQueryable<NotificationType> NotificationTypes { get; }

        #endregion

        #region methods

        public void AddNotifications(ICollection<Notification> notifications);

        public void AddNotificationType(NotificationType notificationType);

        public void UpdateNotificationType(byte id, string newName);

        public string GetNotificationType(byte id);

        #endregion

        #endregion


        #region file

        #region models

        public IQueryable<File> Files { get; }

        public IQueryable<FileContainer> FileContainers { get; }

        #endregion

        #region methods

        public Task<File> AddFileAsync(File file);

        public Task<File> GetFileAsync(Guid guid);

        public Task<FileContainer> AddFileContainerAsync(FileContainer fileContainer);

        public Task<FileContainer> GetFileContainerAsync(Guid id);

        public void DeleteFile(File file);

        public void DeleteFileContainer(FileContainer fileContainer);

        #endregion

        #endregion


        #region feedback

        #region models

        public IQueryable<FeedBack> FeedBacks { get; }

        #endregion

        #region methods

        public void AddFeedBack(FeedBack feedBack);

        #endregion

        #endregion


        #region calendar

        #region models

        public IQueryable<Event> Events { get; }

        public IQueryable<EventProject> EventProjects { get; }

        public IQueryable<EventMember> EventUsers { get; }

        public IQueryable<EventHistory> EventHistories { get; }

        public IQueryable<EventHistoryParameter> EventHistoryParameters { get; }

        #endregion

        #region methods

        public void AddEvent(Event newEvent);

        public void AddEventProject(EventProject eventProject);

        public void RemoveEventProject(EventProject eventProject);

        public void AddEventUser(EventMember eventUser);

        public void RemoveEventUser(EventMember eventUser);

        public void AddEventHistory(EventHistory eventHistory);

        public void AddEventHistoryParameter(EventHistoryParameter eventHistoryParameter);

        #endregion

        #endregion

    }
}
