using Repositories.Interface;

namespace Repositories.UnitOfWork
{
    public interface IUnitOfWork
    {
        IUserRepository User { get; }
        ICategoryRepository Category { get; }
        IProductRepository Product { get; }
        IItemImageRepository ItemImage { get; }
        IFeedbackRepository Feedback { get; }
        void Save();
    }
}
