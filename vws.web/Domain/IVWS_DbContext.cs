using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using vws.web.Domain._base;
using vws.web.Domain._chat;
using vws.web.Domain._department;
using vws.web.Domain._file;
using vws.web.Domain._project;
using vws.web.Domain._task;
using vws.web.Domain._team;
using vws.web.Models._chat;

namespace vws.web.Domain
{
    public interface IVWS_DbContext
    {
        public DatabaseFacade DatabaseFacade { get; }

        public void Save();

        #region base

        #region models

        public IQueryable<UserProfile> UserProfiles { get; }

        public IQueryable<RefreshToken> RefreshTokens { get; }

        public IQueryable<Culture> Cultures { get; }

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

        public IQueryable<MessageRead> MarkMessagesAsRead(long messageId, Guid userId, string userName);

        #endregion

        #endregion



        #region department

        #region models

        public IQueryable<Department> Departments { get; }

        public IQueryable<DepartmentMember> DepartmentMembers { get; }

        #endregion

        #region methods

        public Task<Department> AddDepartmentAsync(Department department);

        public Task<DepartmentMember> AddDepartmentMemberAsync(DepartmentMember departmentMember);

        public IQueryable<Department> GetUserDepartments(Guid userId);

        #endregion

        #endregion


        #region project

        #region models

        public IQueryable<Project> Projects { get; }

        public IQueryable<ProjectStatus> ProjectStatuses { get; }

        public IQueryable<ProjectMember> ProjectMembers { get; }

        public IQueryable<ProjectDepartment> ProjectDepartments { get; }

        public IQueryable<ProjectHistory> ProjectHistories { get; }

        #endregion

        #region methods

        public IQueryable<Project> GetUserProjects(Guid userId);

        public void AddProjectStatus(ProjectStatus projectStatus);

        public string GetProjectStatus(byte id);

        public void UpdateProjectStatus(byte id, string newName);

        public Task<Project> AddProjectAsync(Project project);

        public Task<ProjectMember> AddProjectMemberAsync(ProjectMember projectMember);

        public ProjectDepartment AddProjectDepartment(ProjectDepartment projectDepartment);

        public ProjectHistory AddProjectHistory(ProjectHistory projectHistory);

        public void DeleteProjectDepartment(ProjectDepartment projectDepartment);

        public void DeleteProjectMember(ProjectMember projectMember);

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

        #endregion

        #region methods

        public Task<GeneralTask> AddTaskAsync(GeneralTask generalTask);

        public Task<GeneralTask> GetTaskAsync(long id);

        public Task<TaskAssign> AddTaskAssignAsync(TaskAssign taskAssign);

        public void AddTaskPriority(TaskPriority taskPriority);

        public string GetTaskPriority(byte id);

        public void UpdateTaskPriority(byte id, string newName);

        public void AddTaskStatus(_task.TaskStatus taskStatus);

        public void DeleteTaskStatus(int id);

        public void AddCheckList(TaskCheckList checkList);

        public void AddCheckListItem(TaskCheckListItem taskCheckListItem);

        public void AddCheckListItems(List<TaskCheckListItem> taskCheckListItems);

        #endregion

        #endregion



        #region team

        #region models

        public IQueryable<Team> Teams { get; }

        public IQueryable<TeamMember> TeamMembers { get; }

        public IQueryable<TeamType> TeamTypes { get; }

        public IQueryable<TeamInviteLink> TeamInviteLinks { get; }

        #endregion

        #region methods

        public Task<Team> AddTeamAsync(Team team);

        public Task<TeamMember> AddTeamMemberAsync(TeamMember teamMember);

        public Task<TeamInviteLink> AddTeamInviteLinkAsync(TeamInviteLink teamInviteLink);

        public Task<Team> GetTeamAsync(int id);

        public Task<TeamInviteLink> GetTeamInviteLinkByLinkGuidAsync(Guid guid);

        public Task<TeamInviteLink> GetTeamInviteLinkByIdAsync(int id);

        public Task<TeamMember> GetTeamMemberAsync(int teamId, Guid memberId);

        public IQueryable<Team> GetUserTeams(Guid userId);

        public void AddTeamType(TeamType teamType);

        public string GetTeamType(byte id);

        public void UpdateTeamType(byte id, string newName);


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


    }
}
