using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Repositories.IRepositories;


namespace TelegramBot.Domain.Repositories;
public class AdminProfileRepository : IGenericRepository<AdminProfile>
{
    private readonly ApplicationContext applicationContext;

    public AdminProfileRepository(ApplicationContext applicationContext)
    {
        this.applicationContext = applicationContext;
    }

    public async Task<IEnumerable<AdminProfile>> GetAll(Expression<Func<AdminProfile, bool>> filter, CancellationToken ct)
    {
        var users = await applicationContext.AdminProfiles.Where(filter).ToListAsync();
        return users;
    }

    public async Task<AdminProfile> Get(long id, CancellationToken ct)
    {
        var AdminProfile = await applicationContext.AdminProfiles.FirstOrDefaultAsync(p => p.Id == id);

        return AdminProfile;
    }

    public async Task<int> UpSert(AdminProfile AdminProfile, CancellationToken ct)
    {
        var AdminProfileFromDb = await applicationContext.AdminProfiles.FirstOrDefaultAsync(u => u.Id == AdminProfile.Id);
        if (AdminProfileFromDb == null)
        {
            await applicationContext.AddAsync(AdminProfile);
        }
        else
        {
            applicationContext.Entry(AdminProfile).State = EntityState.Modified;
        }
        return await applicationContext.SaveChangesAsync();

    }
    public async Task<int> Delete(AdminProfile AdminProfile, CancellationToken ct)
    {
        applicationContext.Remove(AdminProfile);
        return await applicationContext.SaveChangesAsync();
    }
}
