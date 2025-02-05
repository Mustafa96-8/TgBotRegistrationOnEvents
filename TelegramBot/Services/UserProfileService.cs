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
        public async Task<UserProfile> Get(long id,CancellationToken ct) 
        {
            var userProfile = await unitOfWork.UserProfileRepository.Get(u=>u.Id==id);

            return userProfile;
        }

        public async Task<IEnumerable<UserProfile>> GetAll(CancellationToken ct)
        {
            IEnumerable<UserProfile> users = await repository.GetAll(u => u.role == Roles.User && u.IsRegistered == true,ct);
            return users;
        }

        public async Task<string> Update(UserProfile userProfile,CancellationToken ct)
        {
            var result = await repository.UpSert(userProfile, ct);
            if (result == 0)
            {
                return "Changes not saved";
            }
            return $"{result} changes are accepted";
        }

        public async Task<string> Delete(UserProfile userProfile, CancellationToken ct)
        {
            var result = await repository.Delete(userProfile, ct);
            if (result == 0)
            {
                return "User is not remove";
            }
            return $"User deleted from DB";
        }


    }
}
