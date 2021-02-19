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
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using vws.web.Domain._file;

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

        #region base

        IQueryable<UserProfile> IVWS_DbContext.UserProfiles { get => UserProfiles; }

        public DbSet<UserProfile> UserProfiles { get; set; }

        IQueryable<Culture> IVWS_DbContext.Cultures { get => Cultures; }

        public DbSet<Culture> Cultures { get; set; }

        IQueryable<RefreshToken> IVWS_DbContext.RefreshTokens { get => RefreshTokens; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

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

        public DbSet<MessageType> MessageTypes { get; set; }

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
            return (selectedChannelType == null) ? null : selectedChannelType.Name;
        }
        public void UpdateChannelType(byte id, string newName)
        {
            var selectedChannelType = ChannelTypes.FirstOrDefault(channelType => channelType.Id == id);
            selectedChannelType.Name = newName;
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

        #endregion

        #region department
        IQueryable<Department> IVWS_DbContext.Departments { get => Departments; }

        public DbSet<Department> Departments { get; set; }

        IQueryable<DepartmentMember> IVWS_DbContext.DepartmentMembers { get => DepartmentMembers; }

        public DbSet<DepartmentMember> DepartmentMembers { get; set; }

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

        public IQueryable<Department> GetUserDepartments(Guid userId)
        {
            return DepartmentMembers.Include(departmentMember => departmentMember.Department)
                                    .Where(departmentMember => departmentMember.IsDeleted == false &&
                                                                departmentMember.UserProfileId == userId &&
                                                                departmentMember.Department.IsDeleted == false)
                                    .Select(departmentMember => departmentMember.Department);
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

        public IQueryable<Project> GetUserProjects(Guid userId)
        {
            return ProjectMembers.Include(projectMember => projectMember.Project)
                .Where(projectMember => projectMember.UserProfileId == userId && projectMember.Project.IsDeleted == false && projectMember.IsDeleted == false)
                .Select(projectMember => projectMember.Project);
        }

        public void AddStatus(ProjectStatus projectStatus)
        {
            ProjectStatuses.Add(projectStatus);
        }
        public string GetStatus(byte id)
        {
            var selectedStatus = ProjectStatuses.FirstOrDefault(status => status.Id == id);
            return selectedStatus == null ? null : selectedStatus.Name;
        }
        public void UpdateStatus(byte id, string newName)
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

        public void DeleteProjectDepartment(ProjectDepartment projectDepartment)
        {
            ProjectDepartments.Remove(projectDepartment);
        }

        public void DeleteProjectMember(ProjectMember projectMember)
        {
            ProjectMembers.Remove(projectMember);
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
            return await TeamMembers.FirstOrDefaultAsync(teamMember => teamMember.TeamId == teamId && teamMember.UserProfileId == memberId && teamMember.HasUserLeft == false);
        }

        public IQueryable<Team> GetUserTeams(Guid userId)
        {
            return TeamMembers.Include(teamMember => teamMember.Team)
                              .Where(teamMember => teamMember.UserProfileId == userId && teamMember.HasUserLeft == false && teamMember.Team.IsDeleted == false)
                              .Select(teamMember => teamMember.Team);
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
        public async Task<FileContainer> GetFileContainerAsync(int id)
        {
            return await FileContainers.FirstOrDefaultAsync(fileContainer => fileContainer.Id == id);
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


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

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
        }
    }
}
