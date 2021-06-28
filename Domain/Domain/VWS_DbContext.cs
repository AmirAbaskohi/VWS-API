using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using vws.web.Domain._base;
using vws.web.Domain._project;
using vws.web.Domain._chat;
using vws.web.Domain._department;
using vws.web.Domain._task;
using vws.web.Domain._team;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using vws.web.Domain._file;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using vws.web.Domain._notification;
using vws.web.Domain._feedback;
using vws.web.Domain._calendar;

namespace vws.web.Domain
{
    public class VWS_DbContext : IdentityDbContext<ApplicationUser>, IVWS_DbContext
    {
        public VWS_DbContext(DbContextOptions<VWS_DbContext> dbContextOptions)
        : base(dbContextOptions)
        {

        }

        public DatabaseFacade DatabaseFacade { get => Database; }

        public void Save()
        {
            SaveChanges();
        }

        #region dbo
        IQueryable<ActivityParameterType> IVWS_DbContext.ActivityParameterTypes { get => ActivityParameterTypes; }

        public DbSet<ActivityParameterType> ActivityParameterTypes { get; set; }

        public void AddActivityParameterType(ActivityParameterType activityParameterType)
        {
            ActivityParameterTypes.Add(activityParameterType);
        }

        public void UpdateActivityParameterType(byte id, string newName)
        {
            var selectedActivityParameterType = ActivityParameterTypes.FirstOrDefault(activityParamType => activityParamType.Id == id);
            selectedActivityParameterType.Name = newName;
        }

        public string GetActivityParameterType(byte id)
        {
            var selectedActivityParameterType = ActivityParameterTypes.FirstOrDefault(activityParamType => activityParamType.Id == id);
            return selectedActivityParameterType == null ? null : selectedActivityParameterType.Name;
        }
        #endregion

        #region version

        IQueryable<_version.Version> IVWS_DbContext.Versions { get => Versions; }

        public DbSet<_version.Version> Versions { get; set; }

        IQueryable<_version.VersionLog> IVWS_DbContext.VersionLogs { get => VersionLogs; }

        public DbSet<_version.VersionLog> VersionLogs { get; set; }

        #endregion

        #region base

        IQueryable<UserProfile> IVWS_DbContext.UserProfiles { get => UserProfiles; }

        public DbSet<UserProfile> UserProfiles { get; set; }

        IQueryable<Culture> IVWS_DbContext.Cultures { get => Cultures; }

        public DbSet<Culture> Cultures { get; set; }

        IQueryable<RefreshToken> IVWS_DbContext.RefreshTokens { get => RefreshTokens; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        IQueryable<UsersOrder> IVWS_DbContext.UsersOrders { get => UsersOrders; }

        public DbSet<UsersOrder> UsersOrders { get; set; }

        IQueryable<UsersActivity> IVWS_DbContext.UsersActivities { get => UsersActivities; }

        public DbSet<UsersActivity> UsersActivities { get; set; }

        public void DeleteUserProfile(UserProfile userProfile)
        {
            UserProfiles.Remove(userProfile);
        }

        public async Task<UserProfile> AddUserProfileAsync(UserProfile userProfile)
        {
            await UserProfiles.AddAsync(userProfile);
            return userProfile;
        }

        public async Task<UserProfile> GetUserProfileAsync(Guid guid)
        {
            return await UserProfiles.Include(profile => profile.ProfileImage).FirstOrDefaultAsync(userProfile => userProfile.UserId == guid);
        }

        public async Task<RefreshToken> GetRefreshTokenAsync(Guid userId, string token)
        {
            return await RefreshTokens.FirstOrDefaultAsync(refreshToken => refreshToken.UserId == userId && refreshToken.Token == token);
        }

        public async Task<RefreshToken> AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            await RefreshTokens.AddAsync(refreshToken);
            return refreshToken;
        }

        public void MakeRefreshTokenInvalid(string token)
        {
            var refreshToken = RefreshTokens.FirstOrDefault(refreshToken => refreshToken.Token == token);
            refreshToken.IsValid = false;
        }

        public void AddCulture(Culture culture)
        {
            Cultures.Add(culture);
        }

        public string GetCulture(byte id)
        {
            var item = Cultures.FirstOrDefault(culture => culture.Id == id);
            return item == null ? null : item.CultureAbbreviation;
        }

        public void UpdateCulture(byte id, string name)
        {
            var item = Cultures.FirstOrDefault(culture => culture.Id == id);
            item.CultureAbbreviation = name;
        }

        public void AddUsersOrder(UsersOrder usersOrder)
        {
            UsersOrders.Add(usersOrder);
        }

        public void AddUsersActivity(UsersActivity usersActivity)
        {
            UsersActivities.Add(usersActivity);
        }

        public void DeleteUsersOrder(UsersOrder usersOrder)
        {
            UsersOrders.Remove(usersOrder);
        }

        public void DeleteUsersOrdersOfSpecificUser(List<Guid> usersOrdersIds, Guid userId)
        {
            UsersOrders.RemoveRange(UsersOrders.Where(usersOrder => usersOrder.OwnerUserId == userId && usersOrdersIds.Contains(usersOrder.TargetUserId)));
        }

        #endregion

        #region chat

        IQueryable<ChannelType> IVWS_DbContext.ChannelTypes { get => ChannelTypes; }

        public DbSet<ChannelType> ChannelTypes { get; set; }

        IQueryable<MutedChannel> IVWS_DbContext.MutedChannels { get => MutedChannels; }

        public DbSet<MutedChannel> MutedChannels { get; set; }

        IQueryable<PinnedChannel> IVWS_DbContext.PinnedChannels { get => PinnedChannels; }

        public DbSet<PinnedChannel> PinnedChannels { get; set; }

        public DbSet<Message> Messages { get; set; }

        IQueryable<Message> IVWS_DbContext.Messages { get => Messages; }

        public DbSet<MessageRead> MessageReads { get; set; }

        IQueryable<MessageRead> IVWS_DbContext.MessageReads { get => MessageReads; }

        public DbSet<MessageDeliver> MessageDelivers { get; set; }

        IQueryable<MessageDeliver> IVWS_DbContext.MessageDelivers { get => MessageDelivers; }

        public DbSet<MessageType> MessageTypes { get; set; }

        IQueryable<MessageEdit> IVWS_DbContext.MessageEdits { get => MessageEdits; }

        public DbSet<MessageEdit> MessageEdits { get; set; }

        IQueryable<MessageType> IVWS_DbContext.MessageTypes { get => MessageTypes; }

        public DbSet<ChannelTransaction> ChannelTransactions { get; set; }

        IQueryable<ChannelTransaction> IVWS_DbContext.ChannelTransactions { get => ChannelTransactions; }

        public void AddMessage(Message m)
        {
            Messages.Add(m);
        }

        public void AddMessageType(MessageType messageType)
        {
            MessageTypes.Add(messageType);
        }

        public string GetMessageType(byte id)
        {
            var selectedMessageType = MessageTypes.FirstOrDefault(messageType => messageType.Id == id);
            return (selectedMessageType == null) ? null : selectedMessageType.Name;
        }

        public void UpdateMessageType(byte id, string newName)
        {
            var selectedMessageType = MessageTypes.FirstOrDefault(messageType => messageType.Id == id);
            selectedMessageType.Name = newName;
        }

        public void AddChannelType(ChannelType channelType)
        {
            ChannelTypes.Add(channelType);
        }

        public string GetChannelType(byte id)
        {
            var selectedChannelType = ChannelTypes.FirstOrDefault(channelType => channelType.Id == id);
            return selectedChannelType == null ? null : selectedChannelType.Name;
        }

        public void UpdateChannelType(byte id, string newName)
        {
            var selectedChannelType = ChannelTypes.FirstOrDefault(channelType => channelType.Id == id);
            selectedChannelType.Name = newName;
        }

        public void AddMessageRead(MessageRead messageRead)
        {
            MessageReads.Add(messageRead);
        }

        public void AddMessageDeliver(MessageDeliver messageDeliver)
        {
            MessageDelivers.Add(messageDeliver);
        }

        public void AddMessageEdit(MessageEdit messageEdit)
        {
            MessageEdits.Add(messageEdit);
        }

        public async Task<MutedChannel> AddMutedChannelAsync(MutedChannel mutedChannel)
        {
            await MutedChannels.AddAsync(mutedChannel);
            return mutedChannel;
        }

        public async Task<MutedChannel> GetMutedChannelAsync(Guid channelId, Guid userId, byte channelTypeId)
        {
            return await MutedChannels.FirstOrDefaultAsync(mChannel => mChannel.ChannelTypeId == channelTypeId &&
                                                                 mChannel.ChannelId == channelId &&
                                                                 mChannel.UserId == userId);
        }

        public PinnedChannel AddPinnedChannel(PinnedChannel pinnedChannel)
        {
            PinnedChannels.Add(pinnedChannel);
            return pinnedChannel;
        }

        public PinnedChannel DeletePinnedChannel(PinnedChannel pinnedChannel)
        {
            PinnedChannels.Remove(pinnedChannel);
            return pinnedChannel;
        }

        public ChannelTransaction AddChannelTransaction(ChannelTransaction channelTransaction)
        {
            ChannelTransactions.Add(channelTransaction);
            return channelTransaction;
        }

        public IQueryable<MessageRead> MarkMessagesAsRead(long messageId, Guid userId)
        {
            SqlParameter[] sqlParameters =
            {
               new SqlParameter("MessageId", messageId),
               new SqlParameter("UserId", $"{userId.ToString()}")
            };
            return MessageReads.FromSqlRaw<MessageRead>("MarkUnreadMessages @MessageId, @UserId", sqlParameters);
        }

        #endregion

        #region department

        IQueryable<Department> IVWS_DbContext.Departments { get => Departments; }

        public DbSet<Department> Departments { get; set; }

        IQueryable<DepartmentMember> IVWS_DbContext.DepartmentMembers { get => DepartmentMembers; }

        public DbSet<DepartmentMember> DepartmentMembers { get; set; }

        IQueryable<DepartmentHistory> IVWS_DbContext.DepartmentHistories { get => DepartmentHistories; }

        public DbSet<DepartmentHistory> DepartmentHistories { get; set; }

        IQueryable<DepartmentHistoryParameter> IVWS_DbContext.DepartmentHistoryParameters { get => DepartmentHistoryParameters; }

        public DbSet<DepartmentHistoryParameter> DepartmentHistoryParameters { get; set; }

        public async Task<Department> AddDepartmentAsync(Department department)
        {
            await Departments.AddAsync(department);
            return department;
        }

        public async Task<DepartmentMember> AddDepartmentMemberAsync(DepartmentMember departmentMember)
        {
            await DepartmentMembers.AddAsync(departmentMember);
            return departmentMember;
        }

        public void AddDepartmentHistory(DepartmentHistory departmentHistory)
        {
            DepartmentHistories.Add(departmentHistory);
        }

        public void AddDepartmentHistoryParameter(DepartmentHistoryParameter departmentHistoryParameter)
        {
            DepartmentHistoryParameters.Add(departmentHistoryParameter);
        }


        #endregion

        #region project

        IQueryable<Project> IVWS_DbContext.Projects { get => Projects; }

        public DbSet<Project> Projects { get; set; }

        IQueryable<ProjectStatus> IVWS_DbContext.ProjectStatuses { get => ProjectStatuses; }

        public DbSet<ProjectStatus> ProjectStatuses { get; set; }

        IQueryable<ProjectMember> IVWS_DbContext.ProjectMembers { get => ProjectMembers; }

        public DbSet<ProjectMember> ProjectMembers { get; set; }

        IQueryable<ProjectDepartment> IVWS_DbContext.ProjectDepartments { get => ProjectDepartments; }

        public DbSet<ProjectDepartment> ProjectDepartments { get; set; }

        IQueryable<ProjectHistory> IVWS_DbContext.ProjectHistories { get => ProjectHistories; }

        public DbSet<ProjectHistory> ProjectHistories { get; set; }

        IQueryable<ProjectHistoryParameter> IVWS_DbContext.ProjectHistoryParameters { get => ProjectHistoryParameters; }

        public DbSet<ProjectHistoryParameter> ProjectHistoryParameters { get; set; }

        IQueryable<UserProjectOrder> IVWS_DbContext.UserProjectOrders { get => UserProjectOrders; }

        public DbSet<UserProjectOrder> UserProjectOrders { get; set; }

        IQueryable<UserProjectActivity> IVWS_DbContext.UserProjectActivities { get => UserProjectActivities; }

        public DbSet<UserProjectActivity> UserProjectActivities { get; set; }

        public void AddProjectStatus(ProjectStatus projectStatus)
        {
            ProjectStatuses.Add(projectStatus);
        }

        public string GetProjectStatus(byte id)
        {
            var selectedStatus = ProjectStatuses.FirstOrDefault(status => status.Id == id);
            return selectedStatus == null ? null : selectedStatus.Name;
        }

        public void UpdateProjectStatus(byte id, string newName)
        {
            var selected = ProjectStatuses.FirstOrDefault(status => status.Id == id);
            selected.Name = newName;
        }

        public async Task<Project> AddProjectAsync(Project project)
        {
            await Projects.AddAsync(project);
            return project;
        }

        public async Task<ProjectMember> AddProjectMemberAsync(ProjectMember projectMember)
        {
            await ProjectMembers.AddAsync(projectMember);
            return projectMember;
        }

        public ProjectDepartment AddProjectDepartment(ProjectDepartment projectDepartment)
        {
            ProjectDepartments.Add(projectDepartment);
            return projectDepartment;
        }

        public ProjectHistory AddProjectHistory(ProjectHistory projectHistory)
        {
            ProjectHistories.Add(projectHistory);
            return projectHistory;
        }

        public ProjectHistoryParameter AddProjectHistoryParameter(ProjectHistoryParameter projectHistoryParameter)
        {
            ProjectHistoryParameters.Add(projectHistoryParameter);
            return projectHistoryParameter;
        }

        public void DeleteProjectDepartment(ProjectDepartment projectDepartment)
        {
            ProjectDepartments.Remove(projectDepartment);
        }

        public void DeleteProjectMember(ProjectMember projectMember)
        {
            ProjectMembers.Remove(projectMember);
        }

        public void AddUserProjectActivity(UserProjectActivity userProjectActivity)
        {
            UserProjectActivities.Add(userProjectActivity);
        }

        public void AddUserProjectOrder(UserProjectOrder userProjectOrder)
        {
            UserProjectOrders.Add(userProjectOrder);
        }

        public void DeleteUserProjectOrders(IEnumerable<int> userProjectOrdersIds)
        {
            UserProjectOrders.RemoveRange(UserProjectOrders.Where(userProjectOrder => userProjectOrdersIds.Contains(userProjectOrder.Id)));
        }

        #endregion

        #region task

        IQueryable<GeneralTask> IVWS_DbContext.GeneralTasks { get => GeneralTasks; }

        public DbSet<GeneralTask> GeneralTasks { get; set; }

        IQueryable<TaskCheckList> IVWS_DbContext.TaskCheckLists { get => TaskCheckLists; }

        public DbSet<TaskCheckList> TaskCheckLists { get; set; }

        IQueryable<TaskCheckListItem> IVWS_DbContext.TaskCheckListItems { get => TaskCheckListItems; }

        public DbSet<TaskCheckListItem> TaskCheckListItems { get; set; }

        IQueryable<TaskCommentTemplate> IVWS_DbContext.TaskCommentTemplates { get => TaskCommentTemplates; }

        public DbSet<TaskCommentTemplate> TaskCommentTemplates { get; set; }

        IQueryable<TaskReminder> IVWS_DbContext.TaskReminders { get => TaskReminders; }

        public DbSet<TaskReminder> TaskReminders { get; set; }

        IQueryable<TaskReminderLinkedUser> IVWS_DbContext.TaskReminderLinkedUsers { get => TaskReminderLinkedUsers; }

        public DbSet<TaskReminderLinkedUser> TaskReminderLinkedUsers { get; set; }

        IQueryable<TaskScheduleType> IVWS_DbContext.TaskScheduleTypes { get => TaskScheduleTypes; }

        public DbSet<TaskScheduleType> TaskScheduleTypes { get; set; }

        IQueryable<TaskAssign> IVWS_DbContext.TaskAssigns { get => TaskAssigns; }

        public DbSet<TaskAssign> TaskAssigns { get; set; }

        IQueryable<TaskPriority> IVWS_DbContext.TaskPriorities { get => TaskPriorities; }

        public DbSet<TaskPriority> TaskPriorities { get; set; }

        IQueryable<_task.TaskStatus> IVWS_DbContext.TaskStatuses { get => TaskStatuses; }

        public DbSet<_task.TaskStatus> TaskStatuses { get; set; }

        IQueryable<Tag> IVWS_DbContext.Tags { get => Tags; }

        public DbSet<Tag> Tags { get; set; }

        IQueryable<TaskTag> IVWS_DbContext.TaskTags { get => TaskTags; }

        public DbSet<TaskTag> TaskTags { get; set; }

        IQueryable<TaskComment> IVWS_DbContext.TaskComments { get => TaskComments; }

        public DbSet<TaskComment> TaskComments { get; set; }

        IQueryable<TaskCommentAttachment> IVWS_DbContext.TaskCommentAttachments { get => TaskCommentAttachments; }

        public DbSet<TaskCommentAttachment> TaskCommentAttachments { get; set; }

        IQueryable<TaskAttachment> IVWS_DbContext.TaskAttachments { get => TaskAttachments; }

        public DbSet<TaskAttachment> TaskAttachments { get; set; }

        IQueryable<TimeTrack> IVWS_DbContext.TimeTracks { get => TimeTracks; }

        public DbSet<TimeTrack> TimeTracks { get; set; }

        IQueryable<TimeTrackPause> IVWS_DbContext.TimeTrackPauses { get => TimeTrackPauses; }

        public DbSet<TimeTrackPause> TimeTrackPauses { get; set; }

        IQueryable<TimeTrackPausedSpentTime> IVWS_DbContext.TimeTrackPausedSpentTimes { get => TimeTrackPausedSpentTimes; }

        public DbSet<TimeTrackPausedSpentTime> TimeTrackPausedSpentTimes { get; set; }

        IQueryable<TaskHistory> IVWS_DbContext.TaskHistories { get => TaskHistories; }

        public DbSet<TaskHistory> TaskHistories { get; set; }

        IQueryable<TaskHistoryParameter> IVWS_DbContext.TaskHistoryParameters { get => TaskHistoryParameters; }

        public DbSet<TaskHistoryParameter> TaskHistoryParameters { get; set; }

        IQueryable<TaskStatusHistory> IVWS_DbContext.TaskStatusHistories { get => TaskStatusHistories; }

        public DbSet<TaskStatusHistory> TaskStatusHistories { get; set; }

        public async Task<GeneralTask> AddTaskAsync(GeneralTask generalTask)
        {
            await GeneralTasks.AddAsync(generalTask);
            return generalTask;
        }

        public async Task<GeneralTask> GetTaskAsync(long id)
        {
            return await GeneralTasks.FirstOrDefaultAsync(task => task.Id == id);
        }

        public async Task<TaskAssign> AddTaskAssignAsync(TaskAssign taskAssign)
        {
            await TaskAssigns.AddAsync(taskAssign);
            return taskAssign;
        }

        public void AddTaskPriority(TaskPriority taskPriority)
        {
            TaskPriorities.Add(taskPriority);
        }

        public string GetTaskPriority(byte id)
        {
            var selectedTaskPriority = TaskPriorities.FirstOrDefault(taskPriority => taskPriority.Id == id);
            return selectedTaskPriority == null ? null : selectedTaskPriority.Name;
        }

        public void UpdateTaskPriority(byte id, string newName)
        {
            var selectedTaskPriority = TaskPriorities.FirstOrDefault(taskPriority => taskPriority.Id == id);
            selectedTaskPriority.Name = newName;
        }

        public void AddTaskStatus(_task.TaskStatus taskStatus)
        {
            TaskStatuses.Add(taskStatus);
        }

        public void AddCheckList(TaskCheckList checkList)
        {
            TaskCheckLists.Add(checkList);
        }

        public void AddCheckListItem(TaskCheckListItem taskCheckListItem)
        {
            TaskCheckListItems.Add(taskCheckListItem);
        }

        public void AddCheckListItems(List<TaskCheckListItem> taskCheckListItems)
        {
            TaskCheckListItems.AddRange(taskCheckListItems);
        }

        public void AddTag(Tag tag)
        {
            Tags.Add(tag);
        }

        public void AddTaskTag(TaskTag taskTag)
        {
            TaskTags.Add(taskTag);
        }

        public void AddTaskComment(TaskComment taskComment)
        {
            TaskComments.Add(taskComment);
        }

        public void AddTaskCommentAttachment(TaskCommentAttachment taskCommentAttachment)
        {
            TaskCommentAttachments.Add(taskCommentAttachment);
        }

        public void DeleteTag(int id)
        {
            var selectedTag = Tags.FirstOrDefault(tag => tag.Id == id);
            Tags.Remove(selectedTag);
        }

        public void DeleteTaskTag(long taskId, int tagId)
        {
            var selectedTaskTag = TaskTags.FirstOrDefault(taskTag => taskTag.GeneralTaskId == taskId && taskTag.TagId == tagId);
            TaskTags.Remove(selectedTaskTag);
        }

        public void DeleteTaskComment(long id)
        {
            var selectedTaskComment = TaskComments.FirstOrDefault(comment => comment.Id == id);
            TaskComments.Remove(selectedTaskComment);
        }

        public void AddTaskAttachment(TaskAttachment taskAttachment)
        {
            TaskAttachments.Add(taskAttachment);
        }

        public void DeleteTaskAttachment(TaskAttachment taskAttachment)
        {
            TaskAttachments.Remove(taskAttachment);
        }

        public void AddTimeTrack(TimeTrack timeTrack)
        {
            TimeTracks.Add(timeTrack);
        }

        public void AddTimeTrackPause(TimeTrackPause timeTrackPause)
        {
            TimeTrackPauses.Add(timeTrackPause);
        }

        public void DeleteTimeTrackPause(TimeTrackPause timeTrackPause)
        {
            TimeTrackPauses.Remove(timeTrackPause);
        }

        public void AddTaskHistory(TaskHistory taskHistory)
        {
            TaskHistories.Add(taskHistory);
        }

        public void AddTaskHistoryParameter(TaskHistoryParameter taskHistoryParameter)
        {
            TaskHistoryParameters.Add(taskHistoryParameter);
        }

        public void AddTaskStatusHistory(TaskStatusHistory taskStatusHistory)
        {
            TaskStatusHistories.Add(taskStatusHistory);
        }

        public void DeleteTimeTrackPauses(IEnumerable<TimeTrackPause> timeTrackPauses)
        {
            TimeTrackPauses.RemoveRange(timeTrackPauses);
        }

        public void AddTimeTrackPausedSpentTime(TimeTrackPausedSpentTime timeTrackPauseSpentTime)
        {
            TimeTrackPausedSpentTimes.Add(timeTrackPauseSpentTime);
        }

        public void DeleteTimeTrackPausedSpentTime(TimeTrackPausedSpentTime timeTrackPauseSpentTime)
        {
            TimeTrackPausedSpentTimes.Remove(timeTrackPauseSpentTime);
        }

        #endregion

        #region team

        IQueryable<Team> IVWS_DbContext.Teams { get => Teams; }

        public DbSet<Team> Teams { get; set; }

        IQueryable<TeamMember> IVWS_DbContext.TeamMembers { get => TeamMembers; }

        public DbSet<TeamMember> TeamMembers { get; set; }

        IQueryable<TeamType> IVWS_DbContext.TeamTypes { get => TeamTypes; }

        public DbSet<TeamType> TeamTypes { get; set; }

        IQueryable<TeamInviteLink> IVWS_DbContext.TeamInviteLinks { get => TeamInviteLinks; }

        public DbSet<TeamInviteLink> TeamInviteLinks { get; set; }

        IQueryable<TeamHistory> IVWS_DbContext.TeamHistories { get => TeamHistories; }

        public DbSet<TeamHistory> TeamHistories { get; set; }

        IQueryable<TeamHistoryParameter> IVWS_DbContext.TeamHistoryParameters { get => TeamHistoryParameters; }

        public DbSet<TeamHistoryParameter> TeamHistoryParameters { get; set; }

        IQueryable<UserTeamOrder> IVWS_DbContext.UserTeamOrders { get => UserTeamOrders; }

        public DbSet<UserTeamOrder> UserTeamOrders { get; set; }

        IQueryable<UserTeamActivity> IVWS_DbContext.UserTeamActivities { get => UserTeamActivities; }

        public DbSet<UserTeamActivity> UserTeamActivities { get; set; }

        public async Task<Team> AddTeamAsync(Team team)
        {
            await Teams.AddAsync(team);
            return team;
        }

        public async Task<TeamMember> AddTeamMemberAsync(TeamMember teamMember)
        {
            await TeamMembers.AddAsync(teamMember);
            return teamMember;
        }

        public async Task<TeamInviteLink> AddTeamInviteLinkAsync(TeamInviteLink teamInviteLink)
        {
            await TeamInviteLinks.AddAsync(teamInviteLink);
            return teamInviteLink;
        }

        public async Task<Team> GetTeamAsync(int id)
        {
            return await Teams.Include(team => team.TeamImage).FirstOrDefaultAsync(team => team.Id == id);
        }

        public async Task<TeamInviteLink> GetTeamInviteLinkByLinkGuidAsync(Guid guid)
        {
            return await TeamInviteLinks.Include(teamInviteLink => teamInviteLink.Team).FirstOrDefaultAsync(teamInviteLink => teamInviteLink.LinkGuid == guid);
        }

        public async Task<TeamInviteLink> GetTeamInviteLinkByIdAsync(int id)
        {
            return await TeamInviteLinks.Include(teamInviteLink => teamInviteLink.Team).FirstOrDefaultAsync(teamInviteLink => teamInviteLink.Id == id);
        }

        public async Task<TeamMember> GetTeamMemberAsync(int teamId, Guid memberId)
        {
            return await TeamMembers.FirstOrDefaultAsync(teamMember => teamMember.TeamId == teamId && teamMember.UserProfileId == memberId && teamMember.IsDeleted == false);
        }

        public void AddTeamType(TeamType teamType)
        {
            TeamTypes.Add(teamType);
        }

        public string GetTeamType(byte id)
        {
            var selectedTeamType = TeamTypes.FirstOrDefault(teamType => teamType.Id == id);
            return (selectedTeamType == null) ? null : selectedTeamType.Name;
        }

        public void UpdateTeamType(byte id, string newName)
        {
            var selectedTeamType = TeamTypes.FirstOrDefault(teamType => teamType.Id == id);
            selectedTeamType.Name = newName;
        }

        public void AddTeamHistory(TeamHistory teamHistory)
        {
            TeamHistories.Add(teamHistory);
        }

        public void AddTeamHistoryParameter(TeamHistoryParameter teamHistoryParameter)
        {
            TeamHistoryParameters.Add(teamHistoryParameter);
        }

        public void AddUserTeamActivity(UserTeamActivity userTeamActivity)
        {
            UserTeamActivities.Add(userTeamActivity);
        }

        public void AddUserTeamOrder(UserTeamOrder userTeamOrder)
        {
            UserTeamOrders.Add(userTeamOrder);
        }

        public void DeleteUserTeamOrders(IEnumerable<int> userTeamOrdersIds)
        {
            UserTeamOrders.RemoveRange(UserTeamOrders.Where(userTeamOrder => userTeamOrdersIds.Contains(userTeamOrder.Id)));
        }

        #endregion

        #region file

        IQueryable<File> IVWS_DbContext.Files { get => Files; }

        public DbSet<File> Files { get; set; }

        IQueryable<FileContainer> IVWS_DbContext.FileContainers { get => FileContainers; }

        public DbSet<FileContainer> FileContainers { get; set; }

        public async Task<File> AddFileAsync(File file)
        {
            await Files.AddAsync(file);
            return file;
        }

        public async Task<File> GetFileAsync(Guid guid)
        {
            return await Files.FirstOrDefaultAsync(file => file.Id == guid);
        }

        public async Task<FileContainer> AddFileContainerAsync(FileContainer fileContainer)
        {
            await FileContainers.AddAsync(fileContainer);
            return fileContainer;
        }

        public async Task<FileContainer> GetFileContainerAsync(Guid guid)
        {
            return await FileContainers.FirstOrDefaultAsync(fileContainer => fileContainer.Guid == guid);
        }

        public void DeleteFile(File file)
        {
            Files.Remove(file);
        }

        public void DeleteFileContainer(FileContainer fileContainer)
        {
            FileContainers.Remove(fileContainer);
        }

        #endregion

        #region notification

        IQueryable<Notification> IVWS_DbContext.Notifications { get => Notifications; }

        public DbSet<Notification> Notifications { get; set; }

        IQueryable<NotificationType> IVWS_DbContext.NotificationTypes { get => NotificationTypes; }

        public DbSet<NotificationType> NotificationTypes { get; set; }

        public void AddNotifications(ICollection<Notification> notifications)
        {
            Notifications.AddRange(notifications);
        }

        public void AddNotificationType(NotificationType notificationType)
        {
            NotificationTypes.Add(notificationType);
        }

        public void UpdateNotificationType(byte id, string newName)
        {
            var selectedNotificationType = NotificationTypes.FirstOrDefault(notifType => notifType.Id == id);
            selectedNotificationType.Name = newName;
        }

        public string GetNotificationType(byte id)
        {
            var selectedNotificationType = NotificationTypes.FirstOrDefault(notifType => notifType.Id == id);
            return selectedNotificationType == null ? (string)null : selectedNotificationType.Name;
        }

        #endregion

        #region feedback

        IQueryable<FeedBack> IVWS_DbContext.FeedBacks { get => FeedBacks; }

        public DbSet<FeedBack> FeedBacks { get; set; }

        public void AddFeedBack(FeedBack feedBack)
        {
            FeedBacks.Add(feedBack);
        }

        #endregion

        #region calendar

        IQueryable<Event> IVWS_DbContext.Events { get => Events; }

        public DbSet<Event> Events { get; set; }

        IQueryable<EventProject> IVWS_DbContext.EventProjects { get => EventProjects; }

        public DbSet<EventProject> EventProjects { get; set; }

        IQueryable<EventMember> IVWS_DbContext.EventUsers { get => EventUsers; }

        public DbSet<EventMember> EventUsers { get; set; }

        IQueryable<EventHistory> IVWS_DbContext.EventHistories { get => EventHistories; }

        public DbSet<EventHistory> EventHistories { get; set; }

        IQueryable<EventHistoryParameter> IVWS_DbContext.EventHistoryParameters { get => EventHistoryParameters; }

        public DbSet<EventHistoryParameter> EventHistoryParameters { get; set; }

        public void AddEvent(Event newEvent)
        {
            Events.Add(newEvent);
        }

        public void AddEventProject(EventProject eventProject)
        {
            EventProjects.Add(eventProject);
        }

        public void RemoveEventProject(EventProject eventProject)
        {
            EventProjects.Remove(eventProject);
        }

        public void AddEventUser(EventMember eventUser)
        {
            EventUsers.Add(eventUser);
        }

        public void RemoveEventUser(EventMember eventUser)
        {
            EventUsers.Remove(eventUser);
        }

        public void AddEventHistory(EventHistory eventHistory)
        {
            EventHistories.Add(eventHistory);
        }

        public void AddEventHistoryParameter(EventHistoryParameter eventHistoryParameter)
        {
            EventHistoryParameters.Add(eventHistoryParameter);
        }

        #endregion

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<TaskCommentAttachment>()
                .HasKey(commentAttachment => new { commentAttachment.FileContainerId, commentAttachment.TaskCommentId});

            builder.Entity<TaskAttachment>()
                .HasKey(taskAttachment => new { taskAttachment.FileContainerId, taskAttachment.GeneralTaskId });

            builder.Entity<TimeTrackPausedSpentTime>()
                .HasKey(ttpst => new { ttpst.UserProfileId, ttpst.GeneralTaskId });

            builder.Entity<ProjectDepartment>()
                .HasKey(pd => new { pd.ProjectId, pd.DepartmentId });
            builder.Entity<ProjectDepartment>()
                .HasOne(pd => pd.Project)
                .WithMany(p => p.ProjectDepartments)
                .HasForeignKey(pd => pd.ProjectId);
            builder.Entity<ProjectDepartment>()
                .HasOne(pd => pd.Department)
                .WithMany(d => d.ProjectDepartments)
                .HasForeignKey(pd => pd.DepartmentId);

            builder.Entity<EventProject>()
                .HasKey(ep => new { ep.ProjectId, ep.EventId });
            builder.Entity<EventProject>()
                .HasOne(ep => ep.Project)
                .WithMany(p => p.EventProjects)
                .HasForeignKey(ep => ep.ProjectId);
            builder.Entity<EventProject>()
                .HasOne(ep => ep.Event)
                .WithMany(e => e.EventProjects)
                .HasForeignKey(ep => ep.EventId);

            builder.Entity<TaskTag>()
                .HasKey(tt => new { tt.TagId, tt.GeneralTaskId });
            builder.Entity<TaskTag>()
                .HasOne(tt => tt.Tag)
                .WithMany(tag => tag.TaskTags)
                .HasForeignKey(tt => tt.TagId);
            builder.Entity<TaskTag>()
                .HasOne(tt => tt.GeneralTask)
                .WithMany(task => task.TaskTags)
                .HasForeignKey(tt => tt.GeneralTaskId);
        }
    }
}
