using BusinessObject;
using BusinessObject.Model;
using Repositories.Interface;

namespace Repositories.Implement
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        private ApplicationDbContext _db;

        public OrderRepository(ApplicationDbContext db)
            : base(db)
        {
            _db = db;
        }

        public void Update(Order order)
        {
            _db.Orders.Update(order);
        }
    }
}
