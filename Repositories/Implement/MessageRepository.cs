using BusinessObject;
using BusinessObject.Model;
using Repositories.Interface;

namespace Repositories.Implement
{
    public class MessageRepository : Repository<Message>, IMessageRepository
    {
        private readonly ApplicationDbContext _db;

        public MessageRepository(ApplicationDbContext db)
            : base(db)
        {
            _db = db;
        }

        public void Update(Message message)
        {
            _db.Messages.Update(message);
        }
    }
}