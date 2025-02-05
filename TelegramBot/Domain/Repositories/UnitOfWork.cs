using TelegramBot.Domain.Entities;

namespace TelegramBot.Domain.Repositories;
public class UnitOfWork(ApplicationContext applicationContext) : IUnitOfWork
{
    private ApplicationContext context = applicationContext;
    private GenericRepository<Person> personRepository;
    private GenericRepository<UserProfile> userProfileRepository;
    private GenericRepository<AdminProfile> adminProfileRepository;

    public GenericRepository<Person> PersonRepository
    {
        get
        {

            if (this.personRepository == null)
            {
                this.personRepository = new GenericRepository<Person>(context);
            }
            return personRepository;
        }
    }
    public GenericRepository<UserProfile> UserProfileRepository
    {
        get
        {

            if (this.userProfileRepository == null)
            {
                this.userProfileRepository = new GenericRepository<UserProfile>(context);
            }
            return userProfileRepository;
        }
    }
    public GenericRepository<AdminProfile> AdminProfileRepository
    {
        get
        {

            if (this.adminProfileRepository == null)
            {
                this.adminProfileRepository = new GenericRepository<AdminProfile>(context);
            }
            return adminProfileRepository;
        }
    }



    public void Save()
    {
        context.SaveChanges();
    }

    private bool disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                context.Dispose();
            }
        }
        this.disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
