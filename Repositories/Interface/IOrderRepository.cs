using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Model;

namespace Repositories.Interface
{
    public interface IOrderRepository : IRepository<Order>
    {
        void Update(Order order);
    }
}
