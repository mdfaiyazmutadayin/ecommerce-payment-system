using DAL.Models;
using System;
using DAL.Interfaces;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repos
{
    public class OrderRepo : IOrderRepo
    {
        private readonly UMSContext _db;

        public OrderRepo(UMSContext db)
        {
            _db = db;
        }

        public bool Create(Order entity)
        {
            _db.Orders.Add(entity);
            return _db.SaveChanges() > 0;
        }

        public bool Delete(int id)
        {
            var exobj = Get(id);
            if (exobj == null) return false;
            _db.Orders.Remove(exobj);
            return _db.SaveChanges() > 0;
        }

        public List<Order> Get() => _db.Orders.ToList();

        public Order Get(int id) => _db.Orders.Find(id);

        public Order GetWithItems(int orderId) =>
        _db.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == orderId);

        public bool Update(Order entity)
        {
            var exobj = Get(entity.Id);
            if (exobj == null) return false;
            _db.Entry(exobj).CurrentValues.SetValues(entity);
            return _db.SaveChanges() > 0;
        }

        public void Add(Order order)
        {
            _db.Orders.Add(order);
        }

        public async Task<Order> GetOrderWithItemsAsync(int orderId)
        {
            return await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        public Task AddAsync(Order order)
        {
            throw new NotImplementedException();
        }
    }
}
