using TelegramBot.Domain.Entities;

namespace TelegramBot.Domain.Repositories;
public interface IUnitOfWork : IDisposable
{
    GenericRepository<AdminProfile> AdminProfileRepository { get; }
    GenericRepository<Person> PersonRepository { get; }
    GenericRepository<UserProfile> UserProfileRepository { get; }
    Task<int> Save(CancellationToken cancellationToken);
}