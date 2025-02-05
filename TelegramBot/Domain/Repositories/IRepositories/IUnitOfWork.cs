using TelegramBot.Domain.Entities;

namespace TelegramBot.Domain.Repositories.IRepositories;
public interface IUnitOfWork : IDisposable
{
    GenericRepository<AdminProfile> AdminProfileRepository { get; }
    GenericRepository<Person> PersonRepository { get; }
    GenericRepository<UserProfile> UserProfileRepository { get; }
    GenericRepository<Event> EventRepository { get; }
    Task<int> Save(CancellationToken cancellationToken);
}