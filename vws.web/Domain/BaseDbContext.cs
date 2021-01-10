using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using vws.web.Domain._base;
using vws.web.Domain._project;
using vws.web.Domain._chat;
using vws.web.Domain._department;
using vws.web.Domain._task;
using vws.web.Domain._team;

namespace vws.web.Domain
{
    public class VWS_DbContext : IdentityDbContext<ApplicationUser>
    {
        public VWS_DbContext(DbContextOptions<VWS_DbContext> dbContextOptions)
        : base(dbContextOptions)
        {

        }

        public DbSet<UserProfile> UserProfiles { get; set; }

        public DbSet<Culture> Cultures { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<MessageType> MessageTypes { get; set; }



        public DbSet<Department> Departments { get; set; }

        public DbSet<DepartmentMember> DepartmentMembers { get; set; }



        public DbSet<Project> Projects { get; set; }

        public DbSet<Status> Statuses { get; set; }

        public DbSet<ProjectMember> ProjectMembers { get; set; }



        public DbSet<Team> Teams { get; set; }

        public DbSet<TeamMember> TeamMembers { get; set; }

        public DbSet<TeamType> TeamTypes { get; set; }



        public DbSet<GeneralTask> GeneralTasks { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
