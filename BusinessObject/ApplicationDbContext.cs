using BusinessObject.Model;
using Microsoft.EntityFrameworkCore;

namespace BusinessObject
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Identity
            modelBuilder.Entity<User>().Property(c => c.Id).ValueGeneratedOnAdd();
        }
    }
}
