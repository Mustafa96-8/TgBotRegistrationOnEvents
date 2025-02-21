using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Repositories.IRepositories;

namespace TelegramBot.Services;
public class AdminProfileService
{
    private readonly IUnitOfWork unitOfWork;

    public AdminProfileService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<AdminProfile?> Get(long id, CancellationToken ct, string includeProperty = "")
    {
        var adminProfile = await unitOfWork.AdminProfileRepository.GetByID(id, ct, includeProperty);
        return adminProfile;
    }

    public async Task<IEnumerable<AdminProfile>> GetAll(CancellationToken ct, Expression<Func<AdminProfile, bool>> filter = null)
    {
        IEnumerable<AdminProfile> admins = await unitOfWork.AdminProfileRepository.Get(ct,filter);
        return admins;
    }


    public async Task<bool> Register(AdminProfile adminProfile, Event myEvent, CancellationToken ct)
    {
        // Загружаем коллекцию перед изменением
        await unitOfWork.AdminProfileRepository.applicationContext.Entry(adminProfile).Collection(a => a.Events).LoadAsync(ct);

        if(!adminProfile.Events.Contains(myEvent))
        {
            adminProfile.Events.Add(myEvent);
            unitOfWork.AdminProfileRepository.Update(adminProfile);
        }

        int result = await unitOfWork.Save(ct);
        return result != 0;
    }

    public async Task<bool> Unregister(AdminProfile adminProfile, Event myEvent, CancellationToken ct)
    {
        // Загружаем коллекцию перед изменением
        await unitOfWork.AdminProfileRepository.applicationContext.Entry(adminProfile).Collection(a => a.Events).LoadAsync(ct);
        await unitOfWork.EventRepository.applicationContext.Entry(myEvent).Collection(e => e.AdminProfiles).LoadAsync(ct);

        if(adminProfile.Events.Contains(myEvent))
        {
            adminProfile.Events.Remove(myEvent);
            myEvent.AdminProfiles.Remove(adminProfile);
            unitOfWork.AdminProfileRepository.Update(adminProfile);
            unitOfWork.EventRepository.Update(myEvent);
        }

        int result = await unitOfWork.Save(ct);
        return result != 0;
    }


    public async Task<string> Update(AdminProfile adminProfile, CancellationToken ct)
    {
        var adminProfileFromDb = await unitOfWork.AdminProfileRepository.GetByID(adminProfile.Id, ct);
        if (adminProfileFromDb == null)
        {
            return await Create(adminProfile, ct);
        }
        unitOfWork.AdminProfileRepository.Update(adminProfile);
        int result = await unitOfWork.Save(ct);
        if (result == 0)
        {
            return "Changes not saved";
        }
        return $"{result} changes are accepted";
    }

    public async Task<string> Create(AdminProfile adminProfile, CancellationToken ct)
    {
        await unitOfWork.AdminProfileRepository.Insert(adminProfile, ct);
        int result = await unitOfWork.Save(ct);
        if (result == 0)
        {
            return "User not created";
        }
        return $"{result} changes are accepted";
    }

    public async Task<string> Delete(AdminProfile adminProfile, CancellationToken ct)
    {
        unitOfWork.AdminProfileRepository.Delete(adminProfile);
        var result = await unitOfWork.Save(ct);
        if (result == 0)
        {
            return "User is not remove";
        }
        return $"User deleted from DB";
    }
}
