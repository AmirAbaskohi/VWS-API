using System;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._base;
using vws.web.Domain._chat;
using vws.web.Domain._department;
using vws.web.Domain._project;
using vws.web.Domain._task;
using vws.web.Domain._team;

namespace vws.web.Domain
{
    public interface IVWS_DbContext
    {
        public void Save();

        #region base

        #region models

        public IQueryable<UserProfile> UserProfiles { get; }

        public IQueryable<Culture> Cultures { get; }

        #endregion

        #region methods

        public Task<UserProfile> AddUserProfileAsync(UserProfile userProfile);

        public Task<UserProfile> GetUserProfileAsync(Guid guid);

        public void DeleteUserProfile(UserProfile userProfile);

        #endregion

        #endregion



        #region chat

        #region models

        public IQueryable<ChannelType> ChannelTypes { get; }

        public IQueryable<Message> Messages { get; }

        public IQueryable<MessageRead> MessageReads { get; }

        public IQueryable<MessageType> MessageTypes { get; }

        #endregion

        #region methods

        public void AddMessage(Message message);

        #endregion

        #endregion



        #region department

        #region models

        public IQueryable<Department> Departments { get; }

        public IQueryable<DepartmentMember> DepartmentMembers { get; }

        #endregion

        #endregion


        #region project

        #region models

        public IQueryable<Project> Projects { get; }

        public IQueryable<ProjectStatus> ProjectStatuses { get; }

        public IQueryable<ProjectMember> ProjectMembers { get; }

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

        #endregion
        #region methods

        public Task<GeneralTask> AddTaskAsync(GeneralTask generalTask);
        public Task<GeneralTask> GetTaskAsync(long id);

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

        #endregion

        #endregion


    }
}
