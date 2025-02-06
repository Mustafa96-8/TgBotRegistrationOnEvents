﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Repositories.IRepositories;

namespace TelegramBot.Services;
public class EventService 
{
    private readonly IUnitOfWork unitOfWork;

    public EventService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<Event?> Get(Guid? id, CancellationToken ct)
    {
        if (id == null) return null; 
        var myEvent = await unitOfWork.EventRepository.GetByID(id,ct);
        return myEvent;
    }

    public async Task<IEnumerable<Event>> GetAll(CancellationToken ct, Expression<Func<Event, bool>> filter = null)
    {
        IEnumerable<Event> events= await unitOfWork.EventRepository.Get(ct,filter);
        return events;
    }
    public async Task<IEnumerable<Event>> GetAllByUser(UserProfile userProfile,CancellationToken ct)
    {
        IEnumerable<Event> events = await unitOfWork.EventRepository.Get(ct, u => u.UserProfiles.Contains(userProfile));
        return events;
    }

    public async Task<string> Update(Event myEvent, CancellationToken ct)
    {
        unitOfWork.EventRepository.Update(myEvent);
        int result = await unitOfWork.Save(ct);
        if (result == 0)
        {
            return "Changes not saved";
        }
        return $"{result} changes are accepted";
    }

    public async Task<string> Create(Event myEvent, CancellationToken ct)
    {
        await unitOfWork.EventRepository.Insert(myEvent, ct);
        int result = await unitOfWork.Save(ct);
        if (result == 0)
        {
            return "Event not created";
        }
        return $"{result} changes are accepted";
    }
    public async Task<string> Delete(Event myEvent, CancellationToken ct)
    {
        unitOfWork.EventRepository.Delete(myEvent);
        var result = await unitOfWork.Save(ct);
        if (result == 0)
        {
            return "Event is not remove";
        }
        return $"Event deleted from DB";
    }
}
