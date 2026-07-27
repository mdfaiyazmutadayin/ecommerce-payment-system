using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Category : Base
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }

        public Category ParentCategory { get; set; }
        public ICollection<Category> Children { get; set; } = new List<Category>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
