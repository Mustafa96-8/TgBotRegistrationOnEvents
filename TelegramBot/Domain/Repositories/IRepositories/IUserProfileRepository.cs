using System.Linq.Expressions;
using TelegramBot.Domain.Entities;

namespace TelegramBot.Domain.Repositories.IRepositories
{
    public interface IUserProfileRepository : IGenericRepository<UserProfile>
    {
    }
}