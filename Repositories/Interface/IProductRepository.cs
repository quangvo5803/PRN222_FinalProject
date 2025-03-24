using BusinessObject.Model;

namespace Repositories.Interface
{
    public interface IProductRepository : IRepository<Product>
    {
        void Update(Product product);
    }
}
