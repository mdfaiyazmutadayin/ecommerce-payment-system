using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IOrderRepo : IRepo<Order, int, bool>
    {
        Task<Order> GetOrderWithItemsAsync(int orderId);

        void Add(Order order);
        Task SaveChangesAsync();
    }
}
