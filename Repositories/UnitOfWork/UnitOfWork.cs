using BusinessObject;
using Repositories.Implement;
using Repositories.Interface;

namespace Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        public IUserRepository User { get; private set; }
        public ICategoryRepository Category { get; private set; }
        public IProductRepository Product { get; private set; }
        public IItemImageRepository ItemImage { get; private set; }
        public IFeedbackRepository Feedback { get; private set; }
        public IShoppingCartRepository ShoppingCart { get; private set; }
        public IOrderDetailRepository OrderDetail { get; private set; }
        public IOrderRepository Order { get; private set; }
        private ApplicationDbContext _db;

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            User = new UserRepository(_db);
            Category = new CategoryRepository(_db);
            Product = new ProductRepository(_db);
            ItemImage = new ItemImageRepository(_db);
            Feedback = new FeedbackRepository(_db);
            ShoppingCart = new ShoppingCartRepository(_db);
            OrderDetail = new OrderDetailRepository(_db);
            Order = new OrderRepository(_db);
        }

        public void Save()
        {
            _db.SaveChanges();
        }
    }
}
