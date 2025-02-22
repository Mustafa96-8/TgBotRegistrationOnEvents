﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Collections;
using TelegramBot.Domain.Repositories.IRepositories;

namespace TelegramBot.Services
{
    public class UserProfileService
    {
        private readonly IUnitOfWork unitOfWork;

        public UserProfileService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;   
        }

        public async Task<UserProfile?> Get(long id,CancellationToken ct) 
        {
            var userProfile = await unitOfWork.UserProfileRepository.GetByID(id,ct);
            return userProfile;
        }

        public async Task<IEnumerable<UserProfile>> GetAll(CancellationToken ct)
        {
            IEnumerable<UserProfile> users = await unitOfWork.UserProfileRepository.Get(ct,u=>u.IsRegistered == true);
            return users;
        }
        public async Task<IEnumerable<UserProfile>> GetAllByEvent(Event myEvent,CancellationToken ct)
        {
            IEnumerable<UserProfile> users = await unitOfWork.UserProfileRepository.Get(ct, u => u.IsRegistered == true&&u.Events.Contains(myEvent));
            return users;
        }

        public async Task<string> Update(UserProfile userProfile,CancellationToken ct)
        {
            var userProfileFromDb = await unitOfWork.UserProfileRepository.GetByID(userProfile.Id, ct);
            if (userProfileFromDb == null)
            {
                return await Create(userProfile, ct);
            }
            
            unitOfWork.UserProfileRepository.Update(userProfile);
            int result = await unitOfWork.Save(ct);
            if (result == 0)
            {
                return "Changes not saved";
            }
            return $"{result} changes are accepted";
        }

        public async Task<string> Create(UserProfile userProfile,CancellationToken ct)
        {
            await unitOfWork.UserProfileRepository.Insert(userProfile,ct);
            int result = await unitOfWork.Save(ct);
            if (result == 0)
            {
                return "User not created";
            }
            return $"{result} changes are accepted";
        }
        public async Task<bool> Register(UserProfile userProfile, Event myEvent, CancellationToken ct)
        {
            // Загружаем коллекцию перед изменением
            await unitOfWork.UserProfileRepository.applicationContext.Entry(userProfile).Collection(a => a.Events).LoadAsync(ct);

            if(!userProfile.Events.Contains(myEvent))
            {
                userProfile.Events.Add(myEvent);
                unitOfWork.UserProfileRepository.Update(userProfile);
            }

            int result = await unitOfWork.Save(ct);
            return result != 0;
        }

        public async Task<bool> Unregister(UserProfile userProfile, Event myEvent, CancellationToken ct)
        {
            // Загружаем коллекцию перед изменением
            await unitOfWork.UserProfileRepository.applicationContext.Entry(userProfile).Collection(a => a.Events).LoadAsync(ct);
            await unitOfWork.EventRepository.applicationContext.Entry(myEvent).Collection(e => e.UserProfiles).LoadAsync(ct);

            if(userProfile.Events.Contains(myEvent))
            {
                userProfile.Events.Remove(myEvent);
                myEvent.UserProfiles.Remove(userProfile);
                unitOfWork.UserProfileRepository.Update(userProfile);
                unitOfWork.EventRepository.Update(myEvent);
            }
            int result = await unitOfWork.Save(ct);
            return result != 0;
        }
        public async Task<string> Delete(UserProfile userProfile, CancellationToken ct)
        {
            unitOfWork.UserProfileRepository.Delete(userProfile);
            var result = await unitOfWork.Save(ct);
            if (result == 0)
            {
                return "User is not remove";
            }
            return $"User deleted from DB";
        }


    }
}
