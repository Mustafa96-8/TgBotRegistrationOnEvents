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
            try
            {
                Database.OpenConnection();  // Пробуем открыть соединение
            }
            catch (Exception)
            {
                Database.EnsureCreated(); // Создаем новую БД
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(Environment.GetEnvironmentVariable("CONNECTIONSTRING"));
            }

            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Person>().UseTpcMappingStrategy();  // Используем стратегию TPC
            modelBuilder.Entity<UserProfile>()
                        .HasMany(u => u.Events)
                        .WithMany(e => e.UserProfiles)
                        .UsingEntity<Dictionary<string, object>>(
                            "UserProfileEvent",
                            j => j.HasOne<Event>().WithMany().HasForeignKey("EventId").OnDelete(DeleteBehavior.Cascade),
                            j => j.HasOne<UserProfile>().WithMany().HasForeignKey("UserProfileId").OnDelete(DeleteBehavior.Cascade)
                        );
            modelBuilder.Entity<AdminProfile>()
                        .HasMany(u => u.Events)
                        .WithMany(u=>u.AdminProfiles)
                        .UsingEntity<Dictionary<string, object>>(
                        "AdminProfileEvent",
                            j => j.HasOne<Event>().WithMany().HasForeignKey("EventId").OnDelete(DeleteBehavior.Cascade),
                            j => j.HasOne<AdminProfile>().WithMany().HasForeignKey("AdminProfileId").OnDelete(DeleteBehavior.Cascade)
                        );
                        
        }
    }
}
