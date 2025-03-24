using BusinessObject.Model;

namespace Repositories.Interface
{
    public interface IFeedbackRepository : IRepository<Feedback>
    {
        void Update(Feedback feedback);
    }
}
