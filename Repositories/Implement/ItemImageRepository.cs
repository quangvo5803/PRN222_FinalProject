using BusinessObject;
using BusinessObject.Model;
using Repositories.Interface;

namespace Repositories.Implement
{
    public class ItemImageRepository : Repository<ItemImage>, IItemImageRepository
    {
        private readonly ApplicationDbContext _db;

        public ItemImageRepository(ApplicationDbContext db)
            : base(db)
        {
            _db = db;
        }

        public void Update(ItemImage itemImage)
        {
            _db.ItemImages.Update(itemImage);
        }
    }
}
