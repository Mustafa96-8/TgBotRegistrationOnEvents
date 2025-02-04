using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Domain.Entities;

namespace TelegramBot.Domain.Repositories.IRepositories;
public interface IGenericRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>> filter, CancellationToken ct);

    Task<T> Get(long id, CancellationToken ct);

    Task<int> UpSert(T T, CancellationToken ct);

    Task<int> Delete(T T, CancellationToken ct);
}

