using Repositories.Interface;

namespace Repositories.UnitOfWork
{
    public interface IUnitOfWork
    {
        IUserRepository User { get; }

        void Save();
    }
}
