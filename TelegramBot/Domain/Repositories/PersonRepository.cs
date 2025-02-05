using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Repositories.IRepositories;

namespace TelegramBot.Domain.Repositories;
public class PersonRepository : IGenericRepository<Person>
{
    private readonly ApplicationContext applicationContext;

    public PersonRepository(ApplicationContext applicationContext)
    {
        this.applicationContext = applicationContext;
    }

    public async Task<IEnumerable<Person>> GetAll(Expression<Func<Person, bool>> filter, CancellationToken ct)
    {
        var users = await applicationContext.Persons.Where(filter).ToListAsync();
        return users;
    }

    public async Task<Person> Get(long id, CancellationToken ct)
    {
        var Person = await applicationContext.Persons.FirstOrDefaultAsync(p => p.Id == id);

        return Person;
    }

    public async Task<int> UpSert(Person Person, CancellationToken ct)
    {
        var PersonFromDb = await applicationContext.Persons.FirstOrDefaultAsync(u => u.Id == Person.Id);
        if (PersonFromDb == null)
        {
            await applicationContext.AddAsync(Person);
        }
        else
        {
            applicationContext.Entry(Person).State = EntityState.Modified;
        }
        return await applicationContext.SaveChangesAsync();

    }
    public async Task<int> Delete(Person Person, CancellationToken ct)
    {
        applicationContext.Remove(Person);
        return await applicationContext.SaveChangesAsync();
    }
}
