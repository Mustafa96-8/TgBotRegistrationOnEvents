using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Collections;
using TelegramBot.Domain.Repositories.IRepositories;
using TelegramBot.Domain.Repositories;

namespace TelegramBot.Services
{
    public class UserProfileService
    {
        private readonly IUnitOfWork unitOfWork;

        public UserProfileService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;   
        }

        /// <summary>
        /// Позволяет получить пользователя по его Идентификатору пользователя в Телеграмм, возвращает <see langword="null"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
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

        public async Task<string> Update(UserProfile userProfile,CancellationToken ct)
        {
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
