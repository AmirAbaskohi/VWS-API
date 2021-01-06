using System;
using Microsoft.EntityFrameworkCore;

namespace vws.web.Domain.chat
{
    public class MessageDbContext : DbContext
    {
        public MessageDbContext(DbContextOptions<MessageDbContext> dbContextOptions)
        : base(dbContextOptions)
        {

        }

        public DbSet<Message> Messages { get; set; }

        public DbSet<MessageType> MessageTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
