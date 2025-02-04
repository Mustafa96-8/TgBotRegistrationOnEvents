using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Linq.Expressions;
using TelegramBot.Domain.Collections;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Repositories.IRepositories;

namespace TelegramBot.Domain.Repositories
{
    public class UserProfileRepository : IGenericRepository<UserProfile>
    {
        private readonly ApplicationContext applicationContext;

        public UserProfileRepository(ApplicationContext applicationContext)                                           
        {
            this.applicationContext = applicationContext;
        }

        public async Task<IEnumerable<UserProfile>> GetAll(Expression<Func<UserProfile, bool>> filter, CancellationToken ct)
        {
            var users = await applicationContext.UserProfiles.Where(filter).ToListAsync();
            return users;
        }

        public async Task<UserProfile> Get(long id, CancellationToken ct)
        {
            var userProfile = await applicationContext.UserProfiles.FirstOrDefaultAsync(p => p.Id == id);

            return userProfile;
        }

        public async Task<int> UpSert(UserProfile userProfile,CancellationToken ct)
        {
            var userProfileFromDb = await applicationContext.UserProfiles.FirstOrDefaultAsync(u=> u.Id == userProfile.Id);
            if (userProfileFromDb == null)
            {
                await applicationContext.AddAsync(userProfile);
            }
            else
            {
                applicationContext.Entry(userProfile).State = EntityState.Modified;
            }
            return await applicationContext.SaveChangesAsync();

        }
        public async Task<int> Delete(UserProfile userProfile,CancellationToken ct)
        {
            applicationContext.Remove(userProfile);
            return await applicationContext.SaveChangesAsync();
        }
    }
}
