using Microsoft.EntityFrameworkCore;
using GameForum.Model;

namespace GameForum.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AuthUser> AuthUsers { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Message> Messages { get; set; }
    }
}
