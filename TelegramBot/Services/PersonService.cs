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

    public async Task<string> Update(Person person, CancellationToken ct)
    {
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
            return "User not created";
        }
        return $"{result} changes are accepted";
    }
}
