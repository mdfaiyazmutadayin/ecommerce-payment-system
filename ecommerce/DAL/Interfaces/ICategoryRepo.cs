using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface ICategoryRepo : IRepo<Category, int, bool>
    {
        List<Category> GetAllFlat();
        List<Product> GetProductsByCategoryIds(IEnumerable<int> categoryIds);
    }
}
