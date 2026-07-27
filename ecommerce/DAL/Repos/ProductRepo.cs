using DAL.Interfaces;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repos
{
    public class ProductRepo : IProductRepo
    {
        private readonly UMSContext _db;
        public ProductRepo(UMSContext db) => _db = db;

        public bool Create(Product entity)
        {
            _db.Products.Add(entity);
            return _db.SaveChanges() > 0;
        }

        public bool Delete(int id)
        {
            var exobj = Get(id);
            if (exobj == null) return false;
            _db.Products.Remove(exobj);
            return _db.SaveChanges() > 0;
        }

        public List<Product> Get() => _db.Products.ToList();

        public Product Get(int id) => _db.Products.Find(id);

        public Product GetBySku(string sku) =>
            _db.Products.FirstOrDefault(p => p.Sku == sku);

        public bool Update(Product entity)
        {
            var exobj = Get(entity.Id);
            if (exobj == null) return false;
            _db.Entry(exobj).CurrentValues.SetValues(entity);
            return _db.SaveChanges() > 0;
        }

        public async Task ReduceStockAsync(int productId, int quantity)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product != null)
            {
                product.Stock -= quantity;
            }
        }

        // Add this implementation
        public async Task<Dictionary<int, Product>> GetActiveProductsDictionaryAsync(IEnumerable<int> productIds)
        {
            return await _db.Products
                .Where(p => p.IsActive && productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);
        }
    }
}
