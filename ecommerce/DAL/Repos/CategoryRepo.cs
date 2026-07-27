using DAL.Interfaces;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repos
{
    public class CategoryRepo : ICategoryRepo
    {
        private readonly UMSContext _db;
        public CategoryRepo(UMSContext db) => _db = db;

        public bool Create(Category entity)
        {
            _db.Categories.Add(entity);
            return _db.SaveChanges() > 0;
        }

        public bool Delete(int id)
        {
            var existing = Get(id);
            if (existing == null) return false;
            _db.Categories.Remove(existing);
            return _db.SaveChanges() > 0;
        }

        public List<Category> Get() => _db.Categories.ToList();
        public Category Get(int id) => _db.Categories.Find(id);

        public bool Update(Category entity)
        {
            var existing = Get(entity.Id);
            if (existing == null) return false;
            _db.Entry(existing).CurrentValues.SetValues(entity);
            return _db.SaveChanges() > 0;
        }

        // Flat list — no eager tree-loading via EF. The DFS traversal
        // reconstructs the tree shape from this in BLL.
        public List<Category> GetAllFlat() => _db.Categories.ToList();

        public List<Product> GetProductsByCategoryIds(IEnumerable<int> categoryIds) =>
            _db.Products.Where(p => categoryIds.Contains(p.CategoryId)).ToList();
    }
}