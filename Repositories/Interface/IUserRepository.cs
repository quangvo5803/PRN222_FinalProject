using BusinessObject.Model;

namespace Repositories.Interface
{
    public interface IUserRepository : IRepository<User>
    {
        void Update(User user);
    }
}
