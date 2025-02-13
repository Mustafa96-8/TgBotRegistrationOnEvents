using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Repositories.IRepositories;

namespace TelegramBot.Services;
public class PersonService
{
    private readonly IUnitOfWork unitOfWork;

    public PersonService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<Person?> Get(long id, CancellationToken ct)
    {
        var person = await unitOfWork.PersonRepository.GetByID(id, ct);
        return person;
    }

    public async Task<bool> Delete(Person person,CancellationToken ct)
    {
        unitOfWork.PersonRepository.Delete(person);
        var result = await unitOfWork.Save(ct);
        if(result == 0)
        {
            return false;
        }
        return true;
    }

    public async Task<string> Update(Person person, CancellationToken ct)
    {
        var personFromDb = await unitOfWork.PersonRepository.GetByID(person.Id, ct);
        if (personFromDb == null)
        {
            return await Create(person, ct);
        }
        unitOfWork.PersonRepository.Update(person);

        int result = await unitOfWork.Save(ct);
        if (result == 0)
        {
            return "Changes not saved";
        }
        return $"{result} changes are accepted";
    }

    public async Task<string> Create(Person person, CancellationToken ct)
    {
        await unitOfWork.PersonRepository.Insert(person, ct);
        int result = await unitOfWork.Save(ct);
        if (result == 0)
        {
            return "Person not created";
        }
        return $"{result} changes are accepted";
    }
}
