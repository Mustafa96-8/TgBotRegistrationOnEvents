using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using TelegramBot.Domain.Entities;

namespace TelegramBot.Domain
{
    public class ApplicationContext : DbContext
    {
        public DbSet<Person> Persons { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<AdminProfile> AdminProfiles { get; set; }
        public DbSet<Event> Events { get; set; }

        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=mydatabase.db");
            }

            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Person>().UseTpcMappingStrategy();  // Используем стратегию TPC
        }
    }
}
