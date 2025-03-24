using BusinessObject.Model;

namespace Repositories.Interface
{
    public interface IItemImageRepository : IRepository<ItemImage>
    {
        void Update(ItemImage itemImage);
    }
}
