using BusinessObject;
using Repositories.Implement;
using Repositories.Interface;

namespace Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        public IUserRepository User { get; private set; }
        private ApplicationDbContext _db;

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            User = new UserRepository(_db);
        }

        public void Save()
        {
            _db.SaveChanges();
        }
    }
}
