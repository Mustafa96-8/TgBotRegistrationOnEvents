using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Domain.Repositories.IRepositories;

namespace TelegramBot.Domain.Repositories;

public class GenericRepository<TEntity> where TEntity : class 
{
    internal ApplicationContext applicationContext;
    internal DbSet<TEntity> dbSet;

    public GenericRepository(ApplicationContext applicationContext)
    {
        this.applicationContext = applicationContext;
        this.dbSet = applicationContext.Set<TEntity>();
    }

    public virtual async Task<IEnumerable<TEntity>> Get(CancellationToken cancellationToken,
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        string includeProperties = "")
    {
        IQueryable<TEntity> query = dbSet;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        foreach (var includeProperty in includeProperties.Split
            (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            query = query.Include(includeProperty);
        }

        if (orderBy != null)
        {
            return await orderBy(query).ToListAsync(cancellationToken);
        }
        else
        {
            return await query.ToListAsync(cancellationToken);
        }
    }

    public virtual async Task<TEntity?> GetByID(object id,CancellationToken cancellationToken)
    {
        return await dbSet.FindAsync(id,cancellationToken);
    }

    public virtual async Task Insert(TEntity entity,CancellationToken cancellationToken)
    {
        await dbSet.AddAsync(entity,cancellationToken:cancellationToken);
    }

    public virtual async Task<bool> DeleteById(object id,CancellationToken cancellationToken)
    {
        TEntity? entityToDelete = await dbSet.FindAsync(id,cancellationToken);
        if (entityToDelete == null)
            return false;
        Delete(entityToDelete);
        return true;
    }

    public virtual void Delete(TEntity entityToDelete)
    {
        if (applicationContext.Entry(entityToDelete).State == EntityState.Detached)
        {
            dbSet.Attach(entityToDelete);
        }
        dbSet.Remove(entityToDelete);
    }

    public virtual void Update(TEntity entityToUpdate)
    {
        dbSet.Update(entityToUpdate);
      
    }
}
