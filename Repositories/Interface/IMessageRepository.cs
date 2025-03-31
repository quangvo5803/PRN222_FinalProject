using BusinessObject.Model;

namespace Repositories.Interface
{
    public interface IMessageRepository : IRepository<Message>
    {
        void Update(Message message);
    }
}