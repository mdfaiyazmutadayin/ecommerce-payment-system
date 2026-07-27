using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DAL.Models;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IProductRepo : IRepo<Product, int, bool>
    {
        Product GetBySku(string sku);

        Task ReduceStockAsync(int productId, int quantity);
        Task<Dictionary<int, Product>> GetActiveProductsDictionaryAsync(IEnumerable<int> productIds);
    }
}
