using BusinessObject;
using BusinessObject.Model;
using Repositories.Interface;

namespace Repositories.Implement
{
    public class FeedbackRepository : Repository<Feedback>, IFeedbackRepository
    {
        private ApplicationDbContext _db;

        public FeedbackRepository(ApplicationDbContext db)
            : base(db)
        {
            _db = db;
        }

        public void Update(Feedback feedback)
        {
            _db.Feedbacks.Update(feedback);
        }
    }
}
