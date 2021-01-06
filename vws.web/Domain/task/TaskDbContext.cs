using System;
using Microsoft.EntityFrameworkCore;

namespace vws.web.Domain.task
{
    public class TaskDbContext : DbContext
    {
        public TaskDbContext(DbContextOptions<TaskDbContext> dbContextOptions)
        : base(dbContextOptions)
        {

        }

        public DbSet<GeneralTask> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
